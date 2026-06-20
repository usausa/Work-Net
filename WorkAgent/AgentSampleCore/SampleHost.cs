namespace AgentSampleCore;

using System.ClientModel;
using System.Reflection;

using Azure.AI.OpenAI;

using Microsoft.Extensions.Configuration;

using OpenAI.Chat;

/// <summary>
/// 各サンプル共通の初期化処理(接続設定の読み込みとクライアント生成)をまとめたヘルパー。
/// 機能ごとのサンプルは「エージェントの組み立て」に集中できるよう、
/// ここに定型のボイラープレートを集約している。
/// </summary>
public static class SampleHost
{
    /// <summary>
    /// <c>appsettings.json</c> → ユーザーシークレット → 環境変数 の順で接続設定を読み込む。
    /// 設定が不足している場合は標準エラーへ案内を出力し、<c>null</c> を返す。
    /// </summary>
    /// <returns>読み込めた場合は <see cref="FoundryOptions"/>、不足している場合は <c>null</c>。</returns>
    public static FoundryOptions? TryGetFoundryOptions()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

        // 実行中の exe(エントリアセンブリ)に付与された UserSecretsId を使う。
        // 全サンプルで同じ UserSecretsId を共有しているため、シークレットの設定は一度で済む。
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is not null)
        {
            builder.AddUserSecrets(entryAssembly, optional: true);
        }

        var configuration = builder
            .AddEnvironmentVariables()
            .Build();

        var options = new FoundryOptions();
        configuration.GetSection(FoundryOptions.SectionName).Bind(options);

        if (string.IsNullOrWhiteSpace(options.Endpoint) ||
            string.IsNullOrWhiteSpace(options.ApiKey) ||
            string.IsNullOrWhiteSpace(options.ChatDeployment))
        {
            Console.Error.WriteLine(
                "接続情報が不足しています。appsettings.json の Foundry セクション、" +
                "またはユーザーシークレット / 環境変数 (Foundry__ApiKey 等) で " +
                "Endpoint / ApiKey / ChatDeployment を設定してください。");
            return null;
        }

        return options;
    }

    /// <summary>
    /// Foundry の chat デプロイメントに接続する <see cref="ChatClient"/> を生成する。
    /// この <see cref="ChatClient"/> に対して <c>AsAIAgent(...)</c> を呼ぶとエージェントになる。
    /// </summary>
    /// <param name="options">接続設定。</param>
    /// <returns>chat デプロイメントへ接続済みの <see cref="ChatClient"/>。</returns>
    public static ChatClient CreateChatClient(FoundryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // AzureOpenAIClient -> GetChatClient(デプロイメント名) の流れ。
        return new AzureOpenAIClient(new Uri(options.Endpoint), new ApiKeyCredential(options.ApiKey))
            .GetChatClient(options.ChatDeployment);
    }
}
