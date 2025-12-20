# プロパティベースAPI - HostApplicationBuilder風の設計

## 概要

`ICliHostBuilder`が`Microsoft.Extensions.Hosting.HostApplicationBuilder`と同様のプロパティベースAPIをサポートするようになりました。

## 新しいプロパティ

```csharp
public interface ICliHostBuilder
{
    // Configuration管理
    ConfigurationManager Configuration { get; }
    
    // ホスト環境情報
    IHostEnvironment Environment { get; }
    
    // サービスコレクション（DIコンテナ）
    IServiceCollection Services { get; }
    
    // Logging設定
    ILoggingBuilder Logging { get; }
    
    // コマンド設定
    ICliHostBuilder ConfigureCommands(Action<ICommandConfigurator> configureCommands);
    
    // ビルド
    ICliHost Build();
}
```

## 使用例

### 基本的な使い方

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkCliHost;

var builder = CliHost.CreateDefaultBuilder(args);

// 1. Configuration - プロパティ経由で直接アクセス
builder.Configuration.AddJsonFile("custom-settings.json", optional: true);
builder.Configuration.AddUserSecrets<Program>();

// 2. Environment - ホスト環境情報へのアクセス
Console.WriteLine($"Application: {builder.Environment.ApplicationName}");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"ContentRoot: {builder.Environment.ContentRootPath}");

// 3. Logging - ログ設定
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);

// 4. Services - DIコンテナへのサービス登録
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddHttpClient<IApiClient, ApiClient>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();

// 5. Commands - コマンド設定
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("My CLI Application");
    });
    
    commands.AddGlobalFilter<TimingFilter>();
    commands.AddCommand<ProcessCommand>();
});

var host = builder.Build();
return await host.RunAsync();
```

## HostApplicationBuilderとの比較

### HostApplicationBuilder

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("custom.json");
builder.Environment.ApplicationName = "MyApp";
builder.Logging.AddDebug();
builder.Services.AddDbContext<AppDbContext>();

var host = builder.Build();
await host.RunAsync();
```

### CliHostBuilder

```csharp
var builder = CliHost.CreateDefaultBuilder(args);

builder.Configuration.AddJsonFile("custom.json");
Console.WriteLine(builder.Environment.ApplicationName);
builder.Logging.AddDebug();
builder.Services.AddDbContext<AppDbContext>();

builder.ConfigureCommands(commands =>
{
    commands.AddCommand<MyCommand>();
});

var host = builder.Build();
return await host.RunAsync();
```

## プロパティの詳細

### Configuration

**型**: `ConfigurationManager`

appsettings.jsonなどの設定ファイルの管理：

```csharp
// JSON設定ファイル
builder.Configuration.AddJsonFile("appsettings.json", optional: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

// 環境変数
builder.Configuration.AddEnvironmentVariables();

// コマンドライン引数
builder.Configuration.AddCommandLine(args);

// ユーザーシークレット
builder.Configuration.AddUserSecrets<Program>();

// 設定値の取得
var connectionString = builder.Configuration["ConnectionStrings:Default"];
var apiKey = builder.Configuration.GetValue<string>("ApiKey");
```

**デフォルトで設定されるプロバイダー**:
1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. 環境変数
4. コマンドライン引数

### Environment

**型**: `IHostEnvironment`

ホスト環境情報へのアクセス：

```csharp
// アプリケーション名
Console.WriteLine(builder.Environment.ApplicationName); // "WorkCliHost"

// 環境名（Development, Staging, Production）
Console.WriteLine(builder.Environment.EnvironmentName); // "Production"

// コンテンツルートパス
Console.WriteLine(builder.Environment.ContentRootPath); // アプリのベースディレクトリ

// ファイルプロバイダー
var fileInfo = builder.Environment.ContentRootFileProvider.GetFileInfo("data.txt");

// 環境チェック
if (builder.Environment.IsDevelopment())
{
    // 開発環境でのみ実行
}
```

**環境名の決定順序**:
1. `DOTNET_ENVIRONMENT`環境変数
2. `ASPNETCORE_ENVIRONMENT`環境変数
3. デフォルト: "Production"

### Services

**型**: `IServiceCollection`

DIコンテナへのサービス登録：

```csharp
// シングルトン
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

// スコープド
builder.Services.AddScoped<IUserService, UserService>();

// トランジエント
builder.Services.AddTransient<IEmailSender, EmailSender>();

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});

// HttpClient
builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiUrl"]!);
});

// オプションパターン
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));
```

### Logging

**型**: `ILoggingBuilder`

ログ設定：

```csharp
// ログプロバイダーの追加
builder.Logging.AddConsole();       // デフォルトで追加済み
builder.Logging.AddDebug();
builder.Logging.AddEventLog();

// 最小ログレベル
builder.Logging.SetMinimumLevel(LogLevel.Information);

// フィルター
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Error);
builder.Logging.AddFilter<ConsoleLoggerProvider>("WorkCliHost", LogLevel.Debug);

// Configuration連携
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

// カスタムログプロバイダー
builder.Logging.AddProvider(new CustomLoggerProvider());
```

**appsettings.jsonでの設定**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Error"
    },
    "Console": {
      "IncludeScopes": true
    }
  }
}
```

## ConfigureServicesの廃止

以前の`ConfigureServices`メソッドは廃止され、`Services`プロパティに置き換わりました。

### Before

```csharp
builder.ConfigureServices(services =>
{
    services.AddDbContext<AppDbContext>();
    services.AddSingleton<IMyService, MyService>();
});
```

### After

```csharp
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddSingleton<IMyService, MyService>();
```

## 実用例

### 完全な設定例

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkCliHost;

var builder = CliHost.CreateDefaultBuilder(args);

// Configuration
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true)
    .AddUserSecrets<Program>();

// Environment
if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("Running in Development mode");
}

// Logging
builder.Logging
    .ClearProviders()
    .AddConsole()
    .AddDebug()
    .SetMinimumLevel(builder.Environment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information);

// Services
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    options.UseSqlServer(connectionString);
});

builder.Services.AddHttpClient<IWeatherService, WeatherService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["WeatherApiUrl"]!);
    client.DefaultRequestHeaders.Add("ApiKey", builder.Configuration["WeatherApiKey"]);
});

builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("App"));

// Commands
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription($"{builder.Environment.ApplicationName} - CLI Tool");
    });
    
    commands.AddGlobalFilter<TimingFilter>(order: -100);
    commands.AddGlobalFilter<LoggingFilter>(order: 0);
    commands.AddGlobalFilter<ExceptionHandlingFilter>(order: int.MaxValue);
    
    commands.AddCommand<WeatherCommand>();
    commands.AddCommand<UserCommand>(user =>
    {
        user.AddSubCommand<UserListCommand>();
        user.AddSubCommand<UserCreateCommand>();
    });
});

var host = builder.Build();
return await host.RunAsync();
```

## まとめ

プロパティベースAPIにより：

- ✅ **ASP.NET Coreとの一貫性** - HostApplicationBuilderと同様のAPI
- ✅ **直感的** - プロパティ経由で直接アクセス
- ✅ **Fluent API** - メソッドチェーンが可能
- ✅ **型安全** - コンパイル時にチェック
- ✅ **発見しやすい** - IntelliSenseで簡単に探索
- ✅ **柔軟** - Configuration、Environment、Services、Loggingを自由に設定可能

これにより、ASP.NET Coreの開発者にとって馴染みやすいAPIとなりました。
