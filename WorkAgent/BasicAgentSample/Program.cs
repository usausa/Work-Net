using System.ClientModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;

using Azure.AI.OpenAI;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

using OpenAI.Chat; // AsAIAgent

// ---------------------------------------------------------------------------
// Microsoft.Agents.AI(.OpenAI) 1.10.0 (GA) + Azure.AI.OpenAI 2.1.0
// ---------------------------------------------------------------------------

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var options = new FoundryOptions();
configuration.GetSection(FoundryOptions.SectionName).Bind(options);

if (string.IsNullOrWhiteSpace(options.Endpoint) ||
    string.IsNullOrWhiteSpace(options.ApiKey) ||
    string.IsNullOrWhiteSpace(options.ChatDeployment))
{
    await Console.Error.WriteLineAsync(
        "接続情報が不足しています。appsettings.json の Foundry セクション、" +
        "またはユーザーシークレット / 環境変数 (Foundry__ApiKey 等) で " +
        "Endpoint / ApiKey / ChatDeployment を設定してください。");
    return 1;
}

// エージェントを生成 (Foundry の chat デプロイメント + PC情報取得ツール3つ)
// AzureOpenAIClient -> GetChatClient(デプロイメント名) -> AsAIAgent の流れ。
AIAgent agent = new AzureOpenAIClient(new Uri(options.Endpoint), new ApiKeyCredential(options.ApiKey))
    .GetChatClient(options.ChatDeployment)
    .AsAIAgent(
        instructions: "あなたはこのPCの状態を調べるアシスタントです。" +
                      "PC・OS・メモリ・ディスクに関する質問には必ず適切なツールを使って実際の値を取得し、" +
                      "結果を簡潔に日本語でまとめて答えてください。",
        name: "PcInfoAssistant",
        tools:
        [
            AIFunctionFactory.Create(PcTools.GetSystemInfo),
            AIFunctionFactory.Create(PcTools.GetMemoryInfo),
            AIFunctionFactory.Create(PcTools.GetDriveInfo),
        ]);

// 単発の実行 (RunAsync は AgentResponse を返す。ToString() で本文を取得)
Console.WriteLine($"=== 単発の実行 (Foundry / {options.ChatDeployment}) ===");
var response = await agent.RunAsync("このPCの基本スペックと空きディスク容量を教えて。");
Console.WriteLine(response);
Console.WriteLine();

// 4. マルチターン対話 (AgentSession が履歴を保持。応答はストリーミング表示)
Console.WriteLine("=== 対話モード ('exit' で終了) ===");
var session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("You   > ");
    var input = Console.ReadLine();
    if (input is null)
    {
        break; // 標準入力が閉じた (EOF / Ctrl+Z)
    }

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    Console.Write("Agent > ");
    await foreach (var update in agent.RunStreamingAsync(input, session))
    {
        Console.Write(update); // 各チャンクの ToString() がテキスト断片
    }

    Console.WriteLine();
}

return 0;

// ---------------------------------------------------------------------------
// 接続設定 (appsettings.json / ユーザーシークレット の "Foundry" セクションにバインド)
// ---------------------------------------------------------------------------
internal sealed class FoundryOptions
{
    public const string SectionName = "Foundry";

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ChatDeployment { get; set; } = string.Empty;
}

// ---------------------------------------------------------------------------
// ツール定義:
//   通常の C# メソッドに [Description] を付けるだけでツールになる。
//   ツールの呼び出し・引数の解釈・結果の取り込みはフレームワークが自動で行う。
//   ここでは BCL のみで取得できる、クロスプラットフォームなPC情報を返す。
// ---------------------------------------------------------------------------
internal static class PcTools
{
    [Description("OS・マシン名・ユーザー名・CPUコア数・.NETバージョン・稼働時間など、このPCの基本情報を取得します。")]
    public static string GetSystemInfo()
        => string.Join(
            Environment.NewLine,
            [
                $"OS              : {RuntimeInformation.OSDescription}",
                $"アーキテクチャ  : {RuntimeInformation.OSArchitecture}",
                $"マシン名        : {Environment.MachineName}",
                $"ユーザー名      : {Environment.UserName}",
                $"論理プロセッサ数: {Environment.ProcessorCount}",
                $".NET ランタイム : {RuntimeInformation.FrameworkDescription}",
                $"稼働時間        : {TimeSpan.FromMilliseconds(Environment.TickCount64):d\\.hh\\:mm\\:ss}",
            ]);

    [Description("このPCのメモリ(RAM)の総量とプロセスの使用量を取得します。")]
    public static string GetMemoryInfo()
    {
        var gc = GC.GetGCMemoryInfo();
        var totalGb = gc.TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024);
        var usedMb = GC.GetTotalMemory(forceFullCollection: false) / (1024.0 * 1024);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"利用可能メモリ(総量): {totalGb:F1} GB / このプロセスのGCヒープ使用量: {usedMb:F1} MB");
    }

    [Description("このPCのドライブごとの空き容量・総容量を取得します。")]
    public static string GetDriveInfo()
    {
        var lines = new List<string>();
        foreach (var d in DriveInfo.GetDrives())
        {
            if (!d.IsReady)
            {
                continue;
            }

            var freeGb = d.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            var totalGb = d.TotalSize / (1024.0 * 1024 * 1024);
            lines.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{d.Name} [{d.DriveType}] 空き {freeGb:F1} GB / 全体 {totalGb:F1} GB"));
        }

        return lines.Count > 0
            ? string.Join(Environment.NewLine, lines)
            : "準備済みのドライブが見つかりませんでした。";
    }
}
