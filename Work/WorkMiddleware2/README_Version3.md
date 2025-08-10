# 二層 (Scoped / Singleton) 切替可能な DI パイプライン + ベンチマーク

本リポジトリは ASP.NET Core の Middleware パターンに類似した汎用パイプラインを
1. 実行毎スコープ (ScopedPerExecution)
2. 事前一括解決 (SingletonPreResolved)
の 2 モードで切り替え可能にしたサンプルです。  
さらに BenchmarkDotNet を用いて両者の実行オーバーヘッドを比較するテンプレートを含みます。

## モード概要

| モード | 特徴 | 利点 | 注意点 |
|-------|------|------|--------|
| ScopedPerExecution | 毎回 IServiceScope を生成し中で各ミドルウェアを解決 | Scoped 依存をそのまま使える。リクエスト固有状態をコンストラクタで適切に注入可能 | スコープ生成 & GetRequiredService のオーバーヘッド |
| SingletonPreResolved | Build() 時にミドルウェアを一括解決し delegate チェーンを固定 | 実行時オーバーヘッド極小 | すべて Singleton / thread-safe 前提。Scoped 依存を直接コンストラクタには受けられない |

## 主な型

- `PipelineLifetimeMode` : モード列挙
- `IPipelineBuilder` : 共通ビルダ API
- `ScopedPipelineBuilder` / `SingletonPipelineBuilder` : 実装
- `PipelineBuilderFactory.Create(IServiceProvider, PipelineLifetimeMode)` : モードに応じたビルダ生成
- `IMiddleware` : ミドルウェアインターフェイス
- `PipelineContext` : 実行コンテキスト

## 代表的な API

```
builder
  .Use(async (ctx, next) => { ...; await next(ctx); })
  .UseMiddleware<LoggingMiddleware>()
  .UseWhen(c => 条件, branch => { branch.Use(...); })
  .Map(c => 条件, branch => { branch.Run(ctx => Task.CompletedTask); })
  .Run(_ => Task.CompletedTask);
```

## 実行方法

### デモ (Console)

```
dotnet run -c Release -p src/SamplePipeline
```

### ベンチマーク

```
dotnet run -c Release -p src/SamplePipeline.Benchmarks
```

| Method            | Mean     | Ratio | Allocated | Alloc Ratio |
|------------------ |---------:|------:|----------:|------------:|
| SingletonPipeline | 2.868 us |  1.01 |      56 B |        1.00 |
| ScopedPipeline    | 6.456 us |  2.28 |     184 B |        3.29 |

## 拡張アイデア

1. Middleware のオプション (型パラメータによるジェネリックペイロード)
2. OpenTelemetry Activity 連携
3. ブランチキャッシュ最適化 (大規模条件分岐)
4. Source Generator による静的チェーン生成
5. CancellationToken 追加 (PipelineContext に保持しミドルウェアへ伝搬)

## 設計メモ

- Singleton モードでは `ctx.Services` は root provider を指す（必要な場合のみ利用）
- Scoped モードでは実行毎に `ctx.Services` をスコープに設定
- どちらのモードでもビルダ API を統一し、利用側の差異を最小化
- `UseWhen` と `Map` は内部で同種モードのビルダを新規生成しネスト

## ライフタイム指針

| 使用したい依存 | 推奨モード |
|----------------|-----------|
| Scoped/Transient をコンストラクタ注入 | ScopedPerExecution |
| Singleton のみ / 低レイテンシ必須 | SingletonPreResolved |

## ベンチマーク拡張例

- `MiddlewareWork` を CPU 負荷 / I/O / Task.Delay などに変更しパイプラインオーバーヘッド比率を把握
- `ThreadingDiagnoser` 有効化でコンテキストスイッチ観測
- GC 設定 (Server GC / Workstation GC) 切替

## ライセンス

自由に改変してご利用ください。
