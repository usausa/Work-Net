using System;
using System.Linq;
using System.Collections.Generic;
using PCSC;

class SuicaReader
{
    const ushort SERVICE_CODE_HISTORY = 0x090F;

    static void Main(string[] args)
    {
        using (var context = ContextFactory.Instance.Establish(SCardScope.System))
        {
            var readers = context.GetReaders();
            if (readers.Length == 0)
            {
                Console.WriteLine("リーダーが見つかりません");
                return;
            }

            Console.WriteLine($"使用リーダー: {readers[0]}\n");

            var reader = context.ConnectReader(readers[0], SCardShareMode.Shared, SCardProtocol.Any);

            try
            {
                // Polling
                Console.WriteLine("=== Polling ===");
                byte[] idm = null;
                byte[] pmm = null;

                List<byte> pollingCmd = new List<byte>();
                pollingCmd.Add(0xFF);
                pollingCmd.Add(0xFE);
                pollingCmd.Add(0x00);
                pollingCmd.Add(0x00);
                pollingCmd.Add(0x05);
                pollingCmd.Add(0x00);
                pollingCmd.Add(0xFF);
                pollingCmd.Add(0xFF);
                pollingCmd.Add(0x01);
                pollingCmd.Add(0x00);
                pollingCmd.Add(0x00);

                var pollingResponse = new byte[256];
                int pollingLen = reader.Transmit(pollingCmd.ToArray(), pollingResponse);

                if (pollingLen >= 2 && pollingResponse[pollingLen - 2] == 0x90 && pollingResponse[pollingLen - 1] == 0x00)
                {
                    int dataLen = pollingLen - 2;
                    if (dataLen >= 18)
                    {
                        idm = new byte[8];
                        Array.Copy(pollingResponse, 1, idm, 0, 8);
                        pmm = new byte[8];
                        Array.Copy(pollingResponse, 9, pmm, 0, 8);

                        Console.WriteLine($"IDm: {BitConverter.ToString(idm).Replace("-", "")}");
                        Console.WriteLine($"PMm: {BitConverter.ToString(pmm).Replace("-", "")}\n");
                    }
                }

                if (idm == null)
                {
                    Console.WriteLine("Polling失敗");
                    return;
                }

                System.Threading.Thread.Sleep(200);

                // 最終テスト
                Console.WriteLine("=== 最終確認テスト ===\n");

                // テスト1: ISO 7816-4 Select DF
                Console.WriteLine("[テスト1] ISO Select DF (FeliCa Common Area)");
                TestSelectDF(reader);

                // テスト2: 異なるCLA
                Console.WriteLine("\n[テスト2] 異なるCLAでテスト");
                TestDifferentCLA(reader, idm);

                // テスト3: Get Response
                Console.WriteLine("\n[テスト3] Get Response (61 XX対応)");
                TestGetResponse(reader, idm);

                // テスト4: リーダー情報取得
                Console.WriteLine("\n[テスト4] リーダー情報取得");
                TestGetReaderInfo(reader);

                // テスト5: 拡張APDUテスト
                Console.WriteLine("\n[テスト5] 拡張APDU形式");
                TestExtendedAPDU(reader, idm);

                // 結論
                Console.WriteLine("\n" + "=".PadRight(80, '='));
                Console.WriteLine("結論");
                Console.WriteLine("=".PadRight(80, '='));
                Console.WriteLine("\nPaSoRi RC-S300のPC/SCドライバは、以下の理由により");
                Console.WriteLine("Read Without Encryptionを直接サポートしていない可能性が高い：");
                Console.WriteLine();
                Console.WriteLine("1. すべてのINS/データパターンで 63 01 エラー");
                Console.WriteLine("2. 5F 46 タグは認識されるが、内容が拒否される");
                Console.WriteLine("3. Web USBでは動作するが、PC/SCではブロックされる");
                Console.WriteLine();
                Console.WriteLine("【代替案】");
                Console.WriteLine("1. libpafe や libnfc などの低レベルライブラリを使用");
                Console.WriteLine("2. Web USB API を使用（ブラウザ経由）");
                Console.WriteLine("3. Androidの NFC API を使用");
                Console.WriteLine("4. Sony公式のFeliCa SDKを使用（商用利用の場合）");
                Console.WriteLine();
                Console.WriteLine("【参考情報】");
                Console.WriteLine("- Polling (INS=0xFE) は成功 → リーダーは正常動作");
                Console.WriteLine("- IDm取得は成功 → カード認識は問題なし");
                Console.WriteLine("- Read コマンドのみ失敗 → ドライバレベルの制限");
                Console.WriteLine();
                Console.WriteLine("【技術的詳細】");
                Console.WriteLine("- エラー 6A 81: PN532がコマンドを認識しない");
                Console.WriteLine("- エラー 63 01: PN532はコマンドを処理しようとするが認証失敗");
                Console.WriteLine("- 5F 46: BER-TLVタグ（正しい形式だが内容が不正）");
            }
            finally
            {
                reader.Disconnect(SCardReaderDisposition.Leave);
            }
        }

        Console.WriteLine("\nEnterキーで終了...");
        Console.ReadLine();
    }

