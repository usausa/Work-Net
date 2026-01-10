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

            //if (ReadTest(reader))
            //{
            //    return;
            //}

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

                ReadTest(reader, idm);

                //ReadHistory(reader, idm);
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

    static bool ReadTest(ICardReader reader)
    {
        try
        {
            //// End Transeparent Session
            //RequestResponse(reader, [0xFF, 0x50, 0x00, 0x00, 0x02, 0x82, 0x00, 0x00]);
            //// Start Transeparent Session
            //RequestResponse(reader, [0xFF, 0x50, 0x00, 0x00, 0x02, 0x81, 0x00, 0x00]);
            //// Turn Off RF
            //RequestResponse(reader, [0xFF, 0x50, 0x00, 0x00, 0x02, 0x83, 0x00, 0x00]);
            //// Turn On RF
            //RequestResponse(reader, [0xFF, 0x50, 0x00, 0x00, 0x02, 0x84, 0x00, 0x00]);

            // Send some command
            RequestResponse(reader, [
                0xFF, 0x50, 0x00, 0x01, 0x00, 0x00, 0x11, 0x5F, 0x46, 0x04, 0xA0, 0x86, 0x01, 0x00, 0x95, 0x82, 0x00, 0x06, 0x06, 0x00, 0xFF, 0xFF, 0x01, 0x00, 0x00, 0x00, 0x00]);

            //// Turn Off RF
            //RequestResponse(reader, [0xFF, 0x50, 0x00, 0x00, 0x02, 0x83, 0x00, 0x00]);
            // End Transeparent Session
            //RequestResponse(reader, [0xFF, 0x50, 0x00, 0x00, 0x02, 0x82, 0x00, 0x00]);
        }
        catch (Exception ex)
        {
            Console.Write(ex);
        }

        return true;
    }

    static void RequestResponse(ICardReader reader, byte[] cmd)
    {
        Console.WriteLine($"[デバッグ] 送信({cmd}): {BitConverter.ToString(cmd)}");
        var response = new byte[256];
        int length = reader.Transmit(cmd, response);
        Console.WriteLine($"[デバッグ] 応答({length}): {BitConverter.ToString(response, 0, length)}");
    }

    static Tuple<byte[], byte[]> ExecutePolling(ICardReader reader)
    {
        try
        {
            Console.WriteLine("[デバッグ] Pollingコマンドを送信 (システムコード: FFFF)");

            List<byte> cmd = new List<byte>();
            cmd.Add(0xFF);  // CLA
            cmd.Add(0xFE);  // INS
            cmd.Add(0x00);  // P1
            cmd.Add(0x00);  // P2

            List<byte> felicaCmd = new List<byte>();
            felicaCmd.Add(0x00);  // コマンドコード: Polling
            felicaCmd.Add(0xFF);  // システムコード上位
            felicaCmd.Add(0xFF);  // システムコード下位
            felicaCmd.Add(0x01);  // リクエストコード
            felicaCmd.Add(0x00);  // タイムスロット

            cmd.Add((byte)felicaCmd.Count);
            cmd.AddRange(felicaCmd);
            //cmd.Add(0x00);

            Console.WriteLine($"[デバッグ] 送信: {BitConverter.ToString(cmd.ToArray())}");
            var response = new byte[256];
            int length = reader.Transmit(cmd.ToArray(), response);
            Console.WriteLine($"[デバッグ] 応答({length}): {BitConverter.ToString(response, 0, length)}");

            // TODO

            // ステータスワードの確認
            if (length < 2 || response[length - 2] != 0x90 || response[length - 1] != 0x00)
            {
                Console.WriteLine("[エラー] ステータスエラー");
                return null;
            }

            int pos = 0;
            byte responseCode = response[pos++];

            Console.WriteLine($"[デバッグ] 応答コード: {responseCode:X2}");

            if (responseCode != 0x01)
            {
                Console.WriteLine($"[エラー] Polling応答コードが不正: {responseCode:X2}");
                return null;
            }

            byte[] idm = new byte[8];
            Array.Copy(response, pos, idm, 0, 8);
            pos += 8;
            Console.WriteLine($"[デバッグ] IDm抽出: {BitConverter.ToString(idm)}");

            byte[] pmm = new byte[8];
            Array.Copy(response, pos, pmm, 0, 8);
            pos += 8;
            Console.WriteLine($"[デバッグ] PMm抽出: {BitConverter.ToString(pmm)}");

            if (pos + 2 <= length - 2)
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

    static void ReadTest(ICardReader reader, byte[] idm)
    {
        List<byte> felicaCmd = new List<byte>();
        felicaCmd.Add(0x04);
        felicaCmd.AddRange(idm);

        List<byte> cmd = new List<byte>();

        // communicateThruEX
        cmd.Add(0xFF);
        cmd.Add(0x50);
        cmd.Add(0x00);
        cmd.Add(0x01);
        cmd.Add(0x00);
        var dataLength = felicaCmd.Count + 11;
        cmd.Add((byte)((dataLength >> 8) & 0xFF));
        cmd.Add((byte)(dataLength & 0xFF));

        // FeliCa Lite-S
        cmd.Add(0x5F);
        cmd.Add(0x46);
        cmd.Add(0x04);

        cmd.Add(0x80); // Timeout microseconds (MSB)?
        cmd.Add(0x1A);
        cmd.Add(0x06);
        cmd.Add(0x00);

        cmd.Add(0x95);
        cmd.Add(0x82);

        cmd.Add((byte)((felicaCmd.Count >> 8) & 0xFF));
        cmd.Add((byte)(felicaCmd.Count & 0xFF));

        // FeliCa
        cmd.AddRange(felicaCmd);

        // communicateThruEX
        cmd.Add(0x00);
        cmd.Add(0x00);
        cmd.Add(0x00);

        Console.WriteLine($"[デバッグ] 送信({cmd.Count}): {BitConverter.ToString(cmd.ToArray())}");

        var response = new byte[256];
        int length = reader.Transmit(cmd.ToArray(), response);
        Console.WriteLine($"[デバッグ] 応答({length}): {BitConverter.ToString(response, 0, length)}");

    }

    //public static byte[] MakeReadWoe(byte[] idm, short serviceCode, params int[] blockNos)
    //{
    //    var command = new byte[14 + (blockNos.Length * 2)];
    //    command[0] = (byte)command.Length;
    //    command[1] = 0x06;
    //    Buffer.BlockCopy(idm, 0, command, 2, idm.Length);
    //    command[10] = 1;
    //    command[11] = (byte)(serviceCode & 0xff);
    //    command[12] = (byte)(serviceCode >> 8);
    //    command[13] = (byte)blockNos.Length;
    //    for (var i = 0; i < blockNos.Length; i++)
    //    {
    //        var offset = 14 + (i * 2);
    //        command[offset] = 0x80;
    //        command[offset + 1] = (byte)blockNos[i];
    //    }

    //    return command;
    //}


    static void ReadHistory(ICardReader reader, byte[] idm)
    {
        Console.WriteLine("履歴情報:");
        Console.WriteLine(new string('-', 60));

        byte[] blockData = ReadBlock(reader, idm, SERVICE_CODE_HISTORY, 0, 1, 2, 3, 4, 5, 6, 7);
    }

    // APDU
    // https://gist.github.com/hemantvallabh/d24d71a933e7319727cd3daa50ad9f2c
    // **-FE-**-** : FeliCaコマンド送信(?), Vendor-specific?
    // FF-50 :
    // FF-59 :

    // 先頭の FF は CLA を表す : https://cardwerk.com/smart-card-standard-iso7816-4-section-5-basic-organizations/?elementor-preview&1514396438071#chap5_4_1

    // SW
    // https://www.eftlab.com/knowledge-base/complete-list-of-apdu-responses
    //
    // | SW1-SW2 | 意味                            | 説明 |
    // |---------|---------------------------------|------|
    // | `90 00` | Success                         | 正常終了 |
    // | `6A 81` | Function not supported          | コマンドがサポートされていない |
    // | `6A 86` | Incorrect P1-P2                 | パラメータP1/P2が不正 |
    // | `6A 87` | Lc inconsistent with P1-P2      | データ長が不正 |
    // | `69 85` | Conditions of use not satisfied | 使用条件が満たされていない |
    // | `69 86` | Command not allowed             | コマンドが許可されていない |
    // | `6B 00` | Wrong parameter(s) P1-P2        | パラメータP1-P2が間違っている |
    // | `67 00` | Wrong length                    | 長さが間違っている |

    // PN532 ?

    // [Command & Response]

    // <06 [XX-XX-XX-XX-XX-XX-XX-XX] 01 8B-00 01 80-00> = 15/0x0F
    //
    // [NG]
    // 存在しないトランザクションを終了しようとしました
    // FF-FE-00-00 0F <06 [XX-XX-XX-XX-XX-XX-XX-XX] 01 8B-00 01 80-00> 00
    //   An attempt was made to end a non-existent transaction.
    // FF-FE-00-00 10 <06 [XX-XX-XX-XX-XX-XX-XX-XX] 01 8B-00 01 80-00> 00
    //   An attempt was made to end a non-existent transaction.
    // FF-FE-00-00 10 <10 06 [XX-XX-XX-XX-XX-XX-XX-XX] 01 8B-00 01 80-00> 00
    //   An attempt was made to end a non-existent transaction.
    // FF-FE-00-00 10 <0F 06 [XX-XX-XX-XX-XX-XX-XX-XX] 01 8B-00 01 80-00> 00
    //   An attempt was made to end a non-existent transaction.
    // FF-FE-00-00 0F <06-XX-XX-XX-XX-XX-XX-XX-XX-01-8B-00-01-80-00>
    //   An attempt was made to end a non-existent transaction.
    //
    // コマンドがサポートされていない
    // FF-00-00-00 0F <0F-06-XX-XX-XX-XX-XX-XX-XX-XX-01-8B-00-01-80-00-00
    //   6A-81 Function not supported
    // FF-00-00-00 0F <06-XX-XX-XX-XX-XX-XX-XX-XX-01-8B-00-01-80-00-00
    //   6A-81 Function not supported

    // FF-FE-00-00 0F <0F 06 [XX-XX-XX-XX-XX-XX-XX-XX] 01 8B-00 01 80-00> 00
    //  67-00 Wrong length
    // FF-FE-00-00-0F-0F-06-XX-XX-XX-XX-XX-XX-XX-XX-01-8B-00-01-80-00
    //   An attempt was made to end a non-existent transaction.
    // FF-FE-00-00-0F-06-XX-XX-XX-XX-XX-XX-XX-XX-01-8B-00-01-80-00-00
    //   An attempt was made to end a non-existent transaction.

    // FF-50-00-00 12 D4-40-01 0F-06-XX-XX-XX-XX-XX-XX-XX-XX-01-8B-00-01-80-00 00
    //  67-00 Wrong length

    // FF-50-00-00-12-D4-40-01-06-XX-XX-XX-XX-XX-XX-XX-XX-01-8B-00-01-80-00-00
    //   C0-03-01 6A-81 90-00
    //   ?        Function not supported  APDUとしては完了？

    // FF-50-00-01 00-00-12 D4-40-01 06-XX-XX-XX-XX-XX-XX-XX-XX-01-8B-00-01-80-00-00
    //   C0-03-01 6A-81 90-00

    // 67-00 Wrong length
    // FF-59-00-00 0F 06-01-01-08-01-20-0D-DD-10-01-8B-00-01-80-00 00
    // FF-59-00-00 10 06-01-01-08-01-20-0D-DD-10-01-8B-00-01-80-00-00
    // FF-59-00-00 0F 0F-06-01-01-08-01-20-0D-DD-10-01-8B-00-01-80-00 00
    // FF-59-00-00 10 0F-06-01-01-08-01-20-0D-DD-10-01-8B-00-01-80-00 00
    // FF-59-00-00 10 10-06-01-01-08-01-20-0D-DD-10-01-8B-00-01-80-00 00
    // FF-59-00-00 00-00-0F 06-01-01-08-01-20-0D-DD-10-01-8B-00-01-80-00 00

    // Polling
    // FF-FE-00-00 05 <00-FF-FF-01-00>

    static byte[] ReadBlock(ICardReader reader, byte[] idm, ushort serviceCode, params int[] blockNos)
    {
        try
        {
            // FeliCa Read Without Encryptionコマンドを送信
            List<byte> cmd = new List<byte>();
            // TODO ?
            cmd.Add(0xFF); // CLA
            //cmd.Add(0x00); // CLA
            //cmd.Add(0xFE); // INS
            //cmd.Add(0x50); // INS
            cmd.Add(0x59); // INS
            cmd.Add(0x00); // P1
            cmd.Add(0x00); // P2
            //cmd.Add(0x01); // P2

            List<byte> felicaCmd = new List<byte>();
            felicaCmd.Add(0x06); // コマンドコード: Read Without Encryption
            felicaCmd.AddRange(idm); // IDm
            felicaCmd.Add(0x01); // サービス数
            felicaCmd.Add((byte)(serviceCode & 0xFF));
            felicaCmd.Add((byte)((serviceCode >> 8) & 0xFF));

            felicaCmd.Add((byte)blockNos.Length); // ブロック数
            // ブロックリスト
            foreach (var blockNumber in blockNos)
            {
                felicaCmd.Add(0x80);
                felicaCmd.Add((byte)blockNumber);
            }

            // TODO ?
            //cmd.Add(0x00);
            //cmd.Add(0x00);
            //cmd.Add((byte)(felicaCmd.Count + 3));
            // TODO ?
            //cmd.Add(0xD4); // Host to PN532
            //cmd.Add(0x40); // InDataExchange
            //cmd.Add(0x01); // Target number

            cmd.Add(0x00);
            cmd.Add(0x00);
            //cmd.Add((byte)(felicaCmd.Count + 1));
            //cmd.Add((byte)(felicaCmd.Count + 1));
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
