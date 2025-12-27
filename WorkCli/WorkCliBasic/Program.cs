using System.CommandLine;
using WorkCliBasic;

namespace WorkCliBasic
{
    class Program
    {
        static int Main(string[] args)
        {
            RootCommand rootCommand = new("WorkCliBasic - System.CommandLine 2.0.1 機能サンプル");

            // グローバルオプション（全コマンドで使用可能）
            Option<bool> verboseOption = new("--verbose", "-v")
            {
                Description = "詳細な出力を表示"
            };
            verboseOption.Recursive = true;
            rootCommand.Options.Add(verboseOption);

            // 1. 基本的なmessageコマンド
            rootCommand.Subcommands.Add(CreateMessageCommand());

            // 2. ファイル操作コマンド（サブサブコマンドの例）
            rootCommand.Subcommands.Add(CreateFileCommand());

            // 3. 各種オプションタイプのデモコマンド
            rootCommand.Subcommands.Add(CreateOptionsCommand());

            // 4. 引数を使用するコマンド
            rootCommand.Subcommands.Add(CreateCalculatorCommand());

            // 5. カスタム検証を使用するコマンド
            rootCommand.Subcommands.Add(CreateValidationCommand());

            return rootCommand.Parse(args).Invoke();
        }

        // 1. 基本的なmessageコマンド
        static Command CreateMessageCommand()
        {
            Option<string> messageOption = new("--message", "-m")
            {
                Description = "表示するメッセージ",
                Required = true
            };

            Option<int> repeatOption = new("--repeat", "-r")
            {
                Description = "繰り返し回数",
                DefaultValueFactory = parseResult => 1
            };

            Option<ConsoleColor> colorOption = new("--color", "-c")
            {
                Description = "テキストの色",
                DefaultValueFactory = parseResult => ConsoleColor.White
            };
            colorOption.CompletionSources.Clear();
            colorOption.CompletionSources.Add("White", "Red", "Blue");

            Command messageCommand = new("message", "メッセージを指定回数表示します")
            {
                messageOption,
                repeatOption,
                colorOption
            };

            messageCommand.SetAction(parseResult =>
            {
                string message = parseResult.GetValue(messageOption);
                int repeat = parseResult.GetValue(repeatOption);
                ConsoleColor color = parseResult.GetValue(colorOption);

                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color;

                for (int i = 0; i < repeat; i++)
                {
                    Console.WriteLine($"{i + 1}: {message}");
                }

                Console.ForegroundColor = originalColor;
                return 0;
            });

            return messageCommand;
        }

        // 2. ファイル操作コマンド（サブサブコマンドの例）
        static Command CreateFileCommand()
        {
            Command fileCommand = new("file", "ファイル操作コマンド");

            // サブコマンド: file read
            var readCommand = CreateFileReadCommand();
            fileCommand.Subcommands.Add(readCommand);

            // サブコマンド: file write
            var writeCommand = CreateFileWriteCommand();
            fileCommand.Subcommands.Add(writeCommand);

            // サブサブコマンド: file text操作
            var textCommand = CreateFileTextCommand();
            fileCommand.Subcommands.Add(textCommand);

            return fileCommand;
        }

        static Command CreateFileReadCommand()
        {
            Option<FileInfo> fileOption = new("--file", "-f")
            {
                Description = "読み込むファイル",
                Required = true
            };

            Option<int> linesOption = new("--lines", "-n")
            {
                Description = "表示する行数（0は全行）",
                DefaultValueFactory = parseResult => 0
            };

            Command readCommand = new("read", "ファイルの内容を読み込む")
            {
                fileOption,
                linesOption
            };

            readCommand.SetAction(parseResult =>
            {
                FileInfo file = parseResult.GetValue(fileOption);
                int lines = parseResult.GetValue(linesOption);

                if (!file.Exists)
                {
                    Console.Error.WriteLine($"エラー: ファイルが見つかりません: {file.FullName}");
                    return 1;
                }

                var fileLines = File.ReadAllLines(file.FullName);
                var linesToShow = lines > 0 ? fileLines.Take(lines) : fileLines;

                foreach (var line in linesToShow)
                {
                    Console.WriteLine(line);
                }

                return 0;
            });

            return readCommand;
        }

