namespace TuiAgentSampleCore;

/// <summary>
/// 各 TUI で共通利用する起動ロゴ・タイトル・ヒント文言。
/// 「最近の AI エージェント CLI」風のスプラッシュ表示に使う。
/// </summary>
public static class AgentBranding
{
    /// <summary>ASCII アートのロゴ (ANSI Shadow 風 / 6 行)。</summary>
    public static IReadOnlyList<string> LogoLines { get; } =
    [
        " █████╗  ██████╗ ███████╗███╗   ██╗████████╗",
        "██╔══██╗██╔════╝ ██╔════╝████╗  ██║╚══██╔══╝",
        "███████║██║  ███╗█████╗  ██╔██╗ ██║   ██║   ",
        "██╔══██║██║   ██║██╔══╝  ██║╚██╗██║   ██║   ",
        "██║  ██║╚██████╔╝███████╗██║ ╚████║   ██║   ",
        "╚═╝  ╚═╝ ╚═════╝ ╚══════╝╚═╝  ╚═══╝   ╚═╝   "
    ];

    /// <summary>製品名タイトル。CJK 端末での桁ずれを避けるため記号は ASCII に統一。</summary>
    public static string Title => "AI Agent - Terminal UI";

    /// <summary>サブタイトル (動作モード)。</summary>
    public static string Tagline => "simulated backend - no API key required";

    /// <summary>起動時に表示する操作ヒント。</summary>
    public static IReadOnlyList<string> Tips { get; } =
    [
        "「PC のスペックを教えて」などと送るとツール呼び出しを再現します",
        "/clear で画面消去、/exit または exit で終了します"
    ];
}
