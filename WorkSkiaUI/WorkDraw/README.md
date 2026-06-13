# WorkDraw

Blazor Server 製の draw.io 風ダイアグラムエディタです。AWS 構成図の作成に特化したステンシル（VPC・サブネット・EC2・Lambda・S3・RDS など 26 種）を備えています。

![editor](.verify/02-sample.png)

## 実行方法

```powershell
dotnet run
```

起動後、コンソールに表示された URL（例: http://localhost:5210）をブラウザで開きます。
ツールバーの「サンプル」を押すと 3 層構成のサンプル図が読み込まれます。

## 機能

| 操作 | 方法 |
|---|---|
| 図形の配置 | パレットからキャンバスへドラッグ＆ドロップ（クリックで中央配置も可） |
| 移動 | 図形をドラッグ（既定で 20px グリッドにスナップ。ツールバーで ON/OFF） |
| 接続 | 図形にホバー → 表示される○ポートをドラッグし、接続先の図形の上で離す |
| 接続線 | draw.io 風の直交（カギ型）ルーティング。位置関係に応じて自動で辺を選択 |
| リサイズ | 選択時の四隅ハンドルをドラッグ |
| 名前変更 | 図形をダブルクリックしてインライン編集（Enter 確定 / Esc キャンセル） |
| コンテナ | VPC・サブネット・AZ は枠として配置。コンテナを動かすと内部の図形も追従 |
| 削除 | 選択して Delete / Backspace、またはツールバーの「削除」 |
| 元に戻す / やり直す | Ctrl+Z / Ctrl+Y（最大 50 履歴） |
| ズーム | マウスホイール（カーソル位置中心）、ツールバーの ±・100%・全体表示 |
| パン | キャンバスの空白部分をドラッグ |
| 保存 / 読み込み | JSON 形式でダウンロード / 「開く」で復元 |
| 画像出力 | 「PNG出力」で図の範囲を 2 倍解像度の PNG としてダウンロード |

## 構成

```
Models/Diagram.cs            ノード・エッジのモデルと AWS ステンシルカタログ
Services/DiagramState.cs     図面状態・スナップ・直交ルーティング・Undo/Redo・JSON 入出力
Components/Pages/Editor.razor(.cs)  エディタ画面とマウス/キーボード操作のロジック
Components/Shared/DiagramNode.razor 1 ノードの SVG 描画（ポート・選択枠・リサイズハンドル含む）
Components/Shared/AwsGlyph.razor    AWS サービスの SVG ピクトグラム
wwwroot/js/diagram.js        座標取得・ファイル保存・PNG 書き出し・キーボード捕捉
```

描画は単一の SVG で行い、ノードやエッジは C# の状態から Razor で宣言的にレンダリングしています。
マウスイベントは Blazor のイベントハンドラ（SignalR 経由）で処理しています。

## 動作確認

`.verify/` に Playwright のスモークテストがあります（要 Node.js / Edge）。

```powershell
# アプリを起動した状態で
node .verify/smoke.js    # サンプル読込・クリック配置・ドラッグ移動
node .verify/smoke2.js   # D&D 配置・ポート接続・ラベル編集・削除・Undo
```
