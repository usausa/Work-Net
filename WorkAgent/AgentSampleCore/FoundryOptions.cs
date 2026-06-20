namespace AgentSampleCore;

/// <summary>
/// Microsoft Foundry(<c>services.ai.azure.com</c>)への接続設定。
/// appsettings.json の "Foundry" セクションや、ユーザーシークレット / 環境変数 にバインドされる。
/// </summary>
public sealed class FoundryOptions
{
    /// <summary>設定をバインドするセクション名。</summary>
    public const string SectionName = "Foundry";

    /// <summary>アカウントエンドポイント(例: <c>https://xxx.services.ai.azure.com</c>)。</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>APIキー。ソースには置かず、ユーザーシークレット / 環境変数 で設定する。</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>chat デプロイメント名(例: <c>gpt-5.4-mini</c>)。</summary>
    public string ChatDeployment { get; set; } = string.Empty;
}
