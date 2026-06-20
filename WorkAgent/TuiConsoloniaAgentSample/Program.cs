using Avalonia;

using Consolonia;

using TuiConsoloniaAgentSample;

// ---------------------------------------------------------------------------
// Consolonia 11.3.12.6 (Avalonia 12 をコンソールへ) によるエージェント TUI
//   - XAML レイアウト + データバインディング + MVVM で宣言的にチャット UI を構築。
//   - 配色 (役割ごとのブラシ) は ViewModel から束縛し、項目種別で色を変える。
// ---------------------------------------------------------------------------
BuildAvaloniaApp().StartWithConsoleLifetime(args);
return 0;

static AppBuilder BuildAvaloniaApp() =>
    AppBuilder.Configure<App>()
        .UseConsolonia()
        .UseAutoDetectedConsole()
        .LogToException();
