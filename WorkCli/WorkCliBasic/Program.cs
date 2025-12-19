using System.CommandLine;

Option<string> messageOption = new("--message")
{
    Description = "表示するメッセージ",
    Required = true
};

Option<int> repeatOption = new("--repeat")
{
    Description = "繰り返し回数",
    DefaultValueFactory = _ => 1
};

Command messageCommand = new("message", "メッセージを指定回数表示します")
{
    messageOption,
    repeatOption
};

RootCommand rootCommand = new("WorkCliBasic - メッセージ表示アプリケーション");
rootCommand.Subcommands.Add(messageCommand);

messageCommand.SetAction(parseResult =>
{
    var message = parseResult.GetValue(messageOption);
    var repeat = parseResult.GetValue(repeatOption);

    for (var i = 0; i < repeat; i++)
    {
        Console.WriteLine(message);
    }

    return 0;
});

return rootCommand.Parse(args).Invoke();
