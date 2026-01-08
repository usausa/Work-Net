using System;
using System.Linq;
using System.Collections.Generic;
using PCSC;
using PCSC.Monitoring;

class SuicaReader
{
    const ushort SERVICE_CODE_HISTORY = 0x090F;
    const ushort SERVICE_CODE_BASIC = 0x008B;

    static void Main(string[] args)
    {
        var monitor = MonitorFactory.Instance.Create(SCardScope.System);
        monitor.CardInserted += (sender, e) =>
        {
            Console.WriteLine($"\n[カード検出] {e.ReaderName}");
            Console.WriteLine(new string('=', 60));
            DetailedTest(e.ReaderName);
        };

        monitor.CardRemoved += (sender, e) =>
        {
            Console.WriteLine($"\n[カード除去] {e.ReaderName}");
        };

        monitor.MonitorException += (sender, e) =>
        {
            Console.WriteLine($"モニターエラー: {e.Message}");
        };

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

    static void DetailedTest(string readerName)
    {
        byte[] idm = null;
        byte[] pmm = null;

        // Polling実行
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            try
            {
                var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);
                try
                {
                    var pollingResult = ExecutePolling(reader, 0xFE);
                    if (pollingResult.Success)
                    {
                        idm = pollingResult.IDm;
                        pmm = pollingResult.PMm;
                        Console.WriteLine($"IDm: {BitConverter.ToString(idm).Replace("-", "")}");
                        Console.WriteLine($"PMm: {BitConverter.ToString(pmm).Replace("-", "")}\n");
                    }
                    else
                    {
                        Console.WriteLine("Polling失敗");
                        return;
                    }
                }
                finally
                {
                    reader.Disconnect(SCardReaderDisposition.Leave);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Polling例外: {ex.Message}");
                return;
            }
        }

        System.Threading.Thread.Sleep(200);

        // 新しいテスト
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Web USB形式に基づくテスト");
        Console.WriteLine("=".PadRight(80, '='));

        TestWebUSBFormat(readerName, idm);
    }

    static void TestWebUSBFormat(string readerName, byte[] idm)
    {
        Console.WriteLine("\n【テスト1】P2=01 + 標準Lc");
        TestReadWithP2_01(readerName, idm, SERVICE_CODE_HISTORY);

        Console.WriteLine("\n【テスト2】P2=01 + 拡張Lc（00 00 XX）");
        TestReadWithExtendedLc(readerName, idm, SERVICE_CODE_HISTORY);

        Console.WriteLine("\n【テスト3】PN532 InDataExchange形式");
        TestReadWithInDataExchange(readerName, idm, SERVICE_CODE_HISTORY);

        Console.WriteLine("\n【テスト4】INS=0x40（Direct Transmit）");
        TestReadWithINS0x40(readerName, idm, SERVICE_CODE_HISTORY);

        Console.WriteLine("\n【テスト5】履歴サービス（0x090F）複数ブロック");
        TestReadMultipleBlocks(readerName, idm, SERVICE_CODE_HISTORY);

        Console.WriteLine("\n【テスト6】基本情報サービス（0x008B）");
        TestReadWithP2_01(readerName, idm, SERVICE_CODE_BASIC);
    }

