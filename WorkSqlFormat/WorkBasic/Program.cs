using System.Diagnostics;
using PoorMansTSqlFormatterRedux;
using PoorMansTSqlFormatterRedux.Formatters;

var options = new TSqlStandardFormatterOptions
{
    //IndentString = "\t",
    IndentString = "    ",
    SpacesPerTab = 4,
    MaxLineWidth = 999,
    //ExpandCommaLists = true,
    ExpandCommaLists = false,
    //TrailingCommas = false,
    TrailingCommas = true,
    //SpaceAfterExpandedComma = false,
    SpaceAfterExpandedComma = true,
    ExpandBooleanExpressions = true,
    ExpandBetweenConditions = true,
    ExpandCaseStatements = true,
    UppercaseKeywords = true,
    //BreakJoinOnSections = false,
    BreakJoinOnSections = true,
    HTMLColoring = false,
    KeywordStandardization = false,
    ExpandInLists = true,
    NewClauseLineBreaks = 1,
    NewStatementLineBreaks = 2,
};
var formatter = new TSqlStandardFormatter(options);
var manager = new SqlFormattingManager(formatter);

var sql = "select Id, Name from Data";
var hasError = false;
var formattedSql = manager.Format(sql, ref hasError);

Debug.WriteLine(hasError);
Debug.WriteLine("----");
Debug.WriteLine(formattedSql);
Debug.WriteLine("----");