        static Command CreateFileWriteCommand()
        {
            Option<FileInfo> fileOption = new("--file", "-f")
            {
                Description = "書き込むファイル",
                Required = true
            };

            Option<string[]> contentOption = new("--content", "-c")
            {
                Description = "書き込む内容（複数指定可能）",
                Required = true,
                AllowMultipleArgumentsPerToken = true
            };

            Option<bool> appendOption = new("--append", "-a")
            {
                Description = "追記モード"
            };

            Command writeCommand = new("write", "ファイルに書き込む")
            {
                fileOption,
                contentOption,
                appendOption
            };

            writeCommand.SetAction(parseResult =>
            {
                FileInfo file = parseResult.GetValue(fileOption);
                string[] content = parseResult.GetValue(contentOption);
                bool append = parseResult.GetValue(appendOption);

                if (append)
                {
                    File.AppendAllLines(file.FullName, content);
                    Console.WriteLine($"ファイルに追記しました: {file.FullName}");
                }
                else
                {
                    File.WriteAllLines(file.FullName, content);
                    Console.WriteLine($"ファイルに書き込みました: {file.FullName}");
                }

                return 0;
            });

            return writeCommand;
        }

        // サブサブコマンドの例
        static Command CreateFileTextCommand()
        {
            Command textCommand = new("text", "テキスト操作");

            // file text upper
            var upperCommand = CreateFileTextUpperCommand();
            textCommand.Subcommands.Add(upperCommand);

            // file text lower
            var lowerCommand = CreateFileTextLowerCommand();
            textCommand.Subcommands.Add(lowerCommand);

            return textCommand;
        }

        static Command CreateFileTextUpperCommand()
        {
            Option<FileInfo> fileOption = new("--file", "-f")
            {
                Description = "変換するファイル",
                Required = true
            };

            Command upperCommand = new("upper", "ファイルの内容を大文字に変換")
            {
                fileOption
            };

            upperCommand.SetAction(parseResult =>
            {
                FileInfo file = parseResult.GetValue(fileOption);

                if (!file.Exists)
                {
                    Console.Error.WriteLine($"エラー: ファイルが見つかりません: {file.FullName}");
                    return 1;
                }

                var content = File.ReadAllText(file.FullName);
                var upper = content.ToUpper();
                File.WriteAllText(file.FullName, upper);
                Console.WriteLine($"ファイルを大文字に変換しました: {file.FullName}");

                return 0;
            });

            return upperCommand;
        }

        static Command CreateFileTextLowerCommand()
        {
            Option<FileInfo> fileOption = new("--file", "-f")
            {
                Description = "変換するファイル",
                Required = true
            };

            Command lowerCommand = new("lower", "ファイルの内容を小文字に変換")
            {
                fileOption
            };

            lowerCommand.SetAction(parseResult =>
            {
                FileInfo file = parseResult.GetValue(fileOption);

                if (!file.Exists)
                {
                    Console.Error.WriteLine($"エラー: ファイルが見つかりません: {file.FullName}");
                    return 1;
                }

                var content = File.ReadAllText(file.FullName);
                var lower = content.ToLower();
                File.WriteAllText(file.FullName, lower);
                Console.WriteLine($"ファイルを小文字に変換しました: {file.FullName}");

                return 0;
            });

            return lowerCommand;
        }