    static void TestReadWithP2_01(string readerName, byte[] idm, ushort serviceCode)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            try
            {
                var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);
                try
                {
                    // FeliCa Read Without Encryptionコマンド
                    List<byte> felicaCmd = new List<byte>();
                    felicaCmd.Add(0x06);  // コマンドコード
                    felicaCmd.AddRange(idm);  // IDm (8バイト)
                    felicaCmd.Add(0x01);  // サービス数
                    felicaCmd.Add((byte)(serviceCode & 0xFF));  // サービスコード（リトルエンディアン）
                    felicaCmd.Add((byte)(serviceCode >> 8));
                    felicaCmd.Add(0x01);  // ブロック数
                    felicaCmd.Add(0x80);  // ブロックリスト要素（2バイトフォーマット）
                    felicaCmd.Add(0x00);  // ブロック番号

                    // APDU構築（P2=01）
                    List<byte> cmd = new List<byte>();
                    cmd.Add(0xFF);  // CLA
                    cmd.Add(0x50);  // INS
                    cmd.Add(0x00);  // P1
                    cmd.Add(0x01);  // P2 = 01 ★重要★
                    cmd.Add((byte)felicaCmd.Count);  // Lc
                    cmd.AddRange(felicaCmd);
                    cmd.Add(0x00);  // Le

                    Console.WriteLine($"送信: {BitConverter.ToString(cmd.ToArray())}");

                    var response = new byte[256];
                    int length = reader.Transmit(cmd.ToArray(), response);

                    Console.WriteLine($"応答({length}): {BitConverter.ToString(response, 0, length)}");
                    AnalyzeResponse(response, length);
                }
                finally
                {
                    reader.Disconnect(SCardReaderDisposition.Leave);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"例外: {ex.Message}");
            }
        }
        System.Threading.Thread.Sleep(100);
    }

    static void TestReadWithExtendedLc(string readerName, byte[] idm, ushort serviceCode)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            try
            {
                var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);
                try
                {
                    // FeliCa Read Without Encryptionコマンド
                    List<byte> felicaCmd = new List<byte>();
                    felicaCmd.Add(0x06);
                    felicaCmd.AddRange(idm);
                    felicaCmd.Add(0x01);
                    felicaCmd.Add((byte)(serviceCode & 0xFF));
                    felicaCmd.Add((byte)(serviceCode >> 8));
                    felicaCmd.Add(0x01);
                    felicaCmd.Add(0x80);
                    felicaCmd.Add(0x00);

                    // 拡張APDUフォーマット
                    List<byte> cmd = new List<byte>();
                    cmd.Add(0xFF);
                    cmd.Add(0x50);
                    cmd.Add(0x00);
                    cmd.Add(0x01);  // P2 = 01
                    cmd.Add(0x00);  // 拡張Lc開始
                    cmd.Add(0x00);
                    cmd.Add((byte)felicaCmd.Count);  // 実際の長さ
                    cmd.AddRange(felicaCmd);
                    cmd.Add(0x00);  // Le

                    Console.WriteLine($"送信: {BitConverter.ToString(cmd.ToArray())}");

                    var response = new byte[256];
                    int length = reader.Transmit(cmd.ToArray(), response);

                    Console.WriteLine($"応答({length}): {BitConverter.ToString(response, 0, length)}");
                    AnalyzeResponse(response, length);
                }
                finally
                {
                    reader.Disconnect(SCardReaderDisposition.Leave);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"例外: {ex.Message}");
            }
        }
        System.Threading.Thread.Sleep(100);
    }

    static void TestReadWithInDataExchange(string readerName, byte[] idm, ushort serviceCode)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            try
            {
                var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);
                try
                {
                    // FeliCa Read Without Encryptionコマンド
                    List<byte> felicaCmd = new List<byte>();
                    felicaCmd.Add(0x06);
                    felicaCmd.AddRange(idm);
                    felicaCmd.Add(0x01);
                    felicaCmd.Add((byte)(serviceCode & 0xFF));
                    felicaCmd.Add((byte)(serviceCode >> 8));
                    felicaCmd.Add(0x01);
                    felicaCmd.Add(0x80);
                    felicaCmd.Add(0x00);

                    // PN532 InDataExchangeでラップ
                    List<byte> pn532Cmd = new List<byte>();
                    pn532Cmd.Add(0xD4);  // PN532コマンド
                    pn532Cmd.Add(0x40);  // InDataExchange
                    pn532Cmd.Add(0x01);  // Tg (ターゲット番号)
                    pn532Cmd.AddRange(felicaCmd);

                    // APDU構築
                    List<byte> cmd = new List<byte>();
                    cmd.Add(0xFF);
                    cmd.Add(0x00);  // INS = 00 (Direct Transmit)
                    cmd.Add(0x00);
                    cmd.Add(0x00);
                    cmd.Add((byte)pn532Cmd.Count);
                    cmd.AddRange(pn532Cmd);
                    cmd.Add(0x00);

                    Console.WriteLine($"送信: {BitConverter.ToString(cmd.ToArray())}");

                    var response = new byte[256];
                    int length = reader.Transmit(cmd.ToArray(), response);

                    Console.WriteLine($"応答({length}): {BitConverter.ToString(response, 0, length)}");
                    AnalyzeResponse(response, length);
                }
                finally
                {
                    reader.Disconnect(SCardReaderDisposition.Leave);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"例外: {ex.Message}");
            }
        }
        System.Threading.Thread.Sleep(100);
    }

    static void TestReadWithINS0x40(string readerName, byte[] idm, ushort serviceCode)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            try
            {
                var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);
                try
                {
                    // FeliCa Read Without Encryptionコマンド
                    List<byte> felicaCmd = new List<byte>();
                    felicaCmd.Add(0x06);
                    felicaCmd.AddRange(idm);
                    felicaCmd.Add(0x01);
                    felicaCmd.Add((byte)(serviceCode & 0xFF));
                    felicaCmd.Add((byte)(serviceCode >> 8));
                    felicaCmd.Add(0x01);
                    felicaCmd.Add(0x80);
                    felicaCmd.Add(0x00);

                    // INS=0x40でテスト
                    List<byte> cmd = new List<byte>();
                    cmd.Add(0xFF);
                    cmd.Add(0x40);  // INS = 40
                    cmd.Add(0x00);
                    cmd.Add(0x00);
                    cmd.Add((byte)felicaCmd.Count);
                    cmd.AddRange(felicaCmd);
                    cmd.Add(0x00);

                    Console.WriteLine($"送信: {BitConverter.ToString(cmd.ToArray())}");

                    var response = new byte[256];
                    int length = reader.Transmit(cmd.ToArray(), response);

                    Console.WriteLine($"応答({length}): {BitConverter.ToString(response, 0, length)}");
                    AnalyzeResponse(response, length);
                }
                finally
                {
                    reader.Disconnect(SCardReaderDisposition.Leave);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"例外: {ex.Message}");
            }
        }
        System.Threading.Thread.Sleep(100);
    }

    static void TestReadMultipleBlocks(string readerName, byte[] idm, ushort serviceCode)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            try
            {
                var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);
                try
                {
                    // 複数ブロック読み取り（ブロック0-3）
                    List<byte> felicaCmd = new List<byte>();
                    felicaCmd.Add(0x06);
                    felicaCmd.AddRange(idm);
                    felicaCmd.Add(0x01);  // サービス数
                    felicaCmd.Add((byte)(serviceCode & 0xFF));
                    felicaCmd.Add((byte)(serviceCode >> 8));
                    felicaCmd.Add(0x04);  // ブロック数 = 4
                    // ブロックリスト
                    felicaCmd.Add(0x80); felicaCmd.Add(0x00);  // ブロック0
                    felicaCmd.Add(0x80); felicaCmd.Add(0x01);  // ブロック1
                    felicaCmd.Add(0x80); felicaCmd.Add(0x02);  // ブロック2
                    felicaCmd.Add(0x80); felicaCmd.Add(0x03);  // ブロック3

                    List<byte> cmd = new List<byte>();
                    cmd.Add(0xFF);
                    cmd.Add(0x50);
                    cmd.Add(0x00);
                    cmd.Add(0x01);  // P2 = 01
                    cmd.Add((byte)felicaCmd.Count);
                    cmd.AddRange(felicaCmd);
                    cmd.Add(0x00);

                    Console.WriteLine($"送信: {BitConverter.ToString(cmd.ToArray())}");

                    var response = new byte[256];
                    int length = reader.Transmit(cmd.ToArray(), response);

                    Console.WriteLine($"応答({length}): {BitConverter.ToString(response, 0, length)}");
                    AnalyzeResponse(response, length);
                }
                finally
                {
                    reader.Disconnect(SCardReaderDisposition.Leave);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"例外: {ex.Message}");
            }
        }
        System.Threading.Thread.Sleep(100);
    }

    static void AnalyzeResponse(byte[] response, int length)
    {
        if (length < 2)
        {
            Console.WriteLine("応答が短すぎます");
            return;
        }

        byte sw1 = response[length - 2];
        byte sw2 = response[length - 1];
        Console.WriteLine($"SW: {sw1:X2} {sw2:X2}");

        if (sw1 == 0x90 && sw2 == 0x00)
        {
            Console.WriteLine("✓ SW=90 00 成功");
            int dataLength = length - 2;

            if (dataLength == 0)
            {
                Console.WriteLine("→ データなし");
                return;
            }

            Console.WriteLine($"データ({dataLength}バイト): {BitConverter.ToString(response, 0, dataLength)}");

            // FeliCa応答の解析
            if (dataLength >= 12 && response[1] == 0x07)
            {
                Console.WriteLine("\n✓✓✓ FeliCa Read応答を検出！ ✓✓✓");
                ParseFeliCaReadResponse(response, dataLength);
            }
            else if (response[0] == 0xC0)
            {
                Console.WriteLine($"→ PN532エラー応答: {response[2]:X2} {response[3]:X2}");
            }
            else
            {
                Console.WriteLine("→ 不明な応答形式");
            }
        }
        else
        {
            Console.WriteLine($"✗ エラー応答: SW={sw1:X2} {sw2:X2}");
        }
    }

    static void ParseFeliCaReadResponse(byte[] response, int dataLength)
    {
        try
        {
            int pos = 0;
            byte respLen = response[pos++];
            byte respCode = response[pos++];

            Console.WriteLine($"  応答長: {respLen}");
            Console.WriteLine($"  応答コード: 0x{respCode:X2}");

            if (dataLength >= pos + 8)
            {
                byte[] idm = new byte[8];
                Array.Copy(response, pos, idm, 0, 8);
                pos += 8;
                Console.WriteLine($"  IDm: {BitConverter.ToString(idm)}");
            }

            if (dataLength >= pos + 2)
            {
                byte sf1 = response[pos++];
                byte sf2 = response[pos++];
                Console.WriteLine($"  ステータスフラグ: {sf1:X2} {sf2:X2}");

                if (sf1 == 0x00 && sf2 == 0x00)
                {
                    Console.WriteLine("  → 読み取り成功");

                    if (dataLength >= pos + 1)
                    {
                        byte blockCount = response[pos++];
                        Console.WriteLine($"  ブロック数: {blockCount}");

                        for (int i = 0; i < blockCount && dataLength >= pos + 16; i++)
                        {
                            byte[] blockData = new byte[16];
                            Array.Copy(response, pos, blockData, 0, 16);
                            pos += 16;

                            Console.WriteLine($"\n  ★★★ ブロック{i}データ取得成功！ ★★★");
                            Console.WriteLine($"  {BitConverter.ToString(blockData)}");

                            // Suica履歴データの場合の解析
                            if (i == 0)
                            {
                                ParseSuicaHistoryBlock(blockData);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"  → FeliCaエラー: SF1={sf1:X2}, SF2={sf2:X2}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  解析エラー: {ex.Message}");
        }
    }

    static void ParseSuicaHistoryBlock(byte[] block)
    {
        try
        {
            Console.WriteLine("\n  === Suica履歴データ解析 ===");

            // 端末種
            byte termType = block[0];
            Console.WriteLine($"  端末種: 0x{termType:X2}");

            // 処理
            byte procType = block[1];
            Console.WriteLine($"  処理: 0x{procType:X2}");

            // 日付（7ビット年 + 4ビット月 + 5ビット日）
            ushort dateData = (ushort)((block[4] << 8) | block[5]);
            int year = ((dateData >> 9) & 0x7F) + 2000;
            int month = (dateData >> 5) & 0x0F;
            int day = dateData & 0x1F;
            Console.WriteLine($"  日付: {year}/{month:D2}/{day:D2}");

            // 残額
            int balance = (block[10] << 8) | block[11];
            Console.WriteLine($"  残額: ¥{balance}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Suicaデータ解析エラー: {ex.Message}");
        }
    }

    static PollingResult ExecutePolling(ICardReader reader, byte ins)
    {
        try
        {
            List<byte> felicaCmd = new List<byte>();
            felicaCmd.Add(0x00);  // システムコード
            felicaCmd.Add(0xFF);
            felicaCmd.Add(0xFF);
            felicaCmd.Add(0x01);  // リクエストコード
            felicaCmd.Add(0x00);  // タイムスロット

            List<byte> cmd = new List<byte>();
            cmd.Add(0xFF);
            cmd.Add(ins);
            cmd.Add(0x00);
            cmd.Add(0x00);
            cmd.Add((byte)felicaCmd.Count);
            cmd.AddRange(felicaCmd);
            cmd.Add(0x00);

            var response = new byte[256];
            int length = reader.Transmit(cmd.ToArray(), response);

            if (length < 2 || response[length - 2] != 0x90 || response[length - 1] != 0x00)
            {
                return new PollingResult { Success = false };
            }

            int dataLength = length - 2;
            if (dataLength < 18 || response[0] != 0x01)
            {
                return new PollingResult { Success = false };
            }

            byte[] idm = new byte[8];
            Array.Copy(response, 1, idm, 0, 8);

            byte[] pmm = new byte[8];
            Array.Copy(response, 9, pmm, 0, 8);

            return new PollingResult { Success = true, IDm = idm, PMm = pmm };
        }
        catch
        {
            return new PollingResult { Success = false };
        }
    }

    class PollingResult
    {
        public bool Success { get; set; }
        public byte[] IDm { get; set; }
        public byte[] PMm { get; set; }
    }
}
