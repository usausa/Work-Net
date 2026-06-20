namespace TuiTerminalGuiAgentSample;

using Terminal.Gui.Drawing;

/// <summary>
/// 表示項目の種別・重要度に応じた配色定義。各 View に <c>SetScheme</c> で適用する。
/// </summary>
internal static class ChatTheme
{
    public static Scheme Window { get; } = Make(Color.White, Color.Black);

    public static Scheme Logo { get; } = Make(Color.BrightCyan, Color.Black);

    public static Scheme Title { get; } = Make(Color.White, Color.Black);

    public static Scheme User { get; } = Make(Color.BrightGreen, Color.Black);

    public static Scheme Assistant { get; } = Make(Color.BrightCyan, Color.Black);

    // 応答の文章は白。見出し (役割) のみアクセント色にする。
    public static Scheme AssistantBody { get; } = Make(Color.White, Color.Black);

    public static Scheme ToolHeader { get; } = Make(Color.BrightBlue, Color.Black);

    public static Scheme ToolResult { get; } = Make(Color.Gray, Color.Black);

    public static Scheme Thinking { get; } = Make(Color.BrightYellow, Color.Black);

    public static Scheme Ready { get; } = Make(Color.Gray, Color.Black);

    private static Scheme Make(Color foreground, Color background) =>
        new(new Attribute(foreground, background));
}
