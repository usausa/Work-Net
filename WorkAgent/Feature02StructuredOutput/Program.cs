using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using AgentSampleCore;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI.Chat;

// ===========================================================================
// Feature02StructuredOutput : 構造化出力(Structured Output)
// ---------------------------------------------------------------------------
// モデルの回答を「文字列」ではなく「型付きの C# オブジェクト」として受け取る。
//  ・RunAsync<T>(...) を使うと、フレームワークが T の JSON スキーマを response_format
//    としてモデルに渡し、返ってきた JSON を T にデシリアライズして返す。
//  ・結果は AgentResponse<T>.Result から強く型付けされた値として取り出せる。
//  ・ツールで実際の PC情報を集めさせ、それを指定スキーマへ整形させる。
// 使う主な API: AIAgent.RunAsync<T>(...), AgentResponse<T>.Result
// ===========================================================================

var options = SampleHost.TryGetFoundryOptions();
if (options is null)
{
    return 1;
}

var chatClient = SampleHost.CreateChatClient(options);

AIAgent agent = chatClient.AsAIAgent(
    instructions:
        "あなたはPC情報を収集してレポートを作るアシスタントです。" +
        "必ずツールを使って実際の値を取得し、要求されたスキーマに従って回答してください。",
    name: "PcReportAssistant",
    tools:
    [
        AIFunctionFactory.Create(PcTools.GetSystemInfo),
        AIFunctionFactory.Create(PcTools.GetDriveInfo),
    ]);

// RunAsync<SystemReport> で型付きの結果を受け取る。
// フレームワークが SystemReport のスキーマをモデルに渡し、JSON を自動でデシリアライズする。
// STJ のソース生成(末尾の PcReportJsonContext)を使うと、リフレクション不要で AOT にも適する。
var jsonOptions = new JsonSerializerOptions { TypeInfoResolver = PcReportJsonContext.Default };

var response = await agent.RunAsync<SystemReport>(
    "このPCのシステム情報と、各ドライブの容量をまとめてください。",
    serializerOptions: jsonOptions);

var report = response.Result;

// 文字列ではなく型付きオブジェクトなので、各フィールドへ直接アクセスできる。
Console.WriteLine("=== 型付きで取得した PC レポート ===");
Console.WriteLine($"OS              : {report.OperatingSystem}");
Console.WriteLine($"アーキテクチャ  : {report.Architecture}");
Console.WriteLine($"マシン名        : {report.MachineName}");
Console.WriteLine($"論理プロセッサ数: {report.LogicalProcessors}");
Console.WriteLine($".NET            : {report.DotNetVersion}");
Console.WriteLine("ドライブ:");
foreach (var drive in report.Drives)
{
    Console.WriteLine(string.Create(
        CultureInfo.InvariantCulture,
        $"  {drive.Name} : 空き {drive.FreeGb:F1} GB / 全体 {drive.TotalGb:F1} GB"));
}

return 0;

// ---------------------------------------------------------------------------
// 構造化出力で受け取る型。モデルはこの形に合わせて JSON を生成する。
// プロパティの説明([Description])はスキーマに含まれ、モデルへのヒントになる。
// ---------------------------------------------------------------------------
internal sealed record SystemReport(
    [property: System.ComponentModel.Description("OS の名称とバージョン")] string OperatingSystem,
    [property: System.ComponentModel.Description("CPU アーキテクチャ")] string Architecture,
    string MachineName,
    [property: System.ComponentModel.Description("論理プロセッサ数")] int LogicalProcessors,
    [property: System.ComponentModel.Description(".NET ランタイムのバージョン")] string DotNetVersion,
    IReadOnlyList<DriveReport> Drives);

internal sealed record DriveReport(
    string Name,
    [property: System.ComponentModel.Description("空き容量(GB)")] double FreeGb,
    [property: System.ComponentModel.Description("総容量(GB)")] double TotalGb);

// System.Text.Json のソース生成コンテキスト。
// これを serializerOptions に渡すことで、構造化出力の生成/解析がリフレクション不要になる。
[JsonSerializable(typeof(SystemReport))]
internal sealed partial class PcReportJsonContext : JsonSerializerContext;
