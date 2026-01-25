namespace WorkFile
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const string inputFile = "file.txt";
            const string outputFile = "file.dat";
            const int startAddress = 0x021B;

            try
            {
                // file.txtを読み込む
                if (!File.Exists(inputFile))
                {
                    Console.WriteLine($"Error: {inputFile} が見つかりません。");
                    return;
                }

                var bytes = new List<byte>();
                var lines = File.ReadAllLines(inputFile);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // 行を空白で分割（最初の要素がアドレス、残りがデータ）
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        continue;

                    // 行の開始アドレスを解析
                    if (!int.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out int lineAddress))
                        continue;

                    // この行の各バイトについて、実際のアドレスを計算して処理
                    for (int i = 1; i < parts.Length; i++)
                    {
                        // 各バイトの実際のアドレス = 行の開始アドレス + (バイトのインデックス - 1)
                        int byteAddress = lineAddress + (i - 1);

                        // 021B以降のバイトのみ追加
                        if (byteAddress >= startAddress)
                        {
                            if (byte.TryParse(parts[i], System.Globalization.NumberStyles.HexNumber, null, out byte b))
                            {
                                bytes.Add(b);
                            }
                        }
                    }
                }

                // file.datとして保存
                File.WriteAllBytes(outputFile, bytes.ToArray());
                Console.WriteLine($"完了: {bytes.Count} バイトを {outputFile} に保存しました。");
                Console.WriteLine($"開始アドレス: 0x{startAddress:X4}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"エラー: {ex.Message}");
            }
        }
    }
}
