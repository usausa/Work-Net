namespace WorkNfc;

using PCSC;
using PCSC.Monitoring;

public sealed class SuicaReader
{
    // TODO Fix polling
    // TODO Fix read block

    const ushort SERVICE_CODE_HISTORY = 0x090F;

    static void Main(string[] args)
    {
        var monitor = MonitorFactory.Instance.Create(SCardScope.System);

        monitor.CardInserted += (sender, e) =>
        {
            Console.WriteLine($"\n[カード検出] {e.ReaderName}");
            Console.WriteLine(new string('=', 60));
            ReadSuicaCard(e.ReaderName);
        };

        monitor.CardRemoved += (sender, e) => { Console.WriteLine($"\n[カード除去] {e.ReaderName}"); };

        monitor.MonitorException += (sender, e) => { Console.WriteLine($"モニターエラー: {e.Message}"); };

        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            var readers = context.GetReaders();
            if (readers.Length == 0)
            {
                Console.WriteLine("リーダーが見つかりません");
                return;
            }

            Console.WriteLine($"使用リーダー: {readers[0]}");
            monitor.Start(readers[0]);
        }

        Console.WriteLine("Suicaをタッチしてください (Enterキーで終了)...\n");
        Console.ReadLine();
        monitor.Cancel();
    }

    static void ReadSuicaCard(string readerName)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);

            try
            {
                Console.WriteLine("[デバッグ] カードに接続しました");

                // Pollingコマンドを実行
                var pollingResult = ExecutePolling(reader);
                if (pollingResult == null)
                {
                    Console.WriteLine("[エラー] Pollingに失敗しました");
                    return;
                }

                byte[] idm = pollingResult.Item1;
                byte[] pmm = pollingResult.Item2;

                Console.WriteLine($"IDm: {BitConverter.ToString(idm).Replace("-", "")}");
                Console.WriteLine($"PMm: {BitConverter.ToString(pmm).Replace("-", "")}");
                Console.WriteLine();

                ReadHistory(reader, idm);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[エラー] 読み取りエラー: {ex.Message}");
                Console.WriteLine($"[エラー] スタックトレース: {ex.StackTrace}");
            }
            finally
            {
                reader.Disconnect(SCardReaderDisposition.Leave);
            }
        }
    }

    static Tuple<byte[], byte[]> ExecutePolling(ICardReader reader)
    {
        try
        {
            Console.WriteLine("[デバッグ] Pollingコマンドを送信 (システムコード: FFFF)");

            // 生のFeliCa Pollingコマンド (コマンドコード 0x00)
            List<byte> cmd = new List<byte>();
            cmd.Add(0xFF); // CLA
            cmd.Add(0xFE); // INS: 汎用
            cmd.Add(0x00); // P1
            cmd.Add(0x00); // P2

            // FeliCa Pollingコマンド
            List<byte> felicaCmd = new List<byte>();
            felicaCmd.Add(0x00); // コマンドコード: Polling
            felicaCmd.Add(0xFF); // システムコード上位
            felicaCmd.Add(0xFF); // システムコード下位
            felicaCmd.Add(0x01); // リクエストコード
            felicaCmd.Add(0x00); // タイムスロット

            cmd.Add((byte)felicaCmd.Count); // Lc
            cmd.AddRange(felicaCmd);
            cmd.Add(0x00); // Le

            Console.WriteLine($"[デバッグ] 送信: {BitConverter.ToString(cmd.ToArray())}");
            var response = new byte[256];
            int length = reader.Transmit(cmd.ToArray(), response);
            Console.WriteLine($"[デバッグ] 応答({length}): {BitConverter.ToString(response, 0, length)}");

            // ステータスワードの確認
            if (length < 2)
            {
                Console.WriteLine("[エラー] 応答が短すぎます");
                return null;
            }

            byte sw1 = response[length - 2];
            byte sw2 = response[length - 1];
            Console.WriteLine($"[デバッグ] ステータスワード: {sw1:X2} {sw2:X2}");

            if (sw1 != 0x90 || sw2 != 0x00)
            {
                Console.WriteLine($"[エラー] ステータスエラー");
                return null;
            }

            // Polling応答の解析
            // 実際の応答: [01][01][IDm(8)][PMm(8)][システムコード(2)][90][00]
            // または: [データ長][応答コード][IDm(8)][PMm(8)][システムコード(2)][90][00]

            int dataLength = length - 2; // ステータスワードを除く

            if (dataLength < 18)
            {
                Console.WriteLine($"[エラー] データが不足: {dataLength}バイト");
                return null;
            }

            int pos = 0;

            // 最初のバイトがデータ長の可能性
            byte firstByte = response[pos];
            Console.WriteLine($"[デバッグ] 最初のバイト: {firstByte:X2}");

            // データ長フィールドをスキップ（存在する場合）
            if (firstByte == dataLength - 1 || firstByte == 0x01)
            {
                pos++;
            }

            // 応答コード
            byte responseCode = response[pos++];
            Console.WriteLine($"[デバッグ] 応答コード: {responseCode:X2}");

            if (responseCode != 0x01)
            {
                Console.WriteLine($"[エラー] Polling応答コードが不正: {responseCode:X2} (期待値: 0x01)");
                return null;
            }

            // IDm (8バイト)
            byte[] idm = new byte[8];
            Array.Copy(response, pos, idm, 0, 8);
            pos += 8;
            Console.WriteLine($"[デバッグ] IDm抽出: {BitConverter.ToString(idm)}");

            // PMm (8バイト)
            byte[] pmm = new byte[8];
            Array.Copy(response, pos, pmm, 0, 8);
            pos += 8;
            Console.WriteLine($"[デバッグ] PMm抽出: {BitConverter.ToString(pmm)}");

            // システムコード (2バイト) - オプション
            if (pos + 2 <= dataLength)
            {
                byte[] systemCode = new byte[2];
                Array.Copy(response, pos, systemCode, 0, 2);
                Console.WriteLine($"[デバッグ] システムコード: {BitConverter.ToString(systemCode)}");
            }

            Console.WriteLine("[デバッグ] Polling成功");
            return Tuple.Create(idm, pmm);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[エラー] Polling例外: {ex.Message}");
            Console.WriteLine($"[エラー] スタックトレース: {ex.StackTrace}");
            return null;
        }
    }

    static void ReadHistory(ICardReader reader, byte[] idm)
    {
        Console.WriteLine("履歴情報:");
        Console.WriteLine(new string('-', 60));

        int successCount = 0;
        int failCount = 0;

        for (int blockNumber = 0; blockNumber < 20; blockNumber++)
        {
            Console.WriteLine($"\n[デバッグ] ブロック {blockNumber} の読み取りを開始");

            byte[] blockData = ReadBlock(reader, idm, SERVICE_CODE_HISTORY, (byte)blockNumber);

            if (blockData == null)
            {
                failCount++;
                if (failCount >= 3)
                {
                    Console.WriteLine("[情報] 連続して失敗したため読み取りを終了します");
                    break;
                }

                continue;
            }

            Console.WriteLine($"[デバッグ] データ: {BitConverter.ToString(blockData)}");
            ParseHistoryRecord(blockNumber, blockData);
            successCount++;
            failCount = 0;
        }

        Console.WriteLine($"\n[情報] 読み取り完了: 成功 {successCount}件");
    }

    static byte[] ReadBlock(ICardReader reader, byte[] idm, ushort serviceCode, byte blockNumber)
    {
        try
        {
            // FeliCa Read Without Encryptionコマンドを送信
            List<byte> cmd = new List<byte>();
            cmd.Add(0xFF); // CLA
            cmd.Add(0xFE); // INS
            cmd.Add(0x00); // P1
            cmd.Add(0x00); // P2

            List<byte> felicaCmd = new List<byte>();
            felicaCmd.Add(0x06); // コマンドコード: Read Without Encryption
            felicaCmd.AddRange(idm); // IDm
            felicaCmd.Add(0x01); // サービス数
            felicaCmd.Add((byte)(serviceCode & 0xFF));
            felicaCmd.Add((byte)((serviceCode >> 8) & 0xFF));
            felicaCmd.Add(0x01); // ブロック数
            felicaCmd.Add(0x80); // ブロックリスト
            felicaCmd.Add(blockNumber);

            cmd.Add((byte)felicaCmd.Count);
            cmd.AddRange(felicaCmd);
            cmd.Add(0x00);

            Console.WriteLine($"[デバッグ] 送信: {BitConverter.ToString(cmd.ToArray())}");
            var response = new byte[256];
            int length = reader.Transmit(cmd.ToArray(), response);
            Console.WriteLine($"[デバッグ] 応答({length}): {BitConverter.ToString(response, 0, length)}");

            if (length < 2)
            {
                Console.WriteLine("[エラー] 応答が短すぎます");
                return null;
            }

            byte sw1 = response[length - 2];
            byte sw2 = response[length - 1];
            Console.WriteLine($"[デバッグ] ステータスワード: {sw1:X2} {sw2:X2}");

            if (sw1 != 0x90 || sw2 != 0x00)
            {
                Console.WriteLine($"[エラー] ステータスエラー");
                return null;
            }

            return ParseFeliCaReadResponse(response, length - 2);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[エラー] 例外: {ex.Message}");
        }

        return null;
    }

    static byte[] ParseFeliCaReadResponse(byte[] response, int dataLength)
    {
        try
        {
            // Read Without Encryption応答
            // [データ長][応答コード 0x07][IDm(8)][SF1][SF2][ブロック数][ブロックデータ(16)]

            if (dataLength < 13)
            {
                Console.WriteLine($"[エラー] データ不足: {dataLength}バイト");
                return null;
            }

            int pos = 0;
            byte respLen = response[pos++];
            byte responseCode = response[pos++];

            Console.WriteLine($"[デバッグ] 応答長: {respLen}, 応答コード: {responseCode:X2}");

            if (responseCode != 0x07)
            {
                Console.WriteLine($"[エラー] 応答コードが不正: {responseCode:X2} (期待値: 0x07)");
                return null;
            }

            // IDmをスキップ (8バイト)
            pos += 8;

            // ステータスフラグ
            byte sf1 = response[pos++];
            byte sf2 = response[pos++];
            Console.WriteLine($"[デバッグ] ステータスフラグ: {sf1:X2} {sf2:X2}");

            if (sf1 != 0x00 || sf2 != 0x00)
            {
                Console.WriteLine($"[警告] FeliCaステータスフラグエラー");
                return null;
            }

            // ブロック数
            byte blockCount = response[pos++];
            Console.WriteLine($"[デバッグ] ブロック数: {blockCount}");

            if (dataLength - pos < 16)
            {
                Console.WriteLine($"[エラー] ブロックデータ不足");
                return null;
            }

            // ブロックデータ (16バイト)
            byte[] blockData = new byte[16];
            Array.Copy(response, pos, blockData, 0, 16);
            Console.WriteLine($"[デバッグ] ブロックデータ抽出成功");

            return blockData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[エラー] 解析例外: {ex.Message}");
            return null;
        }
    }

    static void ParseHistoryRecord(int index, byte[] data)
    {
        int termType = data[0];
        int process = data[1];
        int dateValue = data[4] | (data[5] << 8);

        if (dateValue == 0)
        {
            Console.WriteLine($"[{index + 1:D2}] (空のレコード)");
            return;
        }

        DateTime date = new DateTime(2000, 1, 1).AddDays(dateValue);
        int balance = data[8] | (data[9] << 8);
        int inStation = data[6];
        int outStation = data[7];

        Console.WriteLine($"[{index + 1:D2}] {date:yyyy/MM/dd} " +
                          $"種別:{GetTerminalType(termType)} " +
                          $"処理:{GetProcessType(process)} " +
                          $"残額:¥{balance} " +
                          $"入:{inStation:X2} 出:{outStation:X2}");
    }

    static string GetTerminalType(int type)
    {
        return type switch
        {
            0x03 => "精算機",
            0x05 => "車載端末",
            0x07 => "券売機",
            0x08 => "券売機",
            0x09 => "入金機",
            0x16 => "改札機",
            0x17 => "簡易改札機",
            0x18 => "窓口端末",
            0x1A => "改札端末",
            0x1B => "携帯電話",
            0x1C => "乗継精算機",
            0x1D => "連絡改札機",
            0x1F => "簡易入金機",
            0x23 => "新幹線改札機",
            0xC7 => "物販端末",
            0xC8 => "自販機",
            _ => $"不明({type:X2})"
        };
    }

    static string GetProcessType(int type)
    {
        return type switch
        {
            0x01 => "運賃支払",
            0x02 => "チャージ",
            0x03 => "券購入",
            0x04 => "精算",
            0x05 => "精算(入場)",
            0x06 => "窓出",
            0x07 => "新規",
            0x08 => "控除",
            0x13 => "支払(新幹線)",
            0x14 => "入A",
            0x15 => "出A",
            0x46 => "物販",
            0x48 => "特典",
            0x49 => "入金(レジ)",
            0x4A => "物販取消",
            0x4B => "入物",
            0xC6 => "現金併用物販",
            0x84 => "他社精算",
            0x85 => "他社入精",
            _ => $"不明({type:X2})"
        };
    }
}
