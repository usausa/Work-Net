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

        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("FeliCa LENフィールド徹底テスト");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("\nFeliCaフレーム構造:");
        Console.WriteLine("  [LEN] [CMD] [IDm(8)] [データ...]");
        Console.WriteLine("  LEN = フレーム全体のバイト数（LEN自体を含む）");
        Console.WriteLine("  Read Without Encryption: LEN=0x10 (16バイト)");
        Console.WriteLine();

        TestAllLengthVariations(readerName, idm);
    }

    static void TestAllLengthVariations(string readerName, byte[] idm)
    {
        // テスト対象のINS
        byte[] insToTest = { 0x50, 0x51, 0x53, 0x59 };

        foreach (byte ins in insToTest)
        {
            Console.WriteLine("\n" + "=".PadRight(80, '='));
            Console.WriteLine($"INS=0x{ins:X2} 全パターンテスト");
            Console.WriteLine("=".PadRight(80, '='));

            // パターンA: LENなし（生のFeliCaコマンド）
            Console.WriteLine("\n[パターンA] LENなし - 06-IDm-...");
            TestPatternA(readerName, idm, ins, SERVICE_CODE_HISTORY);

            // パターンB: LEN=0x0F（コマンド以降の長さ）
            Console.WriteLine("\n[パターンB] LEN=0x0F - 0F-06-IDm-...");
            TestPatternB(readerName, idm, ins, SERVICE_CODE_HISTORY);

            // パターンC: LEN=0x10（フレーム全体、LEN含む）
            Console.WriteLine("\n[パターンC] LEN=0x10 - 10-06-IDm-...");
            TestPatternC(readerName, idm, ins, SERVICE_CODE_HISTORY);

            // パターンD: 二重LEN 0x10-0x10
            Console.WriteLine("\n[パターンD] 二重LEN=0x10 - 10-10-06-IDm-...");
            TestPatternD(readerName, idm, ins, SERVICE_CODE_HISTORY);

            // パターンE: 二重LEN 0x10-0x0F
            Console.WriteLine("\n[パターンE] 二重LEN 10-0F - 10-0F-06-IDm-...");
            TestPatternE(readerName, idm, ins, SERVICE_CODE_HISTORY);

            // パターンF: 二重LEN 0x0F-0x10
            Console.WriteLine("\n[パターンF] 二重LEN 0F-10 - 0F-10-06-IDm-...");
            TestPatternF(readerName, idm, ins, SERVICE_CODE_HISTORY);

            // パターンG: Lc調整（Lc=データ長+1）
            Console.WriteLine("\n[パターンG] Lc=データ長+1");
            TestPatternG(readerName, idm, ins, SERVICE_CODE_HISTORY);

            // パターンH: Lc調整（Lc=データ長+2）
            Console.WriteLine("\n[パターンH] Lc=データ長+2");
            TestPatternH(readerName, idm, ins, SERVICE_CODE_HISTORY);
        }

        // 成功したパターンで基本情報サービスもテスト
        Console.WriteLine("\n\n" + "=".PadRight(80, '='));
        Console.WriteLine("基本情報サービス（0x008B）テスト");
        Console.WriteLine("=".PadRight(80, '='));

        foreach (byte ins in insToTest)
        {
            Console.WriteLine($"\n[INS=0x{ins:X2}] パターンC (10-06-...)");
            TestPatternC(readerName, idm, ins, SERVICE_CODE_BASIC);

            Console.WriteLine($"\n[INS=0x{ins:X2}] パターンD (10-10-06-...)");
            TestPatternD(readerName, idm, ins, SERVICE_CODE_BASIC);
        }
    }

    // パターンA: LENなし
    static void TestPatternA(string readerName, byte[] idm, byte ins, ushort serviceCode)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            try
            {
                var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);
                try
                {
                    List<byte> data = new List<byte>();
                    data.Add(0x06);  // CMD
                    data.AddRange(idm);
                    data.Add(0x01);
                    data.Add((byte)(serviceCode & 0xFF));
                    data.Add((byte)(serviceCode >> 8));
                    data.Add(0x01);
                    data.Add(0x80);
                    data.Add(0x00);

                    List<byte> cmd = new List<byte>();
                    cmd.Add(0xFF);
                    cmd.Add(ins);
                    cmd.Add(0x00);
                    cmd.Add(0x00);
                    cmd.Add((byte)data.Count);  // Lc = 15
                    cmd.AddRange(data);
                    cmd.Add(0x00);

                    Console.WriteLine($"  Lc={data.Count} (0x{data.Count:X2})");
                    Console.WriteLine($"  送信: {BitConverter.ToString(cmd.ToArray())}");

                    var response = new byte[256];
                    int length = reader.Transmit(cmd.ToArray(), response);

                    Console.WriteLine($"  応答({length}): {BitConverter.ToString(response, 0, length)}");
                    AnalyzeResponse(response, length);
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

    // パターンB: LEN=0x0F
    static void TestPatternB(string readerName, byte[] idm, byte ins, ushort serviceCode)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            try
            {
                var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);
                try
                {
                    List<byte> data = new List<byte>();
                    data.Add(0x0F);  // LEN（コマンド以降）
                    data.Add(0x06);  // CMD
                    data.AddRange(idm);
                    data.Add(0x01);
                    data.Add((byte)(serviceCode & 0xFF));
                    data.Add((byte)(serviceCode >> 8));
                    data.Add(0x01);
                    data.Add(0x80);
                    data.Add(0x00);

                    List<byte> cmd = new List<byte>();
                    cmd.Add(0xFF);
                    cmd.Add(ins);
                    cmd.Add(0x00);
                    cmd.Add(0x00);
                    cmd.Add((byte)data.Count);  // Lc = 16
                    cmd.AddRange(data);
                    cmd.Add(0x00);

                    Console.WriteLine($"  Lc={data.Count} (0x{data.Count:X2})");
                    Console.WriteLine($"  送信: {BitConverter.ToString(cmd.ToArray())}");

                    var response = new byte[256];
                    int length = reader.Transmit(cmd.ToArray(), response);

                    Console.WriteLine($"  応答({length}): {BitConverter.ToString(response, 0, length)}");
                    AnalyzeResponse(response, length);
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

    // パターンC: LEN=0x10
    static void TestPatternC(string readerName, byte[] idm, byte ins, ushort serviceCode)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            try
            {
                var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);
                try
                {
                    List<byte> data = new List<byte>();
                    data.Add(0x10);  // LEN（フレーム全体、LEN含む）
                    data.Add(0x06);  // CMD
                    data.AddRange(idm);
                    data.Add(0x01);
                    data.Add((byte)(serviceCode & 0xFF));
                    data.Add((byte)(serviceCode >> 8));
                    data.Add(0x01);
                    data.Add(0x80);
                    data.Add(0x00);

                    List<byte> cmd = new List<byte>();
                    cmd.Add(0xFF);
                    cmd.Add(ins);
                    cmd.Add(0x00);
                    cmd.Add(0x00);
                    cmd.Add((byte)data.Count);  // Lc = 16
                    cmd.AddRange(data);
                    cmd.Add(0x00);

                    Console.WriteLine($"  Lc={data.Count} (0x{data.Count:X2})");
                    Console.WriteLine($"  送信: {BitConverter.ToString(cmd.ToArray())}");

                    var response = new byte[256];
                    int length = reader.Transmit(cmd.ToArray(), response);

                    Console.WriteLine($"  応答({length}): {BitConverter.ToString(response, 0, length)}");
                    AnalyzeResponse(response, length);
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

    // パターンD: 二重LEN 0x10-0x10
    static void TestPatternD(string readerName, byte[] idm, byte ins, ushort serviceCode)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            try
            {
                var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);
                try
                {
                    List<byte> data = new List<byte>();
                    data.Add(0x10);  // 外側LEN
                    data.Add(0x10);  // 内側LEN
                    data.Add(0x06);  // CMD
                    data.AddRange(idm);
                    data.Add(0x01);
                    data.Add((byte)(serviceCode & 0xFF));
                    data.Add((byte)(serviceCode >> 8));
                    data.Add(0x01);
                    data.Add(0x80);
                    data.Add(0x00);

                    List<byte> cmd = new List<byte>();
                    cmd.Add(0xFF);
                    cmd.Add(ins);
                    cmd.Add(0x00);
                    cmd.Add(0x00);
                    cmd.Add((byte)data.Count);  // Lc = 17
                    cmd.AddRange(data);
                    cmd.Add(0x00);

                    Console.WriteLine($"  Lc={data.Count} (0x{data.Count:X2})");
                    Console.WriteLine($"  送信: {BitConverter.ToString(cmd.ToArray())}");

                    var response = new byte[256];
                    int length = reader.Transmit(cmd.ToArray(), response);

                    Console.WriteLine($"  応答({length}): {BitConverter.ToString(response, 0, length)}");
                    AnalyzeResponse(response, length);
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

    // パターンE: 二重LEN 0x10-0x0F
    static void TestPatternE(string readerName, byte[] idm, byte ins, ushort serviceCode)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            try
            {
                var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);
                try
                {
                    List<byte> data = new List<byte>();
                    data.Add(0x10);  // 外側LEN
                    data.Add(0x0F);  // 内側LEN
                    data.Add(0x06);  // CMD
                    data.AddRange(idm);
                    data.Add(0x01);
                    data.Add((byte)(serviceCode & 0xFF));
                    data.Add((byte)(serviceCode >> 8));
                    data.Add(0x01);
                    data.Add(0x80);
                    data.Add(0x00);

                    List<byte> cmd = new List<byte>();
                    cmd.Add(0xFF);
                    cmd.Add(ins);
                    cmd.Add(0x00);
                    cmd.Add(0x00);
                    cmd.Add((byte)data.Count);  // Lc = 17
                    cmd.AddRange(data);
                    cmd.Add(0x00);

                    Console.WriteLine($"  Lc={data.Count} (0x{data.Count:X2})");
                    Console.WriteLine($"  送信: {BitConverter.ToString(cmd.ToArray())}");

                    var response = new byte[256];
                    int length = reader.Transmit(cmd.ToArray(), response);

                    Console.WriteLine($"  応答({length}): {BitConverter.ToString(response, 0, length)}");
                    AnalyzeResponse(response, length);
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

    // パターンF: 二重LEN 0x0F-0x10
    static void TestPatternF(string readerName, byte[] idm, byte ins, ushort serviceCode)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            try
            {
                var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);
                try
                {
                    List<byte> data = new List<byte>();
                    data.Add(0x0F);  // 外側LEN
                    data.Add(0x10);  // 内側LEN
                    data.Add(0x06);  // CMD
                    data.AddRange(idm);
                    data.Add(0x01);
                    data.Add((byte)(serviceCode & 0xFF));
                    data.Add((byte)(serviceCode >> 8));
                    data.Add(0x01);
                    data.Add(0x80);
                    data.Add(0x00);

                    List<byte> cmd = new List<byte>();
                    cmd.Add(0xFF);
                    cmd.Add(ins);
                    cmd.Add(0x00);
                    cmd.Add(0x00);
                    cmd.Add((byte)data.Count);  // Lc = 17
                    cmd.AddRange(data);
                    cmd.Add(0x00);

                    Console.WriteLine($"  Lc={data.Count} (0x{data.Count:X2})");
                    Console.WriteLine($"  送信: {BitConverter.ToString(cmd.ToArray())}");

                    var response = new byte[256];
                    int length = reader.Transmit(cmd.ToArray(), response);

                    Console.WriteLine($"  応答({length}): {BitConverter.ToString(response, 0, length)}");
                    AnalyzeResponse(response, length);
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

    // パターンG: Lc=データ長+1
    static void TestPatternG(string readerName, byte[] idm, byte ins, ushort serviceCode)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            try
            {
                var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);
                try
                {
                    List<byte> data = new List<byte>();
                    data.Add(0x10);  // LEN
                    data.Add(0x06);  // CMD
                    data.AddRange(idm);
                    data.Add(0x01);
                    data.Add((byte)(serviceCode & 0xFF));
                    data.Add((byte)(serviceCode >> 8));
                    data.Add(0x01);
                    data.Add(0x80);
                    data.Add(0x00);

                    List<byte> cmd = new List<byte>();
                    cmd.Add(0xFF);
                    cmd.Add(ins);
                    cmd.Add(0x00);
                    cmd.Add(0x00);
                    cmd.Add((byte)(data.Count + 1));  // Lc = 17
                    cmd.AddRange(data);
                    cmd.Add(0x00);

                    Console.WriteLine($"  Lc={data.Count + 1} (0x{(data.Count + 1):X2}), データ長={data.Count}");
                    Console.WriteLine($"  送信: {BitConverter.ToString(cmd.ToArray())}");

                    var response = new byte[256];
                    int length = reader.Transmit(cmd.ToArray(), response);

                    Console.WriteLine($"  応答({length}): {BitConverter.ToString(response, 0, length)}");
                    AnalyzeResponse(response, length);
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

    // パターンH: Lc=データ長+2
    static void TestPatternH(string readerName, byte[] idm, byte ins, ushort serviceCode)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            try
            {
                var reader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);
                try
                {
                    List<byte> data = new List<byte>();
                    data.Add(0x10);  // LEN
                    data.Add(0x06);  // CMD
                    data.AddRange(idm);
                    data.Add(0x01);
                    data.Add((byte)(serviceCode & 0xFF));
                    data.Add((byte)(serviceCode >> 8));
                    data.Add(0x01);
                    data.Add(0x80);
                    data.Add(0x00);

                    List<byte> cmd = new List<byte>();
                    cmd.Add(0xFF);
                    cmd.Add(ins);
                    cmd.Add(0x00);
                    cmd.Add(0x00);
                    cmd.Add((byte)(data.Count + 2));  // Lc = 18
                    cmd.AddRange(data);
                    cmd.Add(0x00);

                    Console.WriteLine($"  Lc={data.Count + 2} (0x{(data.Count + 2):X2}), データ長={data.Count}");
                    Console.WriteLine($"  送信: {BitConverter.ToString(cmd.ToArray())}");

                    var response = new byte[256];
                    int length = reader.Transmit(cmd.ToArray(), response);

                    Console.WriteLine($"  応答({length}): {BitConverter.ToString(response, 0, length)}");
                    AnalyzeResponse(response, length);
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

    static void AnalyzeResponse(byte[] response, int length)
    {
        if (length < 2)
        {
            Console.WriteLine("  応答が短すぎます");
            return;
        }

        byte sw1 = response[length - 2];
        byte sw2 = response[length - 1];

        string swDescription = GetSWDescription(sw1, sw2);
        Console.WriteLine($"  SW: {sw1:X2} {sw2:X2} - {swDescription}");

        if (sw1 == 0x90 && sw2 == 0x00)
        {
            Console.WriteLine("  ✓ 成功");
            int dataLength = length - 2;

            if (dataLength == 0)
            {
                Console.WriteLine("  → データなし");
                return;
            }

            Console.WriteLine($"  データ({dataLength}バイト): {BitConverter.ToString(response, 0, dataLength)}");

            // PN532エラー応答
            if (response[0] == 0xC0)
            {
                AnalyzePN532Error(response, dataLength);
                return;
            }

            // FeliCa応答の検出
            bool isFeliCaResponse = false;
            int felicaStart = 0;

            // パターン1: LENプレフィックス付き
            if (dataLength >= 2 && response[0] == dataLength - 1 && response[1] == 0x07)
            {
                isFeliCaResponse = true;
                felicaStart = 0;
                Console.WriteLine("  → LENプレフィックス付きFeliCa応答検出");
            }
            // パターン2: LENなし
            else if (dataLength >= 12 && response[1] == 0x07)
            {
                isFeliCaResponse = true;
                felicaStart = 0;
                Console.WriteLine("  → FeliCa応答検出（LENなし）");
            }
            // パターン3: 二重LEN
            else if (dataLength >= 3 && response[1] == dataLength - 2 && response[2] == 0x07)
            {
                isFeliCaResponse = true;
                felicaStart = 1;
                Console.WriteLine("  → 二重LENプレフィックス付きFeliCa応答検出");
            }

            if (isFeliCaResponse)
            {
                Console.WriteLine("\n  ★★★★★ FeliCa Read応答成功！ ★★★★★");
                ParseFeliCaReadResponse(response, dataLength, felicaStart);
                return;
            }

            // 16バイトブロック
            if (dataLength == 16)
            {
                Console.WriteLine("\n  ★★★ 16バイトブロックデータ！ ★★★");
                ParseSuicaHistoryBlock(response);
                return;
            }

            Console.WriteLine("  → その他の応答");
        }
        else
        {
            Console.WriteLine("  ✗ エラー");
        }
    }

    static string GetSWDescription(byte sw1, byte sw2)
    {
        if (sw1 == 0x90 && sw2 == 0x00) return "正常終了";
        if (sw1 == 0x61) return $"正常終了（{sw2}バイトのデータが利用可能）";
        if (sw1 == 0x67 && sw2 == 0x00) return "Lcの長さが間違っている";
        if (sw1 == 0x69 && sw2 == 0x81) return "コマンドが許可されていない";
        if (sw1 == 0x69 && sw2 == 0x85) return "コマンドが許可されていない";
        if (sw1 == 0x6A && sw2 == 0x81) return "機能がサポートされていない";
        if (sw1 == 0x6A && sw2 == 0x82) return "ファイルまたはアプリケーションが見つからない";
        if (sw1 == 0x6A && sw2 == 0x86) return "P1-P2が正しくない";
        if (sw1 == 0x6A && sw2 == 0x87) return "Lcが P1-P2と矛盾している";
        if (sw1 == 0x6B && sw2 == 0x00) return "P1-P2が正しくない";
        if (sw1 == 0x6D && sw2 == 0x00) return "INSがサポートされていない";
        if (sw1 == 0x6E && sw2 == 0x00) return "CLAがサポートされていない";

        return "不明なステータス";
    }

    static void AnalyzePN532Error(byte[] response, int dataLength)
    {
        Console.WriteLine("\n  → PN532エラー応答");
        if (dataLength < 4)
        {
            Console.WriteLine("    エラーデータが不完全");
            return;
        }

        byte errorLen = response[1];
        byte errorCategory = response[2];
        byte errorCode1 = response[3];
        byte errorCode2 = dataLength >= 5 ? response[4] : (byte)0;
        Console.WriteLine($"    エラーカテゴリ: 0x{errorCategory:X2}");

        if (errorCategory == 0x01)
        {
            Console.WriteLine($"    → APDUレベルのエラー");
            Console.WriteLine($"    エラーコード: {errorCode1:X2} {errorCode2:X2} - {GetSWDescription(errorCode1, errorCode2)}");
        }
    }

    static void ParseFeliCaReadResponse(byte[] response, int dataLength, int startPos)
    {
        try
        {
            int pos = startPos;

            // LENフィールドをスキップ
            if (pos < dataLength && response[pos] > 0 && response[pos] < dataLength)
            {
                pos++;
            }

            if (pos >= dataLength)
            {
                Console.WriteLine("    解析エラー: データ不足");
                return;
            }

            byte respLen = response[pos++];
            byte respCode = response[pos++];

            Console.WriteLine($"    応答長: {respLen}");
            Console.WriteLine($"    応答コード: 0x{respCode:X2}");

            if (dataLength >= pos + 8)
            {
                byte[] idm = new byte[8];
                Array.Copy(response, pos, idm, 0, 8);
                pos += 8;
                Console.WriteLine($"    IDm: {BitConverter.ToString(idm)}");
            }

            if (dataLength >= pos + 2)
            {
                byte sf1 = response[pos++];
                byte sf2 = response[pos++];
                Console.WriteLine($"    ステータスフラグ: {sf1:X2} {sf2:X2}");

                if (sf1 == 0x00 && sf2 == 0x00)
                {
                    Console.WriteLine("    → 読み取り成功！");

                    if (dataLength >= pos + 1)
                    {
                        byte blockCount = response[pos++];
                        Console.WriteLine($"    ブロック数: {blockCount}");

                        for (int i = 0; i < blockCount && dataLength >= pos + 16; i++)
                        {
                            byte[] blockData = new byte[16];
                            Array.Copy(response, pos, blockData, 0, 16);
                            pos += 16;

                            Console.WriteLine($"\n    ★★★★★ ブロック{i}データ取得成功！ ★★★★★");
                            Console.WriteLine($"    {BitConverter.ToString(blockData)}");

                            if (i == 0)
                            {
                                ParseSuicaHistoryBlock(blockData);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"    → FeliCaエラー");
                    Console.WriteLine($"       SF1=0x{sf1:X2}: {GetFeliCaStatusFlag1(sf1)}");
                    Console.WriteLine($"       SF2=0x{sf2:X2}: {GetFeliCaStatusFlag2(sf2)}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    解析エラー: {ex.Message}");
        }
    }

    static string GetFeliCaStatusFlag1(byte sf1)
    {
        switch (sf1)
        {
            case 0x00: return "正常";
            case 0xFF: return "エラー発生";
            case 0xA1: return "長さエラー";
            case 0xA2: return "メモリエラー";
            case 0xA3: return "書き込みエラー";
            case 0xA8: return "サービスコードエラー";
            default: return "不明";
        }
    }

    static string GetFeliCaStatusFlag2(byte sf2)
    {
        switch (sf2)
        {
            case 0x00: return "正常";
            case 0x70: return "メモリエラー";
            case 0xA1: return "サービス数エラー";
            case 0xA2: return "ブロック数エラー";
            case 0xA3: return "サービスコードエラー";
            case 0xA4: return "アクセス権エラー";
            case 0xA5: return "ブロックリストエラー";
            default: return "不明";
        }
    }

    static void ParseSuicaHistoryBlock(byte[] block)
    {
        try
        {
            Console.WriteLine("\n    === Suica履歴データ解析 ===");

            byte termType = block[0];
            Console.WriteLine($"    端末種: 0x{termType:X2} ({GetTerminalType(termType)})");

            byte procType = block[1];
            Console.WriteLine($"    処理: 0x{procType:X2} ({GetProcessType(procType)})");

            ushort dateData = (ushort)((block[4] << 8) | block[5]);
            int year = ((dateData >> 9) & 0x7F) + 2000;
            int month = (dateData >> 5) & 0x0F;
            int day = dateData & 0x1F;

            if (year >= 2000 && year <= 2099 && month >= 1 && month <= 12 && day >= 1 && day <= 31)
            {
                Console.WriteLine($"    日付: {year}/{month:D2}/{day:D2}");
            }

            int balance = (block[10] << 8) | block[11];
            Console.WriteLine($"    残額: ¥{balance}");

            // 連番
            uint seqNo = (uint)((block[12] << 16) | (block[13] << 8) | block[14]);
            Console.WriteLine($"    連番: {seqNo}");

            // 地域コード
            byte regionCode = block[15];
            Console.WriteLine($"    地域: 0x{regionCode:X2}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    解析エラー: {ex.Message}");
        }
    }

    static string GetTerminalType(byte termType)
    {
        switch (termType)
        {
            case 0x03: return "精算機";
            case 0x04: return "携帯型端末";
            case 0x05: return "車載端末";
            case 0x07: return "券売機";
            case 0x08: return "券売機等";
            case 0x09: return "入金機";
            case 0x12: return "券売機等";
            case 0x14: return "券売機等";
            case 0x15: return "券売機等";
            case 0x16: return "改札機";
            case 0x17: return "簡易改札機";
            case 0x18: return "窓口端末";
            case 0x19: return "窓口端末";
            case 0x1A: return "改札端末";
            case 0x1B: return "携帯電話";
            case 0x1C: return "乗継精算機";
            case 0x1D: return "連絡改札機";
            case 0x1F: return "簡易入金機";
            case 0x23: return "新幹線改札機";
            case 0x46: return "VIEW ALTTE";
            case 0x48: return "VIEW ALTTE";
            case 0xC7: return "物販端末";
            case 0xC8: return "自販機";
            default: return "不明";
        }
    }

    static string GetProcessType(byte procType)
    {
        switch (procType)
        {
            case 0x01: return "運賃支払(改札出場)";
            case 0x02: return "チャージ";
            case 0x03: return "券購入";
            case 0x04: return "精算";
            case 0x05: return "精算 (入場精算)";
            case 0x06: return "窓口出場";
            case 0x07: return "新規";
            case 0x08: return "控除";
            case 0x0D: return "バス (PiTaPa系)";
            case 0x0F: return "バス (IruCa系)";
            case 0x11: return "再発行";
            case 0x13: return "支払(新幹線利用)";
            case 0x14: return "オートチャージ";
            case 0x15: return "バスチャージ";
            case 0x1F: return "バス路面 (PiTaPa系)";
            case 0x23: return "バス (IruCa系)";
            case 0x46: return "物販";
            case 0x48: return "特典チャージ";
            case 0x49: return "レジ入金";
            case 0x4A: return "物販取消";
            case 0xC6: return "現金併用物販";
            case 0xCB: return "入場時オートチャージ";
            case 0x84: return "他社精算";
            default: return "不明";
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
