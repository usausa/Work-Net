// https://github.com/spectreconsole/spectre.console/blob/main/README.jp.md
using Spectre.Console;
using Spectre.Console.Examples;

AnsiConsole.MarkupLine("[bold yellow]Hello[/] [red]World![/]");

AnsiConsole.Markup("[red]Foo[/] ");
AnsiConsole.Markup("[#ff0000]Bar[/] ");
AnsiConsole.Markup("[rgb(255,0,0)]Baz[/] ");
AnsiConsole.WriteLine();

AnsiConsole.MarkupLine("Hello :information:");

var table = new Table();
table.AddColumn(new TableColumn(new Markup("[yellow]Foo[/]")));
table.AddColumn(new TableColumn("[blue]Bar[/]"));
AnsiConsole.Write(table);

AnsiConsole.ResetColors();
AnsiConsole.WriteLine();
AnsiConsole.Write(new Rule("[yellow bold underline]24-bit Colors[/]").RuleStyle("grey").LeftJustified());
AnsiConsole.WriteLine();

AnsiConsole.Write(
    new Panel("[yellow]Hello :globe_showing_europe_africa:![/]")
        .RoundedBorder());

AnsiConsole.ResetDecoration();
var decorations = System.Enum.GetValues(typeof(Decoration));
foreach (var decoration in decorations)
{
    var name = System.Enum.GetName(typeof(Decoration), decoration);
    AnsiConsole.Write(name + ": ");
    AnsiConsole.Write(new Markup(name + "\n", new Style(decoration: (Decoration)decoration)));
}

AnsiConsole.Write(new BarChart()
    .AddItem("Apple", 32, Color.Green)
    .AddItem("Oranges", 13, Color.Orange1)
    .AddItem("Bananas", 22, Color.Yellow));

AnsiConsole.Write(new ColorBox(width: 80, height: 15));

var integer = AnsiConsole.Ask<int>("Enter an integer: ");
AnsiConsole.WriteLine(integer);

Console.ReadLine();
