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

        // INS=0x50の詳細テスト
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("INS=0x50 詳細テスト");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("\n応答 C0-03-01-6A-81 の分析:");
        Console.WriteLine("  C0 = PN532エラー応答");
        Console.WriteLine("  03 = エラーデータ長");
        Console.WriteLine("  01 = エラーカテゴリ");
        Console.WriteLine("  6A 81 = APDUエラー（Function not supported）");
        Console.WriteLine("\n→ INS=0x50はPN532に届いているが、FeliCaコマンドとして処理されていない");
        Console.WriteLine("  P1/P2やLcを変更して、正しいフォーマットを探します\n");

        TestINS0x50Variations(readerName, idm);

        // INS=0x59の詳細テスト
        Console.WriteLine("\n" + "=".PadRight(80, '='));
        Console.WriteLine("INS=0x59 詳細テスト");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("\n応答 67-00 (Wrong length) の分析:");
        Console.WriteLine("  Lcの値が期待値と異なる可能性");
        Console.WriteLine("  または、FeliCaコマンドの構造が間違っている可能性");
        Console.WriteLine("\n→ Lc、Le、コマンド構造を変更してテストします\n");

        TestINS0x59Variations(readerName, idm);
    }

    static void TestINS0x50Variations(string readerName, byte[] idm)
    {
        var testCases = new[]
        {
            // P1/P2の変更
            new { P1 = (byte)0x00, P2 = (byte)0x00, Lc = -1, Le = (byte?)0x00, Name = "デフォルト" },
            new { P1 = (byte)0x01, P2 = (byte)0x00, Lc = -1, Le = (byte?)0x00, Name = "P1=01" },
            new { P1 = (byte)0x00, P2 = (byte)0x01, Lc = -1, Le = (byte?)0x00, Name = "P2=01" },
            new { P1 = (byte)0x00, P2 = (byte)0x06, Lc = -1, Le = (byte?)0x00, Name = "P2=06 (コマンドコード)" },

            // Lcを0にする（データなし）
            new { P1 = (byte)0x00, P2 = (byte)0x00, Lc = 0, Le = (byte?)0x00, Name = "Lc=0" },

            // Leを変更
            new { P1 = (byte)0x00, P2 = (byte)0x00, Lc = -1, Le = (byte?)null, Name = "Leなし" },
            new { P1 = (byte)0x00, P2 = (byte)0x00, Lc = -1, Le = (byte?)0x1D, Name = "Le=0x1D (29バイト)" },
            new { P1 = (byte)0x00, P2 = (byte)0x00, Lc = -1, Le = (byte?)0x20, Name = "Le=0x20 (32バイト)" },
        };

        foreach (var test in testCases)
        {
            Console.WriteLine($"\n{test.Name}:");
            TestReadCustom(readerName, idm, 0x50, test.P1, test.P2, test.Lc, test.Le, SERVICE_CODE_HISTORY);
        }
    }

    static void TestINS0x59Variations(string readerName, byte[] idm)
    {
        var testCases = new[]
        {
            // Lcの値を変更（FeliCaコマンド長 + 1など）
            new { P1 = (byte)0x00, P2 = (byte)0x00, LcOffset = 0, Le = (byte?)0x00, Name = "Lc=FeliCaコマンド長" },
            new { P1 = (byte)0x00, P2 = (byte)0x00, LcOffset = 1, Le = (byte?)0x00, Name = "Lc=FeliCaコマンド長+1" },
            new { P1 = (byte)0x00, P2 = (byte)0x00, LcOffset = -1, Le = (byte?)0x00, Name = "Lc=FeliCaコマンド長-1" },

            // Leなし
            new { P1 = (byte)0x00, P2 = (byte)0x00, LcOffset = 0, Le = (byte?)null, Name = "Leなし" },
            new { P1 = (byte)0x00, P2 = (byte)0x00, LcOffset = 1, Le = (byte?)null, Name = "Lc+1, Leなし" },

            // Leを大きくする
            new { P1 = (byte)0x00, P2 = (byte)0x00, LcOffset = 0, Le = (byte?)0x1D, Name = "Le=0x1D" },
            new { P1 = (byte)0x00, P2 = (byte)0x00, LcOffset = 0, Le = (byte?)0xFF, Name = "Le=0xFF" },

            // P1/P2を変更
            new { P1 = (byte)0x01, P2 = (byte)0x00, LcOffset = 0, Le = (byte?)0x00, Name = "P1=01" },
            new { P1 = (byte)0x00, P2 = (byte)0x01, LcOffset = 0, Le = (byte?)0x00, Name = "P2=01" },
        };

        foreach (var test in testCases)
        {
            Console.WriteLine($"\n{test.Name}:");
            TestReadCustomWithLcOffset(readerName, idm, 0x59, test.P1, test.P2, test.LcOffset, test.Le, SERVICE_CODE_HISTORY);
        }
    }

    static void TestReadCustom(string readerName, byte[] idm, byte ins, byte p1, byte p2,
                               int lcOverride, byte? le, ushort serviceCode)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            try
            {
                var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);

                try
                {
                    List<byte> felicaCmd = new List<byte>();
                    felicaCmd.Add(0x06);
                    felicaCmd.AddRange(idm);
                    felicaCmd.Add(0x01);
                    felicaCmd.Add((byte)(serviceCode & 0xFF));
                    felicaCmd.Add((byte)(serviceCode >> 8));
                    felicaCmd.Add(0x01);
                    felicaCmd.Add(0x80);
                    felicaCmd.Add(0x00);

                    List<byte> cmd = new List<byte>();
                    cmd.Add(0xFF);
                    cmd.Add(ins);
                    cmd.Add(p1);
                    cmd.Add(p2);

                    if (lcOverride >= 0)
                    {
                        cmd.Add((byte)lcOverride);
                    }
                    else
                    {
                        cmd.Add((byte)felicaCmd.Count);
                    }

                    cmd.AddRange(felicaCmd);

                    if (le.HasValue)
                    {
                        cmd.Add(le.Value);
                    }

                    Console.WriteLine($"  送信: {BitConverter.ToString(cmd.ToArray())}");

                    var response = new byte[256];
                    int length = reader.Transmit(cmd.ToArray(), response);

                    Console.WriteLine($"  応答({length}): {BitConverter.ToString(response, 0, length)}");

                    if (length >= 2)
                    {
                        byte sw1 = response[length - 2];
                        byte sw2 = response[length - 1];
                        Console.WriteLine($"  SW: {sw1:X2} {sw2:X2}");

                        if (sw1 == 0x90 && sw2 == 0x00)
                        {
                            AnalyzeSuccessResponse(response, length);
                        }
                    }
                }
                finally
                {
                    reader.Disconnect(SCardReaderDisposition.Leave);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  例外: {ex.Message}");
            }
        }

        System.Threading.Thread.Sleep(100);
    }

    static void TestReadCustomWithLcOffset(string readerName, byte[] idm, byte ins, byte p1, byte p2,
                                           int lcOffset, byte? le, ushort serviceCode)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            try
            {
                var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);

                try
                {
                    List<byte> felicaCmd = new List<byte>();
                    felicaCmd.Add(0x06);
                    felicaCmd.AddRange(idm);
                    felicaCmd.Add(0x01);
                    felicaCmd.Add((byte)(serviceCode & 0xFF));
                    felicaCmd.Add((byte)(serviceCode >> 8));
                    felicaCmd.Add(0x01);
                    felicaCmd.Add(0x80);
                    felicaCmd.Add(0x00);

                    List<byte> cmd = new List<byte>();
                    cmd.Add(0xFF);
                    cmd.Add(ins);
                    cmd.Add(p1);
                    cmd.Add(p2);
                    cmd.Add((byte)(felicaCmd.Count + lcOffset));
                    cmd.AddRange(felicaCmd);

                    if (le.HasValue)
                    {
                        cmd.Add(le.Value);
                    }

                    Console.WriteLine($"  送信: {BitConverter.ToString(cmd.ToArray())}");

                    var response = new byte[256];
                    int length = reader.Transmit(cmd.ToArray(), response);

                    Console.WriteLine($"  応答({length}): {BitConverter.ToString(response, 0, length)}");

                    if (length >= 2)
                    {
                        byte sw1 = response[length - 2];
                        byte sw2 = response[length - 1];
                        Console.WriteLine($"  SW: {sw1:X2} {sw2:X2}");

                        if (sw1 == 0x90 && sw2 == 0x00)
                        {
                            AnalyzeSuccessResponse(response, length);
                        }
                    }
                }
                finally
                {
                    reader.Disconnect(SCardReaderDisposition.Leave);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  例外: {ex.Message}");
            }
        }

        System.Threading.Thread.Sleep(100);
    }

    static void AnalyzeSuccessResponse(byte[] response, int length)
    {
        int dataLength = length - 2;
        Console.WriteLine($"  ✓ SW=90 00 成功");
        Console.WriteLine($"  データ長: {dataLength}バイト");

        if (dataLength == 0)
        {
            Console.WriteLine($"  → データなし（コマンドは受理されたが応答データなし）");
            return;
        }

        Console.WriteLine($"  データ: {BitConverter.ToString(response, 0, dataLength)}");
        Console.WriteLine($"  データ詳細:");

        for (int i = 0; i < dataLength && i < 32; i++)
        {
            Console.Write($"    [{i:D2}] 0x{response[i]:X2}");
            if (response[i] >= 0x20 && response[i] <= 0x7E)
            {
                Console.Write($" ('{(char)response[i]}')");
            }
            Console.WriteLine();
        }

        // 応答パターンの判定
        if (dataLength >= 2)
        {
            byte b0 = response[0];
            byte b1 = response[1];

            Console.WriteLine($"\n  応答パターン判定:");

            // パターン1: PN532エラー応答
            if (b0 == 0xC0)
            {
                Console.WriteLine($"    → PN532エラー応答");
                Console.WriteLine($"       エラーコード長: {b1}");
                if (dataLength >= 4)
                {
                    Console.WriteLine($"       エラー内容: {response[2]:X2} {response[3]:X2}");
                }
            }
            // パターン2: FeliCa応答
            else if (b1 == 0x07)
            {
                Console.WriteLine($"    → FeliCa Read応答の可能性");
                ParsePotentialFeliCaResponse(response, dataLength);
            }
            // パターン3: コマンドエコー
            else if (b0 == 0x06)
            {
                Console.WriteLine($"    → コマンドエコーバック（未処理）");
            }
            // パターン4: その他
            else
            {
                Console.WriteLine($"    → 不明な応答形式");
                Console.WriteLine($"       第1バイト: 0x{b0:X2}");
                Console.WriteLine($"       第2バイト: 0x{b1:X2}");
            }
        }
    }

    static void ParsePotentialFeliCaResponse(byte[] response, int dataLength)
    {
        try
        {
            int pos = 0;
            byte respLen = response[pos++];
            byte respCode = response[pos++];

            Console.WriteLine($"       応答長: {respLen}");
            Console.WriteLine($"       応答コード: {respCode:X2}");

            if (dataLength >= pos + 8)
            {
                byte[] idm = new byte[8];
                Array.Copy(response, pos, idm, 0, 8);
                pos += 8;
                Console.WriteLine($"       IDm: {BitConverter.ToString(idm)}");
            }

            if (dataLength >= pos + 2)
            {
                byte sf1 = response[pos++];
                byte sf2 = response[pos++];
                Console.WriteLine($"       ステータスフラグ: {sf1:X2} {sf2:X2}");

                if (sf1 == 0x00 && sf2 == 0x00)
                {
                    if (dataLength >= pos + 1)
                    {
                        byte blockCount = response[pos++];
                        Console.WriteLine($"       ブロック数: {blockCount}");

                        if (dataLength >= pos + 16)
                        {
                            byte[] blockData = new byte[16];
                            Array.Copy(response, pos, blockData, 0, 16);
                            Console.WriteLine($"       ✓✓✓ ブロックデータ取得成功！ ✓✓✓");
                            Console.WriteLine($"       データ: {BitConverter.ToString(blockData)}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"       解析エラー: {ex.Message}");
        }
    }

    static PollingResult ExecutePolling(ICardReader reader, byte ins)
    {
        try
        {
            List<byte> felicaCmd = new List<byte>();
            felicaCmd.Add(0x00);
            felicaCmd.Add(0xFF);
            felicaCmd.Add(0xFF);
            felicaCmd.Add(0x01);
            felicaCmd.Add(0x00);

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
