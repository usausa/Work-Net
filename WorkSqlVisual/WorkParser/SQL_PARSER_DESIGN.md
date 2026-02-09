# WorkParser SQLパーサー設計

## 目的 / スコープ

このパーサは、SQLテキストを対象とした **軽量な構造パーサ** を意図しています。

- `Span` とトークン文字列を使って **元のテキスト位置情報** を保持します。
- コメントをトリビアとして捨てず、**コメントもノードとして保持** します。
- よくあるステートメント/句の境界を部分的にAST化し、上位機能が
  - 句の抽出（`WITH`, `SELECT`, `FROM`, `WHERE`, `GROUP BY`, `HAVING`, `ORDER BY`, `WINDOW`）
  - Window指定（`OVER (...)`, `PARTITION BY ...`）のまとまり取得
  を行えるようにします。

この実装は **SQL方言の完全な構文検証器ではありません**。

## `SqlTokenKind` と `SqlNodeKind` の役割分担

### `SqlTokenKind`（字句解析レベル）

`SqlTokenKind` は「ソース文字列上の形」を表します。

例:

- `Identifier`: `SELECT`, `dbo`, `MyTable`, `[Quoted Name]`
- `String`: `'abc'`, `N'あいう'`
- `Symbol`: `,`, `(`, `)`, `*`, `;`
- `CommentLine`: `-- comment`
- `CommentBlock`: `/* comment */`

字句解析は、これ以上の意味解釈（例えば `SELECT` が予約語である等）はしません。

### `SqlNodeKind`（構文/ASTレベル）

`SqlNodeKind` は「構造上の意図」を表します。

- `Statement` がルートです。
- `Select`, `With`、および各句（`From`, `Where`, `GroupBy`, `Having`, `OrderBy`, `Window`）は構造ノードです。
- `Over` / `PartitionBy` は Window指定の構造ノードです。
- `Comment` はコメントを保持するための構造ノードです。
- `Token` は、構造ノードを切り出さない部分でも情報を失わないためのフォールバックです。

つまり:

- **Tokenは最小単位**（字句解析出力）
- **NodeはToken群を意味のある範囲に束ねる**（位置情報を保持したまま）

## どのレベルまで解析する想定か

現在の実装は以下を対象にしています。

- ステートメント境界のグルーピング: `WITH`, `SELECT`
- `SELECT` 内の句のグルーピング: `FROM`, `WHERE`, `GROUP BY`, `HAVING`, `ORDER BY`, `WINDOW`
- Window指定のグルーピング: `OVER (...)`, `PARTITION BY ...`

意図的に実装していないもの:

- 式（演算子優先順位）やJOINツリーの完全な構文木化
- 方言ごとの厳密な予約語/構文の検証
- `MERGE`, `INSERT ... SELECT` など全DML網羅

これらは、必要になったタイミングで `SqlNodeKind` とパーサ関数を追加し段階的に拡張します。

## `Span` の利用

- `SqlTextSpan` を `Token.Span` / `Node.Span` に使い、元の位置を常に保持します。
- キーワード判定は `ReadOnlySpan<char>` ベース（`Token.TextSpan.Equals(...)`）で比較し、余計なコストを避けます。
