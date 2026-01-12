using PCSC;
using PCSC.Iso7816;
using PCSC.Monitoring;
using System.Text;

namespace WorkRead;

internal class Program
{
    static void Main(string[] args)
    {
        // PIN入力
        Console.Write("\n券面事項入力補助用PIN（4桁）を入力してください: ");
        string pin = Console.ReadLine();

        if (string.IsNullOrEmpty(pin) || pin.Length != 4)
        {
            Console.WriteLine("PINは4桁の数字である必要があります");
            return;
        }

        using var reader = new Reader(pin);
        reader.Start();


        Console.WriteLine("* Start");

        Console.ReadLine();

        reader.Stop();

        Console.WriteLine("* Stop");
    }
}

internal sealed class Reader : IDisposable
{
    private readonly ISCardMonitor monitor;

    private readonly string pin;

    public bool IsRunning { get; private set; }

    public Reader(string pin)
    {
        monitor = MonitorFactory.Instance.Create(SCardScope.System);
        monitor.CardInserted += OnCardInserted;

        this.pin = pin;
    }

    public void Dispose()
    {
        MonitorFactory.Instance.Release(monitor);
    }


    private void OnCardInserted(object sender, CardStatusEventArgs e)
    {
        using var context = ContextFactory.Instance.Establish(SCardScope.System);
        var reader = context.ConnectReader(e.ReaderName, SCardShareMode.Shared, SCardProtocol.Any);
        try
        {
            Console.WriteLine($"接続プロトコル: {reader.Protocol}");

            // 券面事項入力補助APの選択
            byte[] aid = new byte[] {
            0xD3, 0x92, 0xF0, 0x00, 0x26, 0x01, 0x00, 0x00,
            0x00, 0x01
        };

            var sendBuffer = new byte[] {
            0x00, 0xA4, 0x04, 0x0C,
            (byte)aid.Length
        }.Concat(aid).ToArray();

            Console.WriteLine("券面事項入力補助APを選択...");
            var response = SendCommand(reader, sendBuffer);

            if (!IsSuccess(response))
            {
                Console.WriteLine($"AP選択失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }
            Console.WriteLine("AP選択成功!");

        //    // 方法1: DFを選択してからEFにアクセス
        //    Console.WriteLine("\n=== 方法1: DF選択 ===");

        //    // DF選択（P1=0x01: DFを選択）
        //    sendBuffer = new byte[] {
        //    0x00, 0xA4, 0x01, 0x0C,
        //    0x02,
        //    0x00, 0x11  // DF ID
        //};

        //    Console.WriteLine("DF 0x0011 を選択...");
        //    response = SendCommand(reader, sendBuffer);

        //    if (IsSuccess(response))
        //    {
        //        Console.WriteLine("DF選択成功!");

        //        // DFの下のEFを選択（P1=0x02）
        //        sendBuffer = new byte[] {
        //        0x00, 0xA4, 0x02, 0x0C,
        //        0x02,
        //        0x00, 0x01  // 相対EF ID
        //    };

        //        response = SendCommand(reader, sendBuffer);

        //        if (IsSuccess(response))
        //        {
        //            Console.WriteLine("EF選択成功!");
        //            TryReadFile(reader);
        //        }
        //    }

            // 方法2: MFから選択
            Console.WriteLine("\n=== 方法2: MFから選択 ===");

            // MF選択
            sendBuffer = new byte[] {
            0x00, 0xA4, 0x00, 0x0C,
            0x02,
            0x3F, 0x00  // MF
        };

            //Console.WriteLine("MFを選択...");
            //response = SendCommand(reader, sendBuffer);

            //if (IsSuccess(response))
            //{
            //    Console.WriteLine("MF選択成功!");

            //    // フルパスでEF選択
            //    sendBuffer = new byte[] {
            //    0x00, 0xA4, 0x08, 0x0C,  // P1=0x08: パス指定
            //    0x04,
            //    0x00, 0x11, 0x00, 0x01
            //};

            //    response = SendCommand(reader, sendBuffer);

            //    if (IsSuccess(response))
            //    {
            //        Console.WriteLine("EF選択成功!");
            //        TryReadFile(reader);
            //    }
            //}

            // 方法3: 個人番号カードAPを試す（こちらの方が一般的）
            Console.WriteLine("\n=== 方法3: 個人番号カードAP ===");

            byte[] aid2 = new byte[] {
            0xD3, 0x92, 0x10, 0x00, 0x31, 0x00, 0x01, 0x01,
            0x04, 0x08
        };

            sendBuffer = new byte[] {
            0x00, 0xA4, 0x04, 0x0C,
            (byte)aid2.Length
        }.Concat(aid2).ToArray();

            Console.WriteLine("個人番号カードAPを選択...");
            response = SendCommand(reader, sendBuffer);

            if (IsSuccess(response))
            {
                Console.WriteLine("AP選択成功!");

                if (!string.IsNullOrEmpty(pin) && pin.Length == 4)
                {
                    byte[] pinBytes = Encoding.ASCII.GetBytes(pin);
                    sendBuffer = new byte[] {
                    0x00, 0x20, 0x00, 0x80,
                    (byte)pinBytes.Length
                }.Concat(pinBytes).ToArray();

                    Console.WriteLine("PIN認証...");
                    response = SendCommand(reader, sendBuffer);

                    if (IsSuccess(response))
                    {
                        Console.WriteLine("PIN認証成功!");
                    }
                    else
                    {
                        Console.WriteLine($"PIN認証失敗: SW={response.SW1:X2}{response.SW2:X2}");
                    }
                }

                // EF選択を試す
                ushort[] efIds = { 0x0011 , 0x0016, 0x0017, 0x001A };

                foreach (var efId in efIds)
                {
                    sendBuffer = new byte[] {
                    0x00, 0xA4, 0x02, 0x0C,
                    0x02,
                    (byte)(efId >> 8),
                    (byte)(efId & 0xFF)
                };

                    Console.WriteLine($"\nEF 0x{efId:X4} を選択...");
                    response = SendCommand(reader, sendBuffer);

                    if (IsSuccess(response))
                    {
                        Console.WriteLine("選択成功!");
                        TryReadFile(reader);
                    }
                }
            }

            // 方法4: 公的個人認証APを試す
            Console.WriteLine("\n=== 方法4: 公的個人認証AP（署名用） ===");

            byte[] aid3 = new byte[] {
            0xD3, 0x92, 0xF0, 0x00, 0x26, 0x01, 0x00, 0x00,
            0x00, 0x01
        };

            sendBuffer = new byte[] {
            0x00, 0xA4, 0x04, 0x0C,
            (byte)aid3.Length
        }.Concat(aid3).ToArray();

            response = SendCommand(reader, sendBuffer);

            if (IsSuccess(response))
            {
                // 証明書を読み取る
                sendBuffer = new byte[] {
                0x00, 0xA4, 0x02, 0x0C,
                0x02,
                0x00, 0x0A  // 証明書EF
            };

                Console.WriteLine("証明書EFを選択...");
                response = SendCommand(reader, sendBuffer);

                if (IsSuccess(response))
                {
                    Console.WriteLine("選択成功!");
                    TryReadFile(reader);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラー: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            reader.Disconnect(SCardReaderDisposition.Leave);
        }
    }

    private void TryReadFile(ICardReader reader)
    {
        // READ BINARYを試す
        var sendBuffer = new byte[] {
            0x00, 0xB0,
            0x00, 0x00,
            0x00
        };

        Console.WriteLine("  READ BINARY...");
        var response = SendCommand(reader, sendBuffer);

        if (IsSuccess(response) && response.Data.Length > 0)
        {
            Console.WriteLine($"  成功! ({response.Data.Length}バイト)");
            Console.WriteLine($"  HEX: {BitConverter.ToString(response.Data)}");
            ParseRecordData(response.Data);
            return;
        }

        // READ RECORDを試す
        sendBuffer = new byte[] {
            0x00, 0xB2,
            0x01, 0x04,
            0x00
        };

        Console.WriteLine("  READ RECORD...");
        response = SendCommand(reader, sendBuffer);

        if (IsSuccess(response) && response.Data.Length > 0)
        {
            Console.WriteLine($"  成功! ({response.Data.Length}バイト)");
            Console.WriteLine($"  HEX: {BitConverter.ToString(response.Data)}");
            ParseRecordData(response.Data);
            return;
        }

        Console.WriteLine($"  読み取り失敗: SW={response.SW1:X2}{response.SW2:X2}");
    }

    private void ParseRecordData(byte[] data)
    {
        if (data.Length == 0)
            return;

        try
        {
            Console.WriteLine("\n--- データ解析 ---");

            // UTF-8でデコードを試みる
            try
            {
                var printableData = data.Where(b => b >= 0x20 || b == 0x0A || b == 0x0D).ToArray();

                if (printableData.Length > 10)
                {
                    string text = Encoding.UTF8.GetString(printableData);
                    Console.WriteLine($"テキスト抽出: {text}");
                }
            }
            catch { }

            // TLV構造を解析
            ParseTLV(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析エラー: {ex.Message}");
        }
    }



    private void ParseTLV(byte[] data)
    {
        int pos = 0;
        while (pos < data.Length - 2)
        {
            byte tag = data[pos];
            if (tag == 0x00 || tag == 0xFF) // パディング
            {
                pos++;
                continue;
            }

            int length = data[pos + 1];
            pos += 2;

            if (pos + length > data.Length)
                break;

            byte[] value = new byte[length];
            Array.Copy(data, pos, value, 0, length);

            try
            {
                string valueStr = Encoding.UTF8.GetString(value);
                Console.WriteLine($"Tag: 0x{tag:X2}, Length: {length}, Value: {valueStr}");
            }
            catch
            {
                Console.WriteLine($"Tag: 0x{tag:X2}, Length: {length}, Value: {BitConverter.ToString(value)}");
            }

            pos += length;
        }
    }



    private ResponseData SendCommand(ICardReader reader, byte[] command)
    {
        var receiveBuffer = new byte[258]; // SW1+SW2を含む

        Console.WriteLine($"送信: {BitConverter.ToString(command)}");

        var bytesReceived = reader.Transmit(command, receiveBuffer);

        Console.WriteLine($"受信: {BitConverter.ToString(receiveBuffer, 0, bytesReceived)}");

        return new ResponseData(receiveBuffer, bytesReceived);
    }

    private bool IsSuccess(ResponseData response)
    {
        return response.SW1 == 0x90 && response.SW2 == 0x00;
    }

    private int ParseDataLength(byte[] header)
    {
        // TLV形式のデータ長を解析
        // 通常は header[1] にデータ長が入っている
        if (header.Length < 2)
            return 0;

        if (header[1] <= 0x7F)
        {
            // 短形式
            return header[1];
        }
        else
        {
            // 長形式
            int numBytes = header[1] & 0x7F;
            int length = 0;
            for (int i = 0; i < numBytes && (2 + i) < header.Length; i++)
            {
                length = (length << 8) | header[2 + i];
            }
            return length;
        }
    }

    private void ParseMyNumberCardData(byte[] data)
    {
        Console.WriteLine("\n=== 券面事項入力補助データ ===");

        // TLVデータの解析
        int pos = 0;
        while (pos < data.Length)
        {
            if (pos + 2 > data.Length)
                break;

            byte tag = data[pos];
            int length = data[pos + 1];
            pos += 2;

            if (pos + length > data.Length)
                break;

            byte[] value = new byte[length];
            Array.Copy(data, pos, value, 0, length);
            pos += length;

            // タグに応じて表示
            string valueStr = Encoding.UTF8.GetString(value);
            Console.WriteLine($"Tag: 0x{tag:X2}, Length: {length}, Value: {valueStr}");
        }
    }

    class ResponseData
    {
        public byte[] Data { get; private set; }
        public byte SW1 { get; private set; }
        public byte SW2 { get; private set; }

        public ResponseData(byte[] buffer, int length)
        {
            if (length >= 2)
            {
                SW1 = buffer[length - 2];
                SW2 = buffer[length - 1];
                Data = new byte[length - 2];
                Array.Copy(buffer, 0, Data, 0, length - 2);
            }
            else
            {
                Data = new byte[0];
                SW1 = 0x00;
                SW2 = 0x00;
            }
        }
    }

    public bool Start()
    {
        if (IsRunning)
        {
            return false;
        }

        using var context = ContextFactory.Instance.Establish(SCardScope.System);
        var readers = context.GetReaders();
        if (readers.Length == 0)
        {
            return false;
        }

        monitor.Start(readers[0]);

        IsRunning = true;

        return true;
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        monitor.Cancel();

        IsRunning = false;
    }
}

