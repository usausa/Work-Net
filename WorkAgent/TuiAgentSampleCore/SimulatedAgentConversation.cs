namespace TuiAgentSampleCore;

using System.Runtime.CompilerServices;
using System.Text;

/// <summary>
/// API キー不要で動く擬似エージェント。思考 → ツール呼び出し → 本文ストリームを
/// 「最近のエージェント風」のイベント列として返す。
/// </summary>
/// <remarks>
/// 実エージェントへ差し替える場合は <see cref="IAgentConversation"/> を実装した別クラス
/// (例: Microsoft.Extensions.AI の <c>IChatClient</c> をラップ) を用意し、
/// 各サンプルの生成箇所をそれに置き換えるだけでよい。
/// </remarks>
public sealed class SimulatedAgentConversation : IAgentConversation
{
    private readonly List<ChatMessage> history = [];
    private readonly SimulationOptions options;

    public SimulatedAgentConversation(SimulationOptions? simulationOptions = null)
    {
        options = simulationOptions ?? new();
    }

    public string AgentName => options.AgentName;

    public string ModelName => options.ModelName;

    public IReadOnlyList<ChatMessage> History => history;

    public async IAsyncEnumerable<AgentEvent> SendAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        history.Add(new ChatMessage(ChatRole.User, userMessage));
        var scenario = ScenarioSelector.Select(userMessage);

        // 思考フェーズ
        yield return new ThinkingStarted();
        foreach (var thought in scenario.Thoughts)
        {
            await Task.Delay(options.ThinkingDelayMilliseconds, cancellationToken).ConfigureAwait(false);
            yield return new ThinkingDelta(thought);
        }

        yield return new ThinkingCompleted();

        // ツール呼び出しフェーズ
        foreach (var tool in scenario.ToolCalls)
        {
            yield return new ToolCallStarted(tool.Name, tool.Arguments);
            await Task.Delay(options.ToolCallDelayMilliseconds, cancellationToken).ConfigureAwait(false);
            var result = tool.Execute();
            history.Add(new ChatMessage(ChatRole.Tool, result) { ToolName = tool.Name });
            yield return new ToolCallCompleted(tool.Name, result);
        }

        // 本文ストリーミングフェーズ
        var builder = new StringBuilder();
        foreach (var token in ResponseTokenizer.Tokenize(scenario.Answer))
        {
            await Task.Delay(options.TokenDelayMilliseconds, cancellationToken).ConfigureAwait(false);
            builder.Append(token);
            yield return new TextDelta(token);
        }

        history.Add(new ChatMessage(ChatRole.Assistant, builder.ToString()));
        yield return new ResponseCompleted();
    }
}
