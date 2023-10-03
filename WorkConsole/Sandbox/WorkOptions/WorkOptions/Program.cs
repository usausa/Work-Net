namespace WorkOptions
{
    using System.CommandLine;
    using System.CommandLine.Builder;
    using System.CommandLine.Invocation;

    using System.Threading.Tasks;

    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var parser = new CommandLineBuilder(new Command("work-options", handler: CommandHandler.Create(typeof(Program).GetMethod(nameof(Run)))))
                .UseVersionOption()
                .UseHelp()
                .UseParseDirective()
                .UseDebugDirective()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .AddOption(new Option(new[] { "-t", "--text" }, "string option", new Argument<string>(() => null)))
                .AddOption(new Option(new[] { "--check" }, "bool option", new Argument<bool>()))
                .Build();

            return await parser.InvokeAsync(args).ConfigureAwait(false);
        }

        public static async Task<int> Run(string text, bool check, IConsole console = null)
        {
            console?.Out.WriteLine($"{text}");
            console?.Out.WriteLine($"{check}");

            return await Task.FromResult(0);
        }
    }
}