    static void TestSelectDF(ICardReader reader)
    {
        // FeliCa Common DF
        List<byte> cmd = new List<byte>();
        cmd.Add(0x00);  // CLA
        cmd.Add(0xA4);  // INS (Select File)
        cmd.Add(0x04);  // P1 (Select by DF name)
        cmd.Add(0x00);  // P2
        cmd.Add(0x07);  // Lc
        cmd.AddRange(new byte[] { 0xD3, 0x92, 0xF0, 0x03, 0x92, 0x00, 0x01 });  // FeliCa Common DF
        cmd.Add(0x00);  // Le

        try
        {
            var response = new byte[256];
            int length = reader.Transmit(cmd.ToArray(), response);

            Console.WriteLine($"  SW: {response[length - 2]:X2} {response[length - 1]:X2}");

            if (length > 2)
            {
                int dataLen = length - 2;
                Console.WriteLine($"  データ({dataLen}): {BitConverter.ToString(response, 0, dataLen)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  例外: {ex.Message}");
        }
    }

    static void TestDifferentCLA(ICardReader reader, byte[] idm)
    {
        byte[] claValues = { 0x00, 0x80, 0x90, 0xA0, 0xB0, 0xC0, 0xD0, 0xE0, 0xF0, 0xFF };

        foreach (byte cla in claValues)
        {
            List<byte> data = new List<byte>();
            data.Add(0x5F);
            data.Add(0x46);
            data.Add(0x00);
            data.Add(0x10);
            data.Add(0x06);
            data.AddRange(idm);
            data.Add(0x01);
            data.Add(0x0F);
            data.Add(0x09);
            data.Add(0x01);
            data.Add(0x80);
            data.Add(0x00);

            List<byte> cmd = new List<byte>();
            cmd.Add(cla);
            cmd.Add(0x50);
            cmd.Add(0x00);
            cmd.Add(0x01);
            cmd.Add((byte)data.Count);
            cmd.AddRange(data);
            cmd.Add(0x00);

            try
            {
                var response = new byte[256];
                int length = reader.Transmit(cmd.ToArray(), response);

                byte sw1 = response[length - 2];
                byte sw2 = response[length - 1];

                if (sw1 != 0x6E && sw1 != 0x6D)
                {
                    Console.WriteLine($"  CLA=0x{cla:X2}: SW={sw1:X2} {sw2:X2}");

                    if (length > 2)
                    {
                        int dataLen = length - 2;
                        if (response[0] != 0xC0)
                        {
                            Console.WriteLine($"    ★ データ: {BitConverter.ToString(response, 0, Math.Min(dataLen, 32))}");
                        }
                    }
                }
            }
            catch { }

            System.Threading.Thread.Sleep(50);
        }
    }

    static void TestGetResponse(ICardReader reader, byte[] idm)
    {
        // まず63 01を発生させる
        List<byte> data = new List<byte>();
        data.Add(0x5F);
        data.Add(0x46);
        data.Add(0x00);
        data.Add(0x10);
        data.Add(0x06);
        data.AddRange(idm);
        data.Add(0x01);
        data.Add(0x0F);
        data.Add(0x09);
        data.Add(0x01);
        data.Add(0x80);
        data.Add(0x00);

        List<byte> cmd1 = new List<byte>();
        cmd1.Add(0xFF);
        cmd1.Add(0x50);
        cmd1.Add(0x00);
        cmd1.Add(0x01);
        cmd1.Add((byte)data.Count);
        cmd1.AddRange(data);
        cmd1.Add(0x00);

        var response1 = new byte[256];
        int length1 = reader.Transmit(cmd1.ToArray(), response1);

        byte sw1 = response1[length1 - 2];
        byte sw2 = response1[length1 - 1];

        Console.WriteLine($"  初回: SW={sw1:X2} {sw2:X2}");

        // Get Response
        if (sw1 == 0x61 || sw1 == 0x63)
        {
            List<byte> cmd2 = new List<byte>();
            cmd2.Add(0xFF);
            cmd2.Add(0xC0);  // Get Response
            cmd2.Add(0x00);
            cmd2.Add(0x00);
            cmd2.Add(sw2 > 0 ? sw2 : (byte)0x00);

            try
            {
                var response2 = new byte[256];
                int length2 = reader.Transmit(cmd2.ToArray(), response2);

                Console.WriteLine($"  Get Response: SW={response2[length2 - 2]:X2} {response2[length2 - 1]:X2}");

                if (length2 > 2)
                {
                    int dataLen = length2 - 2;
                    Console.WriteLine($"  データ: {BitConverter.ToString(response2, 0, dataLen)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  例外: {ex.Message}");
            }
        }
    }

    static void TestGetReaderInfo(ICardReader reader)
    {
        // Get Data - UID
        List<byte> cmd1 = new List<byte>();
        cmd1.Add(0xFF);
        cmd1.Add(0xCA);
        cmd1.Add(0x00);
        cmd1.Add(0x00);
        cmd1.Add(0x00);

        try
        {
            var response = new byte[256];
            int length = reader.Transmit(cmd1.ToArray(), response);

            Console.WriteLine($"  Get Data (UID): SW={response[length - 2]:X2} {response[length - 1]:X2}");

            if (length > 2)
            {
                int dataLen = length - 2;
                Console.WriteLine($"  データ: {BitConverter.ToString(response, 0, dataLen)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  例外: {ex.Message}");
        }

        // Get Data - 別のタグ
        for (byte p2 = 0; p2 <= 0x10; p2++)
        {
            List<byte> cmd2 = new List<byte>();
            cmd2.Add(0xFF);
            cmd2.Add(0xCA);
            cmd2.Add(0x00);
            cmd2.Add(p2);
            cmd2.Add(0x00);

            try
            {
                var response = new byte[256];
                int length = reader.Transmit(cmd2.ToArray(), response);

                byte sw1 = response[length - 2];
                byte sw2 = response[length - 1];

                if (sw1 == 0x90 && length > 2)
                {
                    int dataLen = length - 2;
                    if (dataLen > 0)
                    {
                        Console.WriteLine($"  P2=0x{p2:X2}: {BitConverter.ToString(response, 0, dataLen)}");
                    }
                }
            }
            catch { }
        }
    }

    static void TestExtendedAPDU(ICardReader reader, byte[] idm)
    {
        Console.WriteLine("  拡張APDU (Case 4 Extended)");

        List<byte> data = new List<byte>();
        data.Add(0x5F);
        data.Add(0x46);
        data.Add(0x00);
        data.Add(0x10);
        data.Add(0x06);
        data.AddRange(idm);
        data.Add(0x01);
        data.Add(0x0F);
        data.Add(0x09);
        data.Add(0x01);
        data.Add(0x80);
        data.Add(0x00);

        List<byte> cmd = new List<byte>();
        cmd.Add(0xFF);
        cmd.Add(0x50);
        cmd.Add(0x00);
        cmd.Add(0x01);
        // 拡張Lc (3バイト)
        cmd.Add(0x00);
        cmd.Add(0x00);
        cmd.Add((byte)data.Count);
        cmd.AddRange(data);
        // 拡張Le (2バイト)
        cmd.Add(0x00);
        cmd.Add(0x00);

        try
        {
            var response = new byte[65536];
            int length = reader.Transmit(cmd.ToArray(), response);

            Console.WriteLine($"  SW: {response[length - 2]:X2} {response[length - 1]:X2}");

            if (length > 2)
            {
                int dataLen = length - 2;
                Console.WriteLine($"  データ({dataLen}): {BitConverter.ToString(response, 0, Math.Min(dataLen, 32))}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  例外: {ex.Message}");
        }
    }
}
