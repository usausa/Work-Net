using System.Buffers;
using System.Diagnostics;

var text = "うさうさだよもん".AsSpan();

var words = SearchValues.Create(["うさうさ", "だよもん"], StringComparison.OrdinalIgnoreCase);

Debug.WriteLine(text.ContainsAny(words));
Debug.WriteLine(text.IndexOfAny(words));
