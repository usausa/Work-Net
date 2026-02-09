namespace WorkParser.SqlParsing;

public enum SqlTokenKind
{
    Unknown = 0,

    /// <summary>
    /// One or more whitespace characters. Can be skipped by the lexer.
    /// </summary>
    Whitespace,

    /// <summary>
    /// Identifiers and keywords. Includes quoted identifiers like [name].
    /// </summary>
    Identifier,

    /// <summary>
    /// Numeric literals (e.g. 1, 3.14).
    /// </summary>
    Number,

    /// <summary>
    /// String literals (e.g. 'abc', N'abc').
    /// </summary>
    String,

    /// <summary>
    /// Other single-character symbols (e.g. ',', '(', ')', '+', '*', ';').
    /// </summary>
    Symbol,

    /// <summary>
    /// Line comment starting with -- (ends at newline).
    /// </summary>
    CommentLine,

    /// <summary>
    /// Block comment enclosed by /* ... */.
    /// </summary>
    CommentBlock,

    /// <summary>
    /// End of input sentinel.
    /// </summary>
    EndOfFile,
}
