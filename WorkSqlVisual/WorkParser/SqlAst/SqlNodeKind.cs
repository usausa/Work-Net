namespace WorkParser.SqlAst;

public enum SqlNodeKind
{
    /// <summary>
    /// Not classified / fallback.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Root container for a full SQL text.
    /// </summary>
    Statement,

    /// <summary>
    /// Raw token node (used when the parser does not build a higher-level node).
    /// </summary>
    Token,

    /// <summary>
    /// Any SQL comment token (line or block). Stored as a node to preserve trivia.
    /// </summary>
    Comment,

    /// <summary>
    /// SELECT statement.
    /// </summary>
    Select,

    /// <summary>
    /// WITH clause (CTE container).
    /// </summary>
    With,

    /// <summary>
    /// One CTE entry inside WITH.
    /// </summary>
    Cte,

    /// <summary>
    /// FROM clause.
    /// </summary>
    From,

    /// <summary>
    /// WHERE clause.
    /// </summary>
    Where,

    /// <summary>
    /// GROUP BY clause.
    /// </summary>
    GroupBy,

    /// <summary>
    /// HAVING clause.
    /// </summary>
    Having,

    /// <summary>
    /// ORDER BY clause.
    /// </summary>
    OrderBy,

    /// <summary>
    /// WINDOW clause (named window definitions).
    /// </summary>
    Window,

    /// <summary>
    /// OVER ( ... ) window specification.
    /// </summary>
    Over,

    /// <summary>
    /// PARTITION BY ... (within OVER or window definitions).
    /// </summary>
    PartitionBy,

    /// <summary>
    /// Window frame (ROWS/RANGE/GROUPS ...).
    /// </summary>
    Frame,

}
