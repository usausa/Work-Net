using System.Diagnostics.CodeAnalysis;

// このファイルは各サンプル(コンソールアプリ)へリンク参照され、共通の警告抑制を適用する。
// BasicAgentSample で既に採用済みの抑制内容と同じ。
[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "コンソールアプリには同期コンテキストが無いため不要")]
[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "サンプルのコンソールメッセージはローカライズ対象外")]
