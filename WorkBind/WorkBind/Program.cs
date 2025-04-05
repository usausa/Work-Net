namespace WorkBind;

internal static class Program
{
    static void Main()
    {
        //var str = "Param1 = aaa Param2 = \"b\\\"bb\" Param3 = ccc";
        var str = "Value=123 Value2= Value3=222 Flag=true Name=\"aaa bbb\" Point=1,2";

        var param = new Parameter();

        ParameterBinder.Bind(param, str);
    }
}

public static class ParameterBinder
{
    public static void Bind<T>(T target, ReadOnlySpan<char> text)
    {
        var type = typeof(T);

        var enumerator = new TokenEnumerator(text);
        while (enumerator.MoveNext())
        {
            var pi = type.GetProperty(enumerator.Key.ToString());
            if (pi == null)
            {
                // Ignore
                continue;
            }

            var converter = pi.GetCustomAttributes(true)
                .OfType<ConverterAttribute>()
                .FirstOrDefault();
            var value = converter is not null
                ? converter.FromString(enumerator.Value)
                : FromString(enumerator.Value, pi.PropertyType);
            pi.SetValue(target, value);
        }
    }

    private static object FromString(ReadOnlySpan<char> value, Type type)
    {
        if (value.IsEmpty)
        {
            return Activator.CreateInstance(type)!;
        }

        if (type == typeof(string))
        {
            return value.ToString();
        }

        return Convert.ChangeType(value.ToString(), Nullable.GetUnderlyingType(type) ?? type);
    }
}

[AttributeUsage(AttributeTargets.Property)]
public abstract class ConverterAttribute : Attribute
{
    public abstract object FromString(ReadOnlySpan<char> text);
}

#pragma warning disable CA1515
public ref struct TokenEnumerator
{
    private ReadOnlySpan<char> remaining;

    public TokenEnumerator(ReadOnlySpan<char> text)
    {
        remaining = text;
        Key = default;
        Value = default;
    }

    public ReadOnlySpan<char> Key { get; private set; }

    public ReadOnlySpan<char> Value { get; private set; }

    public bool MoveNext()
    {
        // 残りの文字列がない場合は終了
        remaining = remaining.TrimStart();
        if (remaining.IsEmpty)
        {
            return false;
        }

        // キーの取得
        var equalIndex = remaining.IndexOf('=');
        if (equalIndex == -1)
        {
            return false;
        }

        Key = remaining[..equalIndex];

        // 値の解析
        remaining = remaining[(equalIndex + 1)..];
        if (remaining.IsEmpty)
        {
            Value = default;
            return true;
        }

        if (remaining[0] == '"')
        {
            // クォートされた値の処理
            remaining = remaining[1..];

            var length = 0;
            var escape = false;
            for (; length < remaining.Length; length++)
            {
                var c = remaining[length];
                if (escape)
                {
                    escape = false;
                }
                else if (c == '\\')
                {
                    escape = true;
                }
                else if (c == '"')
                {
                    break;
                }
            }

            Value = remaining[..length];
            remaining = length + 1 < remaining.Length ? remaining[(length + 1)..] : default;
        }
        else
        {
            // 値範囲確定
            var spaceIndex = remaining.IndexOf(' ');
            if (spaceIndex == -1)
            {
                Value = remaining;
                remaining = default;
            }
            else
            {
                Value = remaining[..spaceIndex];
                remaining = remaining[spaceIndex..];
            }
        }

        return true;
    }
}

public class Parameter
{
    public string Name { get; set; } = default!;

    public int Value { get; set; }

    public int? Value2 { get; set; }

    public int? Value3 { get; set; }

    public bool Flag { get; set; }

    [PointConverter]
    public Point Point { get; set; }
}

public struct Point
{
    public int X { get; set; }
    public int Y { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class PointConverterAttribute : ConverterAttribute
{
    public override object FromString(ReadOnlySpan<char> text)
    {
        Span<Range> ranges = stackalloc Range[2];
        var splitCount = text.Split(ranges, ',');
        if ((splitCount == 2) && (Int32.TryParse(text[ranges[0]], out var x) && Int32.TryParse(text[ranges[1]], out var y)))
        {
            return new Point { X = x, Y = y };
        }

        throw new FormatException();
    }
}
