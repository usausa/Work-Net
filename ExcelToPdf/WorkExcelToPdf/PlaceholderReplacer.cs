namespace WorkExcelToPdf;

/// <summary>
/// シート情報のプレースホルダーを置換するヘルパークラス
/// </summary>
public class PlaceholderReplacer
{
    private readonly Dictionary<string, string> _replacements;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public PlaceholderReplacer()
    {
        _replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// プレースホルダーと置換文字列のペアを追加
    /// </summary>
    /// <param name="placeholder">プレースホルダー（例: "$NAME"）</param>
    /// <param name="value">置換する値</param>
    public PlaceholderReplacer Add(string placeholder, string value)
    {
        _replacements[placeholder] = value;
        return this;
    }

    /// <summary>
    /// プレースホルダーと置換文字列のペアを複数追加
    /// </summary>
    /// <param name="replacements">プレースホルダーと値のディクショナリ</param>
    public PlaceholderReplacer AddRange(Dictionary<string, string> replacements)
    {
        foreach (var pair in replacements)
        {
            _replacements[pair.Key] = pair.Value;
        }
        return this;
    }

    /// <summary>
    /// シート情報のすべてのセルに対してプレースホルダーを置換
    /// </summary>
    /// <param name="sheetInfo">置換対象のシート情報</param>
    /// <returns>置換されたセルの数</returns>
    public int Replace(SheetInfoEx sheetInfo)
    {
        int replacedCount = 0;

        foreach (var cell in sheetInfo.Cells)
        {
            if (string.IsNullOrEmpty(cell.Value))
                continue;

            string originalValue = cell.Value;
            string newValue = originalValue;

            // すべての登録されたプレースホルダーを置換
            foreach (var replacement in _replacements)
            {
                if (newValue.Contains(replacement.Key, StringComparison.OrdinalIgnoreCase))
                {
                    newValue = newValue.Replace(replacement.Key, replacement.Value, StringComparison.OrdinalIgnoreCase);
                }
            }

            // 値が変更された場合のみカウントと更新
            if (newValue != originalValue)
            {
                cell.Value = newValue;
                replacedCount++;
                Console.WriteLine($"  置換: {cell.CellAddress} '{originalValue}' → '{newValue}'");
            }
        }

        return replacedCount;
    }

    /// <summary>
    /// 特定のプレースホルダーを含むセルを検索
    /// </summary>
    /// <param name="sheetInfo">検索対象のシート情報</param>
    /// <param name="placeholder">検索するプレースホルダー</param>
    /// <returns>プレースホルダーを含むセルのリスト</returns>
    public List<CellBorderInfoEx> FindCells(SheetInfoEx sheetInfo, string placeholder)
    {
        return sheetInfo.Cells
            .Where(c => !string.IsNullOrEmpty(c.Value) && 
                       c.Value.Contains(placeholder, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// シート情報内のすべてのプレースホルダー候補を検出
    /// （$で始まる文字列を抽出）
    /// </summary>
    /// <param name="sheetInfo">検索対象のシート情報</param>
    /// <returns>検出されたプレースホルダー候補のリスト</returns>
    public HashSet<string> DetectPlaceholders(SheetInfoEx sheetInfo)
    {
        var placeholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pattern = new System.Text.RegularExpressions.Regex(@"\$[A-Z_][A-Z0-9_]*", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (var cell in sheetInfo.Cells)
        {
            if (string.IsNullOrEmpty(cell.Value))
                continue;

            var matches = pattern.Matches(cell.Value);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                placeholders.Add(match.Value);
            }
        }

        return placeholders;
    }

    /// <summary>
    /// 登録されているすべての置換ルールをクリア
    /// </summary>
    public void Clear()
    {
        _replacements.Clear();
    }

    /// <summary>
    /// 登録されている置換ルールの数を取得
    /// </summary>
    public int Count => _replacements.Count;
}
