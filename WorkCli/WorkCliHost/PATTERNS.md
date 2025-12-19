# 共通引数定義パターンのサマリー

## 実装されているパターン

### 1. 抽象基底クラス ⭐推奨⭐

**場所**: `UserCommands.cs` - `UserRoleCommandBase`

**メリット**:
- 最もシンプルで直感的
- 属性が自動的に継承される
- IntelliSenseが完璧に機能
- コンパイル時の型チェック

**使用例**:
```csharp
public abstract class UserRoleCommandBase : ICommandDefinition
{
    [CliArgument<string>(0, "username", Description = "Username")]
    public string Username { get; set; } = default!;

    [CliArgument<string>(1, "role", Description = "Role name")]
    public string Role { get; set; } = default!;

    public abstract ValueTask ExecuteAsync();
}
```

**適用コマンド**:
- `UserRoleAssignCommand`
- `UserRoleRemoveCommand`

**コマンドライン**:
```bash
app user role assign alice admin
app user role remove bob editor
```

---

### 2. ジェネリック基底クラス

**場所**: `AdvancedCommandPatterns.cs` - `CommandWithUserAndRole<TLogger>`

**メリット**:
- ロガーの型も共通化
- 高い再利用性
- 複雑なシナリオに対応

**使用例**:
```csharp
public abstract class CommandWithUserAndRole<TLogger> : ICommandDefinition
{
    protected readonly ILogger<TLogger> Logger;

    protected CommandWithUserAndRole(ILogger<TLogger> logger)
    {
        Logger = logger;
    }

    [CliArgument<string>(0, "username", Description = "Username")]
    public string Username { get; set; } = default!;

    [CliArgument<string>(1, "role", Description = "Role name")]
    public string Role { get; set; } = default!;

    public abstract ValueTask ExecuteAsync();
}
```

**適用コマンド**:
- `UserRoleVerifyCommand`

**コマンドライン**:
```bash
app user role verify alice admin
```

---

### 3. インターフェース（型の契約）

**場所**: `AdvancedCommandPatterns.cs` - `IUserTargetArguments`, `IRoleArguments`

**メリット**:
- 複数の共通引数セットを組み合わせ可能
- 型の契約として機能

**制限**:
- 属性は自動継承されない（明示的に付ける必要がある）

**使用例**:
```csharp
public interface IUserTargetArguments
{
    string Username { get; set; }
}

public sealed class UserPermissionGrantCommand : ICommandDefinition, IUserTargetArguments, IRoleArguments
{
    [CliArgument<string>(0, "username", Description = "Username")]
    public string Username { get; set; } = default!;

    [CliArgument<string>(1, "role", Description = "Role name")]
    public string Role { get; set; } = default!;

    [CliArgument<string>(2, "permission", Description = "Permission to grant")]
    public string Permission { get; set; } = default!;
    
    // ...
}
```

**適用コマンド**:
- `UserPermissionGrantCommand`

**コマンドライン**:
```bash
app user grant alice editor read
```

---

## パターン選択ガイド

| シナリオ | 推奨パターン | 理由 |
|---------|------------|------|
| 関連するコマンドグループ | 抽象基底クラス | 最もシンプル、属性自動継承 |
| ロガーも共通化したい | ジェネリック基底クラス | Logger型も含めて汎用化 |
| 複数の共通引数セットを組み合わせ | インターフェース | 柔軟な組み合わせ（属性は手動） |
| 大規模プロジェクト | ジェネリック基底クラス | 高い再利用性 |

## ベストプラクティス

1. **デフォルトは抽象基底クラスを使用**
   - 最もシンプルで保守性が高い
   
2. **ロガーも共通化する場合はジェネリック基底クラス**
   - コードの重複を最小化

3. **インターフェースは型の契約として使用**
   - 属性は実装クラスで明示的に定義
   - 複数の共通引数セットを組み合わせる場合に有効

4. **命名規則**
   - 基底クラス: `{CommandGroup}CommandBase` または `CommandWith{Features}`
   - インターフェース: `I{Feature}Arguments`

## 実装例の動作確認

すべてのパターンが正常に動作します：

```bash
# パターン1: 抽象基底クラス
dotnet run -- user role assign alice admin
dotnet run -- user role remove bob editor

# パターン2: ジェネリック基底クラス
dotnet run -- user role verify charlie manager

# パターン3: インターフェース
dotnet run -- user grant david editor write
```
