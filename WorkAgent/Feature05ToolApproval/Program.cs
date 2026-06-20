using System.ComponentModel;
using System.Globalization;

using AgentSampleCore;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI.Chat;

// ChatMessage は Microsoft.Extensions.AI と OpenAI.Chat の両方にあるため、前者を明示する。
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

// ===========================================================================
// Feature05ToolApproval : ツールの承認(Human-in-the-Loop)
// ---------------------------------------------------------------------------
// 破壊的・機微なツールは、実行前に人間の承認を挟みたい。
//  ・AIFunction を ApprovalRequiredAIFunction でラップすると「承認が必要なツール」になる。
//  ・モデルがそのツールを呼ぼうとすると、実行されず応答に ToolApprovalRequestContent が入る。
//  ・人間が可否を判断し、request.CreateResponse(approved) で応答を作って再実行する。
//  ・承認されればツールが実行され、却下されればスキップされる。
// (広いポリシーで一括制御したい場合は AIAgentBuilder.UseToolApproval(...) も使える)
// 使う主な API: ApprovalRequiredAIFunction, ToolApprovalRequestContent.CreateResponse(...)
// ===========================================================================

var options = SampleHost.TryGetFoundryOptions();
if (options is null)
{
    return 1;
}

var chatClient = SampleHost.CreateChatClient(options);

// 機微なツールを ApprovalRequiredAIFunction でラップして「要承認」にする。
var sensitiveTool = new ApprovalRequiredAIFunction(
    AIFunctionFactory.Create(MaintenanceTools.CleanTemporaryFiles));

AIAgent agent = chatClient.AsAIAgent(
    instructions:
        "あなたはこのPCのメンテナンス担当アシスタントです。" +
        "依頼に応じてツールを使い、結果を簡潔に日本語で報告してください。",
    name: "PcMaintenanceAssistant",
    tools:
    [
        AIFunctionFactory.Create(PcTools.GetDriveInfo),
        sensitiveTool,
    ]);

// セッションを使う(承認の往復で同じ会話を継続するため)。
var session = await agent.CreateSessionAsync();

const string question = "一時ファイルを削除して空き容量を増やして。";
Console.WriteLine($"You   > {question}");

var response = await agent.RunAsync(question, session);

// 応答から承認要求を集める。
var approvalRequests = response.Messages
    .SelectMany(static m => m.Contents)
    .OfType<ToolApprovalRequestContent>()
    .ToList();

if (approvalRequests.Count == 0)
{
    // 機微ツールが使われず、そのまま回答が返ったケース。
    Console.WriteLine($"Agent > {response}");
    return 0;
}

// 承認応答を組み立てる。
// ここではデモのため自動で承認しているが、実運用ではこの位置で人間に可否を尋ねる。
var approvalContents = new List<AIContent>();
foreach (var request in approvalRequests)
{
    var toolName = (request.ToolCall as FunctionCallContent)?.Name ?? request.ToolCall.CallId;
    Console.WriteLine($"  [承認要求] ツール '{toolName}' の実行許可を求めています。 -> 承認(approved: true)");

    approvalContents.Add(request.CreateResponse(approved: true, reason: "ユーザーが承認しました"));
}

Console.WriteLine();

// 承認応答を同じセッションに渡して再実行 → 承認されたツールが実行され、最終回答が返る。
var resumeMessage = new ChatMessage(ChatRole.User, approvalContents);
var finalResponse = await agent.RunAsync(resumeMessage, session);

Console.WriteLine($"Agent > {finalResponse}");

return 0;

// ---------------------------------------------------------------------------
// 機微な(破壊的とみなす)メンテナンス用ツール。承認デモのため、実際の削除は行わない。
// ---------------------------------------------------------------------------
internal static class MaintenanceTools
{
    [Description("一時フォルダ内の不要ファイルを削除して空き容量を増やします(破壊的なメンテナンス操作)。")]
    public static string CleanTemporaryFiles()
    {
        var tempPath = Path.GetTempPath();
        long count = 0;
        long totalBytes = 0;

        try
        {
            foreach (var file in Directory.EnumerateFiles(tempPath))
            {
                try
                {
                    totalBytes += new FileInfo(file).Length;
                    count++;
                }
                catch (IOException)
                {
                    // 使用中などで取得できないファイルは無視する
                }
                catch (UnauthorizedAccessException)
                {
                    // アクセス権の無いファイルは無視する
                }
            }
        }
        catch (IOException)
        {
            // 一時フォルダ自体を列挙できない場合
        }
        catch (UnauthorizedAccessException)
        {
            // 一時フォルダ自体へアクセスできない場合
        }

        var megaBytes = totalBytes / (1024.0 * 1024);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"一時フォルダ {tempPath} の不要ファイル {count} 個(約 {megaBytes:F1} MB)を削除しました(このサンプルでは安全のため実際には削除していません)。");
    }
}
