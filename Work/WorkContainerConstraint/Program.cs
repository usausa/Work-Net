using System.Diagnostics;
using System.Text;

var cache = new Dictionary<object, object>
{
    ["foo"] = new object()
};

Debug.WriteLine(cache.ContainsKey("foo"));

var sb = new StringBuilder();
sb.Append('f').Append('o').Append('o');
var key = sb.ToString();

Debug.WriteLine(cache.ContainsKey(key));