        // 3. 各種オプションタイプのデモ
        static Command CreateOptionsCommand()
        {
            // string型
            Option<string> stringOption = new("--string", "-s")
            {
                Description = "文字列オプション",
                DefaultValueFactory = parseResult => "デフォルト値"
            };

            // int型
            Option<int> intOption = new("--int", "-i")
            {
                Description = "整数オプション",
                DefaultValueFactory = parseResult => 42
            };

            // bool型
            Option<bool> boolOption = new("--bool", "-b")
            {
                Description = "真偽値オプション"
            };

            // double型
            Option<double> doubleOption = new("--double", "-d")
            {
                Description = "浮動小数点数オプション",
                DefaultValueFactory = parseResult => 3.14
            };

            // enum型
            Option<DayOfWeek> enumOption = new("--day", "-e")
            {
                Description = "曜日オプション（列挙型）",
                DefaultValueFactory = parseResult => DayOfWeek.Monday
            };

            // 配列型
            Option<string[]> arrayOption = new("--items", "-a")
            {
                Description = "複数の文字列（配列）",
                AllowMultipleArgumentsPerToken = true
            };

            // DirectoryInfo型
            Option<DirectoryInfo> dirOption = new("--dir")
            {
                Description = "ディレクトリパス"
            };

            Command optionsCommand = new("options", "各種オプションタイプのデモ")
            {
                stringOption,
                intOption,
                boolOption,
                doubleOption,
                enumOption,
                arrayOption,
                dirOption
            };

            optionsCommand.SetAction(parseResult =>
            {
                Console.WriteLine("=== オプション値 ===");
                Console.WriteLine($"String: {parseResult.GetValue(stringOption)}");
                Console.WriteLine($"Int: {parseResult.GetValue(intOption)}");
                Console.WriteLine($"Bool: {parseResult.GetValue(boolOption)}");
                Console.WriteLine($"Double: {parseResult.GetValue(doubleOption)}");
                Console.WriteLine($"Enum (DayOfWeek): {parseResult.GetValue(enumOption)}");

                var items = parseResult.GetValue(arrayOption);
                if (items != null && items.Length > 0)
                {
                    Console.WriteLine($"Array: [{string.Join(", ", items)}]");
                }
                else
                {
                    Console.WriteLine("Array: (未指定)");
                }

                var dir = parseResult.GetValue(dirOption);
                if (dir != null)
                {
                    Console.WriteLine($"Directory: {dir.FullName} (存在: {dir.Exists})");
                }
                else
                {
                    Console.WriteLine("Directory: (未指定)");
                }

                return 0;
            });

            return optionsCommand;
        }

        // 4. 引数を使用するコマンド
        static Command CreateCalculatorCommand()
        {
            Argument<int> firstArgument = new("first")
            {
                Description = "最初の数値"
            };

            Argument<int> secondArgument = new("second")
            {
                Description = "2番目の数値"
            };

            Option<string> operationOption = new("--operation", "-o")
            {
                Description = "演算（add, sub, mul, div）",
                DefaultValueFactory = parseResult => "add"
            };

            Command calcCommand = new("calc", "2つの数値を計算する")
            {
                operationOption
            };
            calcCommand.Arguments.Add(firstArgument);
            calcCommand.Arguments.Add(secondArgument);

            calcCommand.SetAction(parseResult =>
            {
                int first = parseResult.GetValue(firstArgument);
                int second = parseResult.GetValue(secondArgument);
                string operation = parseResult.GetValue(operationOption);

                double result = operation.ToLower() switch
                {
                    "add" => first + second,
                    "sub" => first - second,
                    "mul" => first * second,
                    "div" => second != 0 ? (double)first / second : double.NaN,
                    _ => double.NaN
                };

                if (double.IsNaN(result))
                {
                    Console.Error.WriteLine("エラー: 無効な演算またはゼロ除算");
                    return 1;
                }

                Console.WriteLine($"{first} {operation} {second} = {result}");
                return 0;
            });

            return calcCommand;
        }

        // 5. カスタム検証を使用するコマンド
        static Command CreateValidationCommand()
        {
            Option<int> ageOption = new("--age", "-a")
            {
                Description = "年齢（0-150の範囲）",
                Required = true,
                DefaultValueFactory = result =>
                {
                    if (result.Tokens.Count == 0)
                    {
                        return 0;
                    }

                    string value = result.Tokens.Single().Value;
                    if (!int.TryParse(value, out int age))
                    {
                        result.AddError("年齢は整数で指定してください");
                        return 0;
                    }

                    if (age < 0 || age > 150)
                    {
                        result.AddError("年齢は0から150の範囲で指定してください");
                        return 0;
                    }

                    return age;
                }
            };

            Option<string> emailOption = new("--email", "-e")
            {
                Description = "メールアドレス",
                Required = true,
                DefaultValueFactory = result =>
                {
                    if (result.Tokens.Count == 0)
                    {
                        return string.Empty;
                    }

                    string email = result.Tokens.Single().Value;
                    if (!email.Contains("@"))
                    {
                        result.AddError("有効なメールアドレスを指定してください");
                        return string.Empty;
                    }

                    return email;
                }
            };

            Command validateCommand = new("validate", "カスタム検証のデモ")
            {
                ageOption,
                emailOption
            };

            validateCommand.SetAction(parseResult =>
            {
                int age = parseResult.GetValue(ageOption);
                string email = parseResult.GetValue(emailOption);

                Console.WriteLine("=== 検証成功 ===");
                Console.WriteLine($"年齢: {age}");
                Console.WriteLine($"メール: {email}");

                return 0;
            });

            return validateCommand;
        }
    }
}
