namespace TuiAgentSampleCore;

using System.Globalization;

/// <summary>1 回のツール (関数) 呼び出しの定義。</summary>
internal sealed record ToolInvocation(string Name, string Arguments, Func<string> Execute);

/// <summary>1 回の応答シナリオ (思考・ツール・本文)。</summary>
internal sealed record Scenario(
    IReadOnlyList<string> Thoughts,
    IReadOnlyList<ToolInvocation> ToolCalls,
    string Answer);

/// <summary>
/// 利用者入力から応答シナリオを決定する。乱数は使わず、入力内容に応じて決定的に選ぶ。
/// </summary>
internal static class ScenarioSelector
{
    private static readonly string[] SystemKeywords =
    [
        "pc", "spec", "スペック", "システム", "system", "環境",
        "メモリ", "memory", "cpu", "ディスク", "disk", "マシン", "os"
    ];

    public static Scenario Select(string userMessage)
    {
        foreach (var keyword in SystemKeywords)
        {
            if (userMessage.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return BuildSystemScenario();
            }
        }

        return BuildGenericScenario(userMessage);
    }

    private static Scenario BuildSystemScenario()
    {
        string[] thoughts =
        [
            "意図を解釈: この PC / 実行環境に関する質問。",
            "方針: get_system_info ツールで実際の値を取得する。",
            "取得結果を簡潔な日本語サマリにまとめる。"
        ];

        var tool = new ToolInvocation("get_system_info", "{ \"scope\": \"summary\" }", SystemProbe.SystemInfo);

        var nl = Environment.NewLine;
        var answer =
            "## 環境サマリ" + nl + nl +
            "`" + SystemProbe.MachineName + "` を確認しました。主な構成は次のとおりです。" + nl + nl +
            "- **CPU 論理コア数**: " + SystemProbe.ProcessorCount.ToString(CultureInfo.InvariantCulture) + nl +
            "- **物理メモリ (目安)**: " + SystemProbe.TotalMemoryGigabytes.ToString("F1", CultureInfo.InvariantCulture) + " GB" + nl +
            "- **マシン名**: `" + SystemProbe.MachineName + "`" + nl + nl +
            "> 生の出力は `get_system_info` ツールの結果を参照してください。";

        return new Scenario(thoughts, [tool], answer);
    }

    private static Scenario BuildGenericScenario(string question)
    {
        string[] thoughts =
        [
            "質問の主旨を把握中...",
            "回答に必要な観点を整理。",
            "Markdown で簡潔に構成する。"
        ];

        var nl = Environment.NewLine;
        var index = (question.Length % 2 == 0) ? 0 : 1;
        var answer = index == 0
            ? "## 「" + question + "」について" + nl + nl + GenericBodyWithCode
            : "## 「" + question + "」ですね" + nl + nl + GenericBodyChoosing;

        return new Scenario(thoughts, [], answer);
    }

    private const string GenericBodyWithCode = """
ポイントは次のとおりです。

- **基本構造**: ターミナル UI は「入力 -> イベント購読 -> 逐次描画」が土台です
- **ストリーミング**: 応答は `IAsyncEnumerable<AgentEvent>` を `await foreach` で受けると素直です
- **状態分離**: 思考中・ツール実行・本文ストリームを分けると綺麗にまとまります

最小の擬似コード:

```csharp
await foreach (var ev in agent.SendAsync(input))
{
    Render(ev); // ThinkingStarted / TextDelta / ToolCallStarted ...
}
```

続けて知りたい点があれば、遠慮なくどうぞ。
""";

    private const string GenericBodyChoosing = """
結論から言うと、目的に応じて選ぶのが近道です。

- **すぐ綺麗な出力**: 整形テキストやテーブル中心なら軽量ライブラリ (Spectre.Console など)
- **本格的な画面**: 入力欄・スクロール・複数ペインが要るならフルスクリーン TUI (Terminal.Gui など)
- **既存資産の活用**: XAML/MVVM の知見があるなら宣言的フレームワーク (Consolonia など)

`実際に動かして比べる` のが最終的な判断材料になります。続けてどうぞ。
""";
}
