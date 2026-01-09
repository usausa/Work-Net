using System.Buffers;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace WorkSerialLineReader
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== SerialPortReader 総合テスト ===\n");

            // テスト1: 通常の受信（単一バイト終端）
            Test1_NormalReceive();
            Thread.Sleep(500);

            // テスト2: バッファオーバーフロー
            Test2_BufferOverflow();
            Thread.Sleep(500);

            // テスト3: リングバッファの境界をまたぐケース
            Test3_RingWrap();
            Thread.Sleep(500);

            // テスト4: 複数バイト終端文字列
            Test4_MultiByteDelimiter();
            Thread.Sleep(500);

            // テスト5: 検索位置の最適化テスト
            Test5_SearchOptimization();
            Thread.Sleep(500);

            // テスト6: 連続したデータ受信
            Test6_ContinuousData();
            Thread.Sleep(500);

            // テスト7: 空行の処理
            Test7_EmptyLines();
            Thread.Sleep(500);

            // ===== 新規追加テスト =====

            Test8_StackAllocThreshold();
            Thread.Sleep(500);

            Test9_DelimiterAtBufferBoundary();
            Thread.Sleep(500);

            Test10_MultiByteDelimiterSplit();
            Thread.Sleep(500);

            Test11_MaxBufferSizeExactly();
            Thread.Sleep(500);

            Test12_RepeatedOverflow();
            Thread.Sleep(500);

            Test13_DelimiterOnly();
            Thread.Sleep(500);

            Test14_LargeLineWithStackAlloc();
            Thread.Sleep(500);

            Test15_SearchStartAtBoundary();
            Thread.Sleep(500);

            Test16_AlternatingHeadTailPositions();
            Thread.Sleep(500);

            Test17_PartialDelimiterAtEnd();
            Thread.Sleep(500);

            Test18_ConsecutiveDelimiters();
            Thread.Sleep(500);

            Test19_SingleByteReads();
            Thread.Sleep(500);

            Test20_FullBufferNoDelimiter();
            Thread.Sleep(500);

            Test21_OverflowThenDelimiter();
            Thread.Sleep(500);

            Test22_ContinuousOverflowNoDelimiter();
            Thread.Sleep(500);

            Test23_OverflowThenMultipleLines();
            Thread.Sleep(500);

            Test24_Statistics();
            Thread.Sleep(500);

            // バッファ破棄機能のテスト
            Test25_DiscardBuffer_Basic();
            Thread.Sleep(500);

            Test26_DiscardBuffer_Empty();
            Thread.Sleep(500);

            Test27_DiscardBuffer_WithRingWrap();
            Thread.Sleep(500);

            Test28_DiscardBuffer_Statistics();
            Thread.Sleep(500);

            Test29_DiscardBuffer_AfterPartialData();
            Thread.Sleep(500);

            Test30_DiscardBuffer_MultipleDiscards();
            Thread.Sleep(500);

            Test31_DiscardBuffer_DuringReceive();
            Thread.Sleep(500);

            Test32_DiscardBuffer_WithPeakUsage();
            Thread.Sleep(500);

            Console.WriteLine("\n=== 全テスト完了 ===");
            Console.ReadLine();
        }

        // テスト1: 通常の受信（単一バイト終端）
        static void Test1_NormalReceive()
        {
            Console.WriteLine("\n--- Test1: 通常の受信 ---");
            Debug.WriteLine("\n========== Test1: Normal Receive ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 100,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: {line}");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // データ送信
            sendPort.Write("Hello\n");
            Thread.Sleep(100);
            sendPort.Write("World\n");
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 2行, 実際: {receivedCount}行");
        }

        // テスト2: バッファオーバーフロー
        static void Test2_BufferOverflow()
        {
            Console.WriteLine("\n--- Test2: バッファオーバーフロー ---");
            Debug.WriteLine("\n========== Test2: Buffer Overflow ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 10,  // 小さいバッファ
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            int overflowCount = 0;

            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: {line} ({lineBytes.Length}バイト)");
            };

            reader.BufferOverflow += (sender, discardedBytes) =>
            {
                overflowCount++;
                Console.WriteLine($"  オーバーフロー #{overflowCount}: {discardedBytes}バイト破棄");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // バッファサイズ(10)を超えるデータを送信
            sendPort.Write("ABCDEFGHIJKLMNO\n");  // 16バイト（終端含む）
            Thread.Sleep(200);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: オーバーフロー発生、最後の10バイト程度を受信");
        }

        // テスト3: リングバッファの境界をまたぐケース
        static void Test3_RingWrap()
        {
            Console.WriteLine("\n--- Test3: リングバッファの境界越え ---");
            Debug.WriteLine("\n========== Test3: Ring Buffer Wrap ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 15,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: {line}");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // バッファの後半に書き込んでから、先頭に回り込むパターン
            sendPort.Write("First\n");      // 6バイト
            Thread.Sleep(100);
            sendPort.Write("Second\n");     // 7バイト
            Thread.Sleep(100);
            sendPort.Write("Third\n");      // 6バイト
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 3行受信");
        }

        // テスト4: 複数バイト終端文字列
        static void Test4_MultiByteDelimiter()
        {
            Console.WriteLine("\n--- Test4: 複数バイト終端文字列 ---");
            Debug.WriteLine("\n========== Test4: Multi-byte Delimiter ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            // \r\n を終端とする
            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { 0x0D, 0x0A },  // \r\n
                maxBufferSize: 50,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: {line}");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // \r\n区切りでデータ送信
            sendPort.Write("Line1\r\n");
            Thread.Sleep(100);
            sendPort.Write("Line2\r\n");
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 2行受信");
        }

        // テスト5: 検索位置の最適化テスト
        static void Test5_SearchOptimization()
        {
            Console.WriteLine("\n--- Test5: 検索位置の最適化 ---");
            Debug.WriteLine("\n========== Test5: Search Optimization ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 50,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: {line}");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 終端なしのデータを送信してから、後で終端を送信
            sendPort.Write("Partial");
            Thread.Sleep(100);
            sendPort.Write("Data");
            Thread.Sleep(100);
            sendPort.Write("Here\n");  // ここで終端
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 1行受信（PartialDataHere）");
            Console.WriteLine($"  searchStartが更新されることを確認");
        }

        // テスト6: 連続したデータ受信
        static void Test6_ContinuousData()
        {
            Console.WriteLine("\n--- Test6: 連続データ受信 ---");
            Debug.WriteLine("\n========== Test6: Continuous Data ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 30,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: {line}");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 一度に複数行を送信
            sendPort.Write("Line1\nLine2\nLine3\n");
            Thread.Sleep(200);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 3行受信");
        }

        // テスト7: 空行の処理
        static void Test7_EmptyLines()
        {
            Console.WriteLine("\n--- Test7: 空行の処理 ---");
            Debug.WriteLine("\n========== Test7: Empty Lines ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 50,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: '{line}' ({lineBytes.Length}バイト)");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 空行を含むデータ
            sendPort.Write("Before\n\nAfter\n");
            Thread.Sleep(200);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 2行受信（空行はスキップ）");
        }

        // Test8: stackallocとArrayPoolの閾値テスト
        static void Test8_StackAllocThreshold()
        {
            Console.WriteLine("\n--- Test8: stackalloc閾値テスト ---");
            Debug.WriteLine("\n========== Test8: StackAlloc Threshold ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 2048,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                Console.WriteLine($"  受信 #{receivedCount}: {lineBytes.Length}バイト");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 512バイト以下（stackalloc使用）
            string smallData = new string('A', 500);
            sendPort.Write(smallData + "\n");
            Thread.Sleep(100);

            // 512バイト超（ArrayPool使用）
            string largeData = new string('B', 600);
            sendPort.Write(largeData + "\n");
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 小データはstackalloc、大データはArrayPool");
        }

        // Test9: 終端文字列がバッファ境界に位置するケース
        static void Test9_DelimiterAtBufferBoundary()
        {
            Console.WriteLine("\n--- Test9: 終端文字列がバッファ境界 ---");
            Debug.WriteLine("\n========== Test9: Delimiter At Buffer Boundary ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 20,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: {line}");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // バッファ境界で終端が来るように調整
            sendPort.Write("12345678901234567\n");  // 18バイト
            Thread.Sleep(100);
            sendPort.Write("AB\n");  // tailが境界を越える
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 境界をまたぐ終端の正しい検出");
        }

        // Test10: 複数バイト終端文字列が分割受信されるケース
        static void Test10_MultiByteDelimiterSplit()
        {
            Console.WriteLine("\n--- Test10: 複数バイト終端の分割受信 ---");
            Debug.WriteLine("\n========== Test10: Multi-byte Delimiter Split ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { 0x0D, 0x0A },  // \r\n
                maxBufferSize: 50,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: {line}");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // \r と \n を分けて送信
            sendPort.Write("Test\r");
            Thread.Sleep(50);
            sendPort.Write("\n");
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 分割された終端文字列の正しい検出");
        }

        // Test11: バッファサイズぴったりのデータ
        static void Test11_MaxBufferSizeExactly()
        {
            Console.WriteLine("\n--- Test11: バッファサイズぴったり ---");
            Debug.WriteLine("\n========== Test11: Max Buffer Size Exactly ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 10,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                Console.WriteLine($"  受信 #{receivedCount}: {lineBytes.Length}バイト");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // バッファサイズぴったり10バイト（終端含む）
            sendPort.Write("123456789\n");
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: バッファ満杯状態での正常処理");
        }

        // Test12: 連続的なオーバーフロー
        static void Test12_RepeatedOverflow()
        {
            Console.WriteLine("\n--- Test12: 連続オーバーフロー ---");
            Debug.WriteLine("\n========== Test12: Repeated Overflow ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 10,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            int overflowCount = 0;

            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: {line}");
            };

            reader.BufferOverflow += (sender, discardedBytes) =>
            {
                overflowCount++;
                Console.WriteLine($"  オーバーフロー #{overflowCount}: {discardedBytes}バイト");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 連続してオーバーフローを発生させる
            sendPort.Write("AAAAAAAAAAAAAAAA\n");  // 17バイト
            Thread.Sleep(100);
            sendPort.Write("BBBBBBBBBBBBBBBB\n");  // 17バイト
            Thread.Sleep(100);
            sendPort.Write("CCCCCCCCCCCCCCCC\n");  // 17バイト
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 複数回のオーバーフローでも正常動作");
        }

        // Test13: 終端文字列のみ
        static void Test13_DelimiterOnly()
        {
            Console.WriteLine("\n--- Test13: 終端文字列のみ ---");
            Debug.WriteLine("\n========== Test13: Delimiter Only ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 50,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                Console.WriteLine($"  受信 #{receivedCount}: {lineBytes.Length}バイト");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 終端のみ送信
            sendPort.Write("\n");
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 空行としてスキップ（イベント発火なし）");
        }

        // Test14: リング境界越えで大きいデータ（ArrayPool使用確認）
        static void Test14_LargeLineWithStackAlloc()
        {
            Console.WriteLine("\n--- Test14: 大きいデータのリング境界越え ---");
            Debug.WriteLine("\n========== Test14: Large Line With Ring Wrap ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 1024,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                Console.WriteLine($"  受信 #{receivedCount}: {lineBytes.Length}バイト");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // バッファの後半に位置させる
            string padding = new string('X', 900);
            sendPort.Write(padding + "\n");
            Thread.Sleep(100);

            // 600バイトのデータ（リング境界を越える & ArrayPool使用）
            string largeData = new string('Y', 600);
            sendPort.Write(largeData + "\n");
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: ArrayPoolを使用したリング境界越え");
        }

        // Test15: searchStartが境界付近にあるケース
        static void Test15_SearchStartAtBoundary()
        {
            Console.WriteLine("\n--- Test15: searchStartが境界付近 ---");
            Debug.WriteLine("\n========== Test15: SearchStart At Boundary ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 20,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: {line}");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // バッファをほぼ満たす
            sendPort.Write("12345678901234567");  // 17バイト（終端なし）
            Thread.Sleep(100);
            // 終端を追加
            sendPort.Write("8\n");
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: searchStartが更新された状態での終端検出");
        }

        // Test16: head/tailの位置が交互に変わるケース
        static void Test16_AlternatingHeadTailPositions()
        {
            Console.WriteLine("\n--- Test16: head/tail位置の交互変化 ---");
            Debug.WriteLine("\n========== Test16: Alternating Head/Tail Positions ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 30,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: {line}");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 様々なサイズのデータを送信してhead/tailを動かす
            sendPort.Write("A\n");           // 2バイト
            Thread.Sleep(50);
            sendPort.Write("BBBBBBBBBB\n");  // 11バイト
            Thread.Sleep(50);
            sendPort.Write("CCC\n");         // 4バイト
            Thread.Sleep(50);
            sendPort.Write("DDDDDDDDDDDDDD\n"); // 15バイト
            Thread.Sleep(50);
            sendPort.Write("EE\n");          // 3バイト
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 様々なhead/tail位置での正常動作");
        }

        // Test17: 終端の一部がバッファ末尾にあるケース
        static void Test17_PartialDelimiterAtEnd()
        {
            Console.WriteLine("\n--- Test17: 終端の一部がバッファ末尾 ---");
            Debug.WriteLine("\n========== Test17: Partial Delimiter At End ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { 0x0D, 0x0A, 0x0D, 0x0A },  // \r\n\r\n（4バイト）
                maxBufferSize: 50,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: {line}");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 終端の一部がバッファ末尾に来るように送信
            sendPort.Write("Data\r\n");
            Thread.Sleep(50);
            sendPort.Write("\r\n");
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 分割された複数バイト終端の検出");
        }

        // Test18: 連続する終端文字列
        static void Test18_ConsecutiveDelimiters()
        {
            Console.WriteLine("\n--- Test18: 連続する終端文字列 ---");
            Debug.WriteLine("\n========== Test18: Consecutive Delimiters ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 50,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                Console.WriteLine($"  受信 #{receivedCount}: {lineBytes.Length}バイト");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 連続する終端文字列
            sendPort.Write("\n\n\n");
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 空行はスキップされる");
        }

        // Test19: 1バイトずつ受信
        static void Test19_SingleByteReads()
        {
            Console.WriteLine("\n--- Test19: 1バイトずつ受信 ---");
            Debug.WriteLine("\n========== Test19: Single Byte Reads ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 50,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: {line}");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 1バイトずつ送信
            string data = "Test\n";
            foreach (char c in data)
            {
                sendPort.Write(c.ToString());
                Thread.Sleep(10);
            }
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 1バイトずつでも正常に行を組み立て");
        }

        // Test20: バッファ満杯で終端なし
        static void Test20_FullBufferNoDelimiter()
        {
            Console.WriteLine("\n--- Test20: バッファ満杯・終端なし ---");
            Debug.WriteLine("\n========== Test20: Full Buffer No Delimiter ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 10,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                Console.WriteLine($"  受信 #{receivedCount}: {lineBytes.Length}バイト");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 終端なしでバッファを満たす
            sendPort.Write("1234567890");
            Thread.Sleep(100);

            // さらにデータを送信（オーバーフローするが終端なし）
            sendPort.Write("ABCDE");
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 終端が来るまでイベント発火なし");
        }

        // Test21: バッファオーバーフロー後に終端受信（不完全データ）
        static void Test21_OverflowThenDelimiter()
        {
            Console.WriteLine("\n--- Test21: オーバーフロー後の終端受信 ---");
            Debug.WriteLine("\n========== Test21: Overflow Then Delimiter ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 10,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            int overflowCount = 0;

            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: '{line}' ({lineBytes.Length}バイト)");
                Console.WriteLine($"  ⚠️ これは不完全なデータ（先頭が欠けている）");
            };

            reader.BufferOverflow += (sender, discardedBytes) =>
            {
                overflowCount++;
                Console.WriteLine($"  オーバーフロー #{overflowCount}: {discardedBytes}バイト破棄");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // バッファサイズ(10)を超えるデータを送信し、その後に終端を送信
            // "ABCDEFGHIJKLMNOPQRSTUVWXYZ\n" = 27バイト
            // バッファは10バイトなので、17バイト破棄され、"QRSTUVWXYZ"が残る
            // その後"\n"が来て、"QRSTUVWXYZ"が受信される（先頭の"ABCDEFGHIJKLMNOP"は失われている）
            sendPort.Write("ABCDEFGHIJKLMNOPQRSTUVWXYZ\n");
            Thread.Sleep(200);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 不完全なデータ（途中から）が受信される");
            Console.WriteLine($"  問題: 先頭が欠けたデータがイベント通知される");
        }

        // Test22: オーバーフロー後に終端なしでさらにオーバーフロー
        static void Test22_ContinuousOverflowNoDelimiter()
        {
            Console.WriteLine("\n--- Test22: 終端なしの連続オーバーフロー ---");
            Debug.WriteLine("\n========== Test22: Continuous Overflow No Delimiter ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 10,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            int overflowCount = 0;

            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: '{line}' ({lineBytes.Length}バイト)");
            };

            reader.BufferOverflow += (sender, discardedBytes) =>
            {
                overflowCount++;
                Console.WriteLine($"  オーバーフロー #{overflowCount}: {discardedBytes}バイト破棄");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 終端なしで大量のデータを送信
            sendPort.Write("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");  // 40バイト
            Thread.Sleep(100);
            sendPort.Write("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");  // 40バイト
            Thread.Sleep(100);
            // 最後に終端付きデータ
            sendPort.Write("CCCCCCCCCC\n");  // 11バイト
            Thread.Sleep(200);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  受信回数: {receivedCount}");
            Console.WriteLine($"  オーバーフロー回数: {overflowCount}");
            Console.WriteLine($"  期待: 最後の10バイト程度のみ受信");
        }

        // Test23: オーバーフロー後の複数行受信
        static void Test23_OverflowThenMultipleLines()
        {
            Console.WriteLine("\n--- Test23: オーバーフロー後の複数行 ---");
            Debug.WriteLine("\n========== Test23: Overflow Then Multiple Lines ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 15,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            int overflowCount = 0;

            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: '{line}' ({lineBytes.Length}バイト)");
            };

            reader.BufferOverflow += (sender, discardedBytes) =>
            {
                overflowCount++;
                Console.WriteLine($"  オーバーフロー #{overflowCount}: {discardedBytes}バイト破棄");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 長いデータ（オーバーフローする）+ 複数の短い行
            sendPort.Write("VERYLONGDATAAAAAAAAAAAA\n");  // 24バイト（オーバーフロー）
            Thread.Sleep(100);
            sendPort.Write("OK1\n");  // 4バイト
            Thread.Sleep(50);
            sendPort.Write("OK2\n");  // 4バイト
            Thread.Sleep(200);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 1行目は不完全、2-3行目は正常");
        }

        // Test24: 統計情報の確認
        static void Test24_Statistics()
        {
            Console.WriteLine("\n--- Test24: 統計情報 ---");
            Debug.WriteLine("\n========== Test24: Statistics ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 20,
                ownsSerialPort: true,
                enableDebugOutput: false
            );

            reader.LineReceived += (sender, lineBytes) => { };
            reader.BufferOverflow += (sender, discardedBytes) => { };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 様々なパターンのデータを送信
            sendPort.Write("Line1\n");           // 通常行
            Thread.Sleep(50);
            sendPort.Write("\n");                // 空行
            Thread.Sleep(50);
            sendPort.Write("Line2\n");           // 通常行
            Thread.Sleep(50);
            sendPort.Write("VERYLONGDATAAAAAAAAAAAAAAAAAA\n");  // オーバーフロー
            Thread.Sleep(50);
            sendPort.Write("Line3\n");           // 通常行
            Thread.Sleep(100);

            var stats = reader.GetStatistics();

            Console.WriteLine($"  受信行数: {stats.TotalLinesReceived}");
            Console.WriteLine($"  受信バイト数: {stats.TotalBytesReceived}");
            Console.WriteLine($"  オーバーフロー回数: {stats.TotalOverflowCount}");
            Console.WriteLine($"  破棄バイト数: {stats.TotalBytesDiscarded}");
            Console.WriteLine($"  空行スキップ数: {stats.TotalEmptyLinesSkipped}");
            Console.WriteLine($"  ピークバッファ: {stats.PeakBufferUsage}/{stats.MaxBufferSize}");
            Console.WriteLine($"  現在のバッファ: {stats.CurrentBufferUsage}/{stats.MaxBufferSize}");
            Console.WriteLine($"  バッファ使用率: {stats.BufferUsageRate:P1}");
            Console.WriteLine($"  ピーク使用率: {stats.PeakBufferUsageRate:P1}");
            Console.WriteLine($"  簡易表示: {stats}");

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 受信3行, 空行1行, オーバーフロー1回");
        }


        // Test25: 基本的なバッファ破棄
        static void Test25_DiscardBuffer_Basic()
        {
            Console.WriteLine("\n--- Test25: 基本的なバッファ破棄 ---");
            Debug.WriteLine("\n========== Test25: Discard Buffer Basic ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 50,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: '{line}'");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 終端なしのデータを送信
            sendPort.Write("PartialData");
            Thread.Sleep(100);

            Console.WriteLine($"  送信後のバッファ: {reader.CurrentBufferUsage}バイト");

            // バッファを破棄
            int discarded = reader.DiscardBuffer();
            Console.WriteLine($"  破棄したバイト数: {discarded}");
            Console.WriteLine($"  破棄後のバッファ: {reader.CurrentBufferUsage}バイト");

            // 新しいデータを送信
            sendPort.Write("NewLine\n");
            Thread.Sleep(100);

            var stats = reader.GetStatistics();
            Console.WriteLine($"  受信行数: {stats.TotalLinesReceived}");
            Console.WriteLine($"  破棄回数: {stats.TotalDiscardCount}");
            Console.WriteLine($"  破棄バイト数: {stats.TotalBytesDiscarded}");

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: PartialDataは破棄され、NewLineのみ受信");
            Console.WriteLine($"  実際: {receivedCount}行受信");
        }

        // Test26: 空バッファの破棄
        static void Test26_DiscardBuffer_Empty()
        {
            Console.WriteLine("\n--- Test26: 空バッファの破棄 ---");
            Debug.WriteLine("\n========== Test26: Discard Empty Buffer ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 50,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            Console.WriteLine($"  初期バッファ: {reader.CurrentBufferUsage}バイト");

            // 空のバッファを破棄
            int discarded = reader.DiscardBuffer();
            Console.WriteLine($"  破棄したバイト数: {discarded}");
            Console.WriteLine($"  破棄後のバッファ: {reader.CurrentBufferUsage}バイト");

            var stats = reader.GetStatistics();
            Console.WriteLine($"  破棄回数: {stats.TotalDiscardCount}");

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 0バイト破棄、破棄回数は1回");
        }

        // Test27: リング状態でのバッファ破棄
        static void Test27_DiscardBuffer_WithRingWrap()
        {
            Console.WriteLine("\n--- Test27: リング状態でのバッファ破棄 ---");
            Debug.WriteLine("\n========== Test27: Discard With Ring Wrap ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 20,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: '{line}'");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // バッファの後半に位置させる
            sendPort.Write("First\n");
            Thread.Sleep(100);
            sendPort.Write("Second\n");
            Thread.Sleep(100);

            // 終端なしのデータを送信（リング状態になる）
            sendPort.Write("PartialThird");
            Thread.Sleep(100);

            Console.WriteLine($"  破棄前のバッファ: {reader.CurrentBufferUsage}バイト");

            // リング状態のバッファを破棄
            int discarded = reader.DiscardBuffer();
            Console.WriteLine($"  破棄したバイト数: {discarded}");
            Console.WriteLine($"  破棄後のバッファ: {reader.CurrentBufferUsage}バイト");

            // 新しいデータを送信
            sendPort.Write("NewData\n");
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: First, Second, NewDataを受信（PartialThirdは破棄）");
            Console.WriteLine($"  実際: {receivedCount}行受信");
        }

        // Test28: 統計情報の確認
        static void Test28_DiscardBuffer_Statistics()
        {
            Console.WriteLine("\n--- Test28: 統計情報の確認 ---");
            Debug.WriteLine("\n========== Test28: Discard Statistics ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 30,
                ownsSerialPort: true,
                enableDebugOutput: false
            );

            reader.LineReceived += (sender, lineBytes) => { };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // データ送信と破棄を繰り返す
            sendPort.Write("Data1");
            Thread.Sleep(50);
            int d1 = reader.DiscardBuffer();

            sendPort.Write("Data2Data2");
            Thread.Sleep(50);
            int d2 = reader.DiscardBuffer();

            sendPort.Write("Data3Data3Data3");
            Thread.Sleep(50);
            int d3 = reader.DiscardBuffer();

            sendPort.Write("FinalLine\n");
            Thread.Sleep(100);

            var stats = reader.GetStatistics();

            Console.WriteLine($"  破棄回数: {stats.TotalDiscardCount}");
            Console.WriteLine($"  破棄バイト数: {stats.TotalBytesDiscarded}");
            Console.WriteLine($"  受信行数: {stats.TotalLinesReceived}");
            Console.WriteLine($"  破棄詳細: d1={d1}, d2={d2}, d3={d3}");

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 破棄回数=3, 受信行数=1");
        }

        // Test29: 部分データ受信後の破棄
        static void Test29_DiscardBuffer_AfterPartialData()
        {
            Console.WriteLine("\n--- Test29: 部分データ受信後の破棄 ---");
            Debug.WriteLine("\n========== Test29: Discard After Partial Data ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 50,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: '{line}'");
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 部分的なデータを複数回送信
            sendPort.Write("Part");
            Thread.Sleep(50);
            sendPort.Write("ial");
            Thread.Sleep(50);
            sendPort.Write("Data");
            Thread.Sleep(50);

            Console.WriteLine($"  部分データ蓄積後: {reader.CurrentBufferUsage}バイト");

            // 破棄
            int discarded = reader.DiscardBuffer();
            Console.WriteLine($"  破棄: {discarded}バイト");

            // 完全なデータを送信
            sendPort.Write("CompleteLine\n");
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: CompleteLineのみ受信");
            Console.WriteLine($"  実際: {receivedCount}行受信");
        }

        // Test30: 連続的な破棄
        static void Test30_DiscardBuffer_MultipleDiscards()
        {
            Console.WriteLine("\n--- Test30: 連続的な破棄 ---");
            Debug.WriteLine("\n========== Test30: Multiple Discards ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 50,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 連続して破棄
            int d1 = reader.DiscardBuffer();
            int d2 = reader.DiscardBuffer();
            int d3 = reader.DiscardBuffer();

            Console.WriteLine($"  連続破棄: d1={d1}, d2={d2}, d3={d3}");

            // データ送信後に連続破棄
            sendPort.Write("Test");
            Thread.Sleep(50);
            int d4 = reader.DiscardBuffer();
            int d5 = reader.DiscardBuffer();

            Console.WriteLine($"  データ後の連続破棄: d4={d4}, d5={d5}");

            var stats = reader.GetStatistics();
            Console.WriteLine($"  破棄回数: {stats.TotalDiscardCount}");
            Console.WriteLine($"  破棄バイト数: {stats.TotalBytesDiscarded}");

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: 破棄回数=5, 空破棄は0バイト");
        }

        // Test31: 受信中の破棄
        static void Test31_DiscardBuffer_DuringReceive()
        {
            Console.WriteLine("\n--- Test31: 受信中の破棄 ---");
            Debug.WriteLine("\n========== Test31: Discard During Receive ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 50,
                ownsSerialPort: true,
                enableDebugOutput: true
            );

            int receivedCount = 0;
            bool shouldDiscard = false;

            reader.LineReceived += (sender, lineBytes) =>
            {
                receivedCount++;
                string line = Encoding.UTF8.GetString(lineBytes);
                Console.WriteLine($"  受信 #{receivedCount}: '{line}'");

                // 特定の条件で破棄
                if (line.Contains("ERROR"))
                {
                    shouldDiscard = true;
                }
            };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 正常データ
            sendPort.Write("NormalLine\n");
            Thread.Sleep(100);

            // エラーデータ
            sendPort.Write("ERROR_LINE\n");
            Thread.Sleep(100);

            if (shouldDiscard)
            {
                // エラー後に部分データがある場合を想定
                sendPort.Write("PartialAfterError");
                Thread.Sleep(50);

                int discarded = reader.DiscardBuffer();
                Console.WriteLine($"  エラー後の破棄: {discarded}バイト");
            }

            // 新しいデータ
            sendPort.Write("RecoveryLine\n");
            Thread.Sleep(100);

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: NormalLine, ERROR_LINE, RecoveryLineを受信");
            Console.WriteLine($"  実際: {receivedCount}行受信");
        }

        // Test32: ピーク使用量との関係
        static void Test32_DiscardBuffer_WithPeakUsage()
        {
            Console.WriteLine("\n--- Test32: ピーク使用量との関係 ---");
            Debug.WriteLine("\n========== Test32: Discard With Peak Usage ==========");

            using var receivePort = new SerialPort("COM7", 9600);
            using var sendPort = new SerialPort("COM8", 9600);

            using var reader = new SerialPortReader(
                receivePort,
                delimiter: new byte[] { (byte)'\n' },
                maxBufferSize: 50,
                ownsSerialPort: true,
                enableDebugOutput: false
            );

            reader.LineReceived += (sender, lineBytes) => { };

            receivePort.Open();
            sendPort.Open();
            reader.Attach();

            // 大きなデータを送信
            string largeData = new string('X', 40);
            sendPort.Write(largeData);
            Thread.Sleep(100);

            var stats1 = reader.GetStatistics();
            Console.WriteLine($"  大量データ後: Current={stats1.CurrentBufferUsage}, Peak={stats1.PeakBufferUsage}");

            // 破棄
            int discarded = reader.DiscardBuffer();
            Console.WriteLine($"  破棄: {discarded}バイト");

            var stats2 = reader.GetStatistics();
            Console.WriteLine($"  破棄後: Current={stats2.CurrentBufferUsage}, Peak={stats2.PeakBufferUsage}");

            // 小さなデータを送信
            sendPort.Write("Small\n");
            Thread.Sleep(100);

            var stats3 = reader.GetStatistics();
            Console.WriteLine($"  小データ後: Current={stats3.CurrentBufferUsage}, Peak={stats3.PeakBufferUsage}");

            reader.Detach();
            sendPort.Close();

            Console.WriteLine($"  期待: Peakは破棄後も保持される");
            Console.WriteLine($"  実際: Peak={stats3.PeakBufferUsage}バイト");
        }

        //static void Main(string[] args)
        //{
        //    var serialPort = new SerialPort("COM9");
        //    using var reader = new SerialPortReader(
        //        serialPort,
        //        delimiter: new byte[] { (byte)'\r' },
        //        maxBufferSize: 10,
        //        ownsSerialPort: true
        //    );

        //    reader.LineReceived += (sender, lineBytes) =>
        //    {
        //        Console.WriteLine($"受信: {Convert.ToHexString(lineBytes)}");
        //    };

        //    reader.BufferOverflow += (sender, bufferSize) =>
        //    {
        //        Console.WriteLine($"警告: バッファオーバーフロー ({bufferSize} bytes)");
        //    };

        //    serialPort.Open();
        //    reader.Attach();

        //    Console.WriteLine("データ受信中... (Enterキーで終了)");
        //    Console.ReadLine();

        //    reader.Detach();
        //    serialPort.Close();
        //}
    }
}


public class SerialPortReader : IDisposable
{
    private readonly SerialPort _serialPort;
    private readonly object _lockObject = new();
    private readonly byte[] _delimiter;
    private readonly int _maxBufferSize;
    private readonly bool _ownsSerialPort;
    private readonly bool _enableDebugOutput;
    private byte[] _buffer;
    private int _head;  // 読み取り開始位置
    private int _tail;  // 書き込み位置
    private int _count; // バッファ内のデータ数
    private int _searchStart; // 次回の終端検索開始位置（headからの相対位置）
    private bool _isAttached;

    private const int StackAllocThreshold = 512; // stackallocを使用する閾値

    // 統計情報
    private long _totalLinesReceived;
    private long _totalBytesReceived;
    private long _totalOverflowCount;
    private long _totalBytesDiscarded;
    private long _totalEmptyLinesSkipped;
    private int _peakBufferUsage;
    private long _totalDiscardCount;  // Discardメソッドの呼び出し回数

    /// <summary>
    /// 終端文字列区切りでデータを受信したときに発火するイベント
    /// </summary>
    public event EventHandler<ReadOnlySpan<byte>>? LineReceived;

    /// <summary>
    /// バッファオーバーフロー時に発火するイベント（破棄されたバイト数を通知）
    /// </summary>
    /// <remarks>
    /// このイベントが発火した後に受信されるLineReceivedイベントのデータは、
    /// 先頭部分が欠けた不完全なデータである可能性があります。
    /// </remarks>
    public event EventHandler<int>? BufferOverflow;

    #region 統計情報プロパティ

    /// <summary>
    /// 受信した行の総数
    /// </summary>
    public long TotalLinesReceived => _totalLinesReceived;

    /// <summary>
    /// 受信したバイトの総数（終端文字列を含む）
    /// </summary>
    public long TotalBytesReceived => _totalBytesReceived;

    /// <summary>
    /// バッファオーバーフローが発生した回数
    /// </summary>
    public long TotalOverflowCount => _totalOverflowCount;

    /// <summary>
    /// 破棄されたバイトの総数
    /// </summary>
    public long TotalBytesDiscarded => _totalBytesDiscarded;

    /// <summary>
    /// スキップされた空行の総数
    /// </summary>
    public long TotalEmptyLinesSkipped => _totalEmptyLinesSkipped;

    /// <summary>
    /// Discardメソッドの呼び出し回数
    /// </summary>
    public long TotalDiscardCount => _totalDiscardCount;

    /// <summary>
    /// バッファ使用量のピーク値（バイト）
    /// </summary>
    public int PeakBufferUsage => _peakBufferUsage;

    /// <summary>
    /// 現在のバッファ使用量（バイト）
    /// </summary>
    public int CurrentBufferUsage
    {
        get
        {
            lock (_lockObject)
            {
                return _count;
            }
        }
    }

    /// <summary>
    /// 最大バッファサイズ（バイト）
    /// </summary>
    public int MaxBufferSize => _maxBufferSize;

    /// <summary>
    /// バッファ使用率（0.0～1.0）
    /// </summary>
    public double BufferUsageRate => (double)CurrentBufferUsage / _maxBufferSize;

    /// <summary>
    /// ピークバッファ使用率（0.0～1.0）
    /// </summary>
    public double PeakBufferUsageRate => (double)_peakBufferUsage / _maxBufferSize;

    #endregion

    /// <summary>
    /// SerialPortReaderを初期化します
    /// </summary>
    /// <param name="serialPort">使用するSerialPortインスタンス</param>
    /// <param name="delimiter">終端文字列のバイト配列（デフォルト: \n）</param>
    /// <param name="maxBufferSize">最大バッファサイズ（デフォルト: 65536）</param>
    /// <param name="ownsSerialPort">SerialPortの所有権を持つか（Dispose時にSerialPortもDisposeする）</param>
    /// <param name="enableDebugOutput">デバッグ出力を有効にするか</param>
    public SerialPortReader(
        SerialPort serialPort,
        byte[]? delimiter = null,
        int maxBufferSize = 65536,
        bool ownsSerialPort = false,
        bool enableDebugOutput = false)
    {
        _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
        _delimiter = delimiter ?? new byte[] { (byte)'\n' };
        _maxBufferSize = maxBufferSize;
        _ownsSerialPort = ownsSerialPort;
        _enableDebugOutput = enableDebugOutput;
        _buffer = ArrayPool<byte>.Shared.Rent(maxBufferSize);
        _head = 0;
        _tail = 0;
        _count = 0;
        _searchStart = 0;
        _isAttached = false;

        // 統計情報の初期化
        ResetStatistics();

        if (_delimiter.Length == 0)
        {
            throw new ArgumentException("Delimiter cannot be empty", nameof(delimiter));
        }

        //if (maxBufferSize < 256)
        //{
        //    throw new ArgumentException("Max buffer size must be at least 256 bytes", nameof(maxBufferSize));
        //}

        DebugLog($"[Init] MaxBufferSize={maxBufferSize}, DelimiterLength={_delimiter.Length}");
    }

    /// <summary>
    /// 統計情報をリセットします
    /// </summary>
    public void ResetStatistics()
    {
        lock (_lockObject)
        {
            _totalLinesReceived = 0;
            _totalBytesReceived = 0;
            _totalOverflowCount = 0;
            _totalBytesDiscarded = 0;
            _totalEmptyLinesSkipped = 0;
            _totalDiscardCount = 0;
            _peakBufferUsage = 0;
        }
    }

    /// <summary>
    /// バッファに溜まっているデータを破棄し、位置情報を初期状態にリセットします
    /// </summary>
    /// <returns>破棄されたバイト数</returns>
    public int DiscardBuffer()
    {
        lock (_lockObject)
        {
            int discardedBytes = _count;

            // [MEMO] これを採用するか？
            // 統計情報を更新（空でも呼び出し回数はカウント）
            _totalDiscardCount++;

            if (discardedBytes > 0)
            {
                DebugLog($"[Discard] Discarding {discardedBytes} bytes from buffer");
                _totalBytesDiscarded += discardedBytes;
            }
            else
            {
                DebugLog($"[Discard] Buffer is already empty");
            }

            // 位置情報を初期状態にリセット
            _head = 0;
            _tail = 0;
            _count = 0;
            _searchStart = 0;

            DebugLog($"[Discard] Buffer reset: head=0, tail=0, count=0, searchStart=0");

            return discardedBytes;
        }
    }
    /// <summary>
    /// 統計情報を取得します
    /// </summary>
    public Statistics GetStatistics()
    {
        lock (_lockObject)
        {
            return new Statistics
            {
                TotalLinesReceived = _totalLinesReceived,
                TotalBytesReceived = _totalBytesReceived,
                TotalOverflowCount = _totalOverflowCount,
                TotalBytesDiscarded = _totalBytesDiscarded,
                TotalEmptyLinesSkipped = _totalEmptyLinesSkipped,
                TotalDiscardCount = _totalDiscardCount,
                PeakBufferUsage = _peakBufferUsage,
                CurrentBufferUsage = _count,
                MaxBufferSize = _maxBufferSize
            };
        }
    }

    /// <summary>
    /// データ受信の監視を開始します
    /// </summary>
    public void Attach()
    {
        if (_isAttached)
        {
            return;
        }

        _serialPort.DataReceived += OnDataReceived;
        _isAttached = true;

        DebugLog("[Attach] Started");
    }

    /// <summary>
    /// データ受信の監視を停止します
    /// </summary>
    public void Detach()
    {
        if (!_isAttached)
        {
            return;
        }

        _serialPort.DataReceived -= OnDataReceived;
        _isAttached = false;

        DebugLog("[Detach] Stopped");
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        lock (_lockObject)
        {
            try
            {
                int bytesToRead = _serialPort.BytesToRead;
                if (bytesToRead == 0) return;

                DebugLog($"[Receive] BytesToRead={bytesToRead}, Before: head={_head}, tail={_tail}, count={_count}, searchStart={_searchStart}");

                // 受信データをリングバッファに書き込み
                WriteToRingBuffer(bytesToRead);

                DebugLog($"[Write] After: head={_head}, tail={_tail}, count={_count}");

                // 終端文字列を探して処理
                ProcessLines();

                DebugLog($"[Process] After: head={_head}, tail={_tail}, count={_count}, searchStart={_searchStart}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Error] {ex.Message}");
                Debug.WriteLine($"[Error] StackTrace: {ex.StackTrace}");
            }
        }
    }

    private void WriteToRingBuffer(int bytesToRead)
    {
        int availableSpace = _maxBufferSize - _count;
        int bytesToWrite = bytesToRead;
        int discardedBytes = 0;

        // バッファが満杯の場合、古いデータを破棄
        if (bytesToRead > availableSpace)
        {
            discardedBytes = bytesToRead - availableSpace;
            DebugLog($"[Overflow] Discarding={discardedBytes} bytes");

            // 統計情報を更新
            _totalOverflowCount++;
            _totalBytesDiscarded += discardedBytes;

            // 古いデータを破棄（headを進める）
            int oldHead = _head;
            _head = (_head + discardedBytes) % _maxBufferSize;
            _count -= discardedBytes;

            // 検索開始位置を調整
            _searchStart = Math.Max(0, _searchStart - discardedBytes);

            DebugLog($"[Overflow] head: {oldHead}->{_head}, count={_count}, searchStart={_searchStart}");

            BufferOverflow?.Invoke(this, discardedBytes);
        }

        // データを読み込み
        int totalBytesRead = 0;
        while (totalBytesRead < bytesToWrite)
        {
            // 現在のtail位置から書き込める連続領域のサイズを計算
            int contiguousSpace;
            if (_count == 0)
            {
                contiguousSpace = _maxBufferSize - _tail;
            }
            else if (_tail >= _head)
            {
                contiguousSpace = _maxBufferSize - _tail;
            }
            else
            {
                contiguousSpace = _head - _tail;
            }

            int chunkSize = Math.Min(bytesToWrite - totalBytesRead, contiguousSpace);

            if (chunkSize <= 0)
            {
                DebugLog($"[Write] Error: chunkSize={chunkSize}, tail={_tail}, head={_head}, count={_count}, contiguousSpace={contiguousSpace}");
                break;
            }

            int bytesRead = _serialPort.Read(_buffer, _tail, chunkSize);

            if (bytesRead == 0) break;

            DebugLog($"[Write] Position={_tail}, Length={bytesRead}");

            _tail = (_tail + bytesRead) % _maxBufferSize;
            _count += bytesRead;
            totalBytesRead += bytesRead;

            // 統計情報を更新
            _totalBytesReceived += bytesRead;

            // ピークバッファ使用量を更新
            if (_count > _peakBufferUsage)
            {
                _peakBufferUsage = _count;
            }
        }
    }

    private void ProcessLines()
    {
        int lineCount = 0;

        while (_count > 0)
        {
            // 前回の検索位置から終端文字列を検索
            int delimiterIndex = FindDelimiterInRingBuffer();

            if (delimiterIndex == -1)
            {
                // 終端が見つからない場合、次回の検索開始位置を更新
                _searchStart = Math.Max(0, _count - _delimiter.Length + 1);
                DebugLog($"[Search] Delimiter not found, searchStart updated to {_searchStart}");
                break;
            }

            lineCount++;
            DebugLog($"[Process] Line#{lineCount}, DelimiterAt={delimiterIndex}, LineLength={delimiterIndex}");

            // 終端までのデータを取得してイベント発火
            if (delimiterIndex > 0)
            {
                // 統計情報を更新
                _totalLinesReceived++;

                // データをコピーせずに処理できる場合
                if (_head + delimiterIndex <= _maxBufferSize)
                {
                    // 連続したメモリ領域として処理
                    ReadOnlySpan<byte> line = _buffer.AsSpan(_head, delimiterIndex);
                    DebugLog($"[Process] Contiguous read: offset={_head}, length={delimiterIndex}");
                    LineReceived?.Invoke(this, line);
                }
                else
                {
                    // リングバッファの境界をまたぐ場合
                    ProcessRingWrapLine(delimiterIndex);
                }
            }
            else
            {
                // 空行
                _totalEmptyLinesSkipped++;
                DebugLog($"[Process] Empty line skipped");
            }

            // 処理済みデータと終端文字列を削除
            int bytesToRemove = delimiterIndex + _delimiter.Length;
            DebugLog($"[Process] Removing {bytesToRemove} bytes (line={delimiterIndex}, delimiter={_delimiter.Length})");

            _head = (_head + bytesToRemove) % _maxBufferSize;
            _count -= bytesToRemove;

            // 検索開始位置をリセット（新しい行の検索は先頭から）
            _searchStart = 0;
        }
    }

    private void ProcessRingWrapLine(int lineLength)
    {
        // サイズが小さい場合はstackallocを使用
        if (lineLength <= StackAllocThreshold)
        {
            Span<byte> tempBuffer = stackalloc byte[lineLength];
            CopyFromRingBuffer(tempBuffer);
            DebugLog($"[Process] Ring-wrap read (stackalloc): length={lineLength}");
            LineReceived?.Invoke(this, tempBuffer);
        }
        else
        {
            // サイズが大きい場合はArrayPoolを使用
            byte[] tempBuffer = ArrayPool<byte>.Shared.Rent(lineLength);
            try
            {
                CopyFromRingBuffer(tempBuffer.AsSpan(0, lineLength));
                ReadOnlySpan<byte> line = tempBuffer.AsSpan(0, lineLength);
                DebugLog($"[Process] Ring-wrap read (ArrayPool): length={lineLength}");
                LineReceived?.Invoke(this, line);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(tempBuffer);
            }
        }
    }

    private int FindDelimiterInRingBuffer()
    {
        // 検索に必要な最小データ量をチェック
        if (_count < _delimiter.Length)
        {
            return -1;
        }

        // 検索開始位置から検索
        int searchEnd = _count - _delimiter.Length + 1;

        DebugLog($"[Search] Start={_searchStart}, End={searchEnd}, Count={_count}");

        // 単一バイトの終端文字列の場合は最適化
        if (_delimiter.Length == 1)
        {
            byte delimiterByte = _delimiter[0];
            for (int i = _searchStart; i < _count; i++)
            {
                int index = (_head + i) % _maxBufferSize;
                if (_buffer[index] == delimiterByte)
                {
                    DebugLog($"[Search] Found at position {i}");
                    return i;
                }
            }
            return -1;
        }

        // 複数バイトの終端文字列の場合
        for (int i = _searchStart; i < searchEnd; i++)
        {
            bool found = true;
            for (int j = 0; j < _delimiter.Length; j++)
            {
                int index = (_head + i + j) % _maxBufferSize;
                if (_buffer[index] != _delimiter[j])
                {
                    found = false;
                    break;
                }
            }
            if (found)
            {
                DebugLog($"[Search] Found at position {i}");
                return i;
            }
        }

        return -1;
    }

    private void CopyFromRingBuffer(Span<byte> destination)
    {
        int sourceIndex = _head;
        int remaining = destination.Length;
        int destIndex = 0;

        while (remaining > 0)
        {
            int contiguousLength = Math.Min(remaining, _maxBufferSize - sourceIndex);
            _buffer.AsSpan(sourceIndex, contiguousLength).CopyTo(destination.Slice(destIndex, contiguousLength));

            sourceIndex = (sourceIndex + contiguousLength) % _maxBufferSize;
            destIndex += contiguousLength;
            remaining -= contiguousLength;
        }
    }

    private void DebugLog(string message)
    {
        if (_enableDebugOutput)
        {
            Debug.WriteLine(message);
        }
    }

    public void Dispose()
    {
        Detach();

        if (_buffer != null)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = null!;
        }

        if (_ownsSerialPort)
        {
            _serialPort?.Dispose();
        }
    }

    /// <summary>
    /// 統計情報を表すクラス
    /// </summary>
    public class Statistics
    {
        /// <summary>受信した行の総数</summary>
        public long TotalLinesReceived { get; init; }

        /// <summary>受信したバイトの総数</summary>
        public long TotalBytesReceived { get; init; }

        /// <summary>バッファオーバーフローが発生した回数</summary>
        public long TotalOverflowCount { get; init; }

        /// <summary>破棄されたバイトの総数</summary>
        public long TotalBytesDiscarded { get; init; }

        /// <summary>スキップされた空行の総数</summary>
        public long TotalEmptyLinesSkipped { get; init; }

        /// <summary>Discardメソッドの呼び出し回数</summary>
        public long TotalDiscardCount { get; init; }

        /// <summary>バッファ使用量のピーク値（バイト）</summary>
        public int PeakBufferUsage { get; init; }

        /// <summary>現在のバッファ使用量（バイト）</summary>
        public int CurrentBufferUsage { get; init; }

        /// <summary>最大バッファサイズ（バイト）</summary>
        public int MaxBufferSize { get; init; }

        /// <summary>バッファ使用率（0.0～1.0）</summary>
        public double BufferUsageRate => (double)CurrentBufferUsage / MaxBufferSize;

        /// <summary>ピークバッファ使用率（0.0～1.0）</summary>
        public double PeakBufferUsageRate => (double)PeakBufferUsage / MaxBufferSize;

        public override string ToString()
        {
            return $"Lines: {TotalLinesReceived}, " +
                   $"Bytes: {TotalBytesReceived}, " +
                   $"Overflows: {TotalOverflowCount}, " +
                   $"Discarded: {TotalBytesDiscarded}, " +
                   $"ManualDiscards: {TotalDiscardCount}, " +
                   $"EmptyLines: {TotalEmptyLinesSkipped}, " +
                   $"Buffer: {CurrentBufferUsage}/{MaxBufferSize} ({BufferUsageRate:P1}), " +
                   $"PeakBuffer: {PeakBufferUsage}/{MaxBufferSize} ({PeakBufferUsageRate:P1})";
        }
    }
}
