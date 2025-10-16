using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SqlOptimizerHelper.Models;

namespace SqlOptimizerHelper.Services
{
    /// <summary>
    /// Analyzes SQL queries to detect missing indexes
    /// </summary>
    public class IndexAnalyzer
    {
        private readonly SqlOptimizerConfig _config;

        public IndexAnalyzer(SqlOptimizerConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Analyzes a query for missing index opportunities
        /// </summary>
        /// <param name="metrics">Query execution metrics</param>
        /// <returns>List of missing index suggestions</returns>
        public List<QueryAnalysisResult> AnalyzeForMissingIndexes(QueryMetrics metrics)
        {
            var results = new List<QueryAnalysisResult>();

            if (!metrics.IsSelectQuery || string.IsNullOrEmpty(metrics.CommandText))
                return results;

            // Extract table and column information from WHERE clauses
            var whereClauses = ExtractWhereClauses(metrics.CommandText);
            
            foreach (var clause in whereClauses)
            {
                var suggestion = AnalyzeWhereClause(clause, metrics.CommandText);
                if (suggestion != null)
                {
                    results.Add(suggestion);
                }
            }

            return results;
        }

        /// <summary>
        /// Extracts WHERE clauses from SQL query
        /// </summary>
        /// <param name="query">SQL query</param>
        /// <returns>List of WHERE clause conditions</returns>
        private List<string> ExtractWhereClauses(string query)
        {
            var clauses = new List<string>();

            if (string.IsNullOrEmpty(query))
                return clauses;

            // Find WHERE clause
            var whereMatch = Regex.Match(query, @"WHERE\s+(.+?)(?:\s+ORDER\s+BY|\s+GROUP\s+BY|\s+HAVING|$)", 
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!whereMatch.Success)
                return clauses;

            var whereClause = whereMatch.Groups[1].Value.Trim();

            // Split by AND/OR but be careful with parentheses
            var conditions = SplitWhereConditions(whereClause);
            clauses.AddRange(conditions);

            return clauses;
        }

        /// <summary>
        /// Splits WHERE conditions while respecting parentheses
        /// </summary>
        /// <param name="whereClause">WHERE clause text</param>
        /// <returns>List of individual conditions</returns>
        private List<string> SplitWhereConditions(string whereClause)
        {
            var conditions = new List<string>();
            var currentCondition = "";
            var parenCount = 0;

            foreach (var token in whereClause.Split(' '))
            {
                if (token.Equals("AND", StringComparison.OrdinalIgnoreCase) && parenCount == 0)
                {
                    if (!string.IsNullOrEmpty(currentCondition.Trim()))
                    {
                        conditions.Add(currentCondition.Trim());
                        currentCondition = "";
                    }
                }
                else if (token.Equals("OR", StringComparison.OrdinalIgnoreCase) && parenCount == 0)
                {
                    if (!string.IsNullOrEmpty(currentCondition.Trim()))
                    {
                        conditions.Add(currentCondition.Trim());
                        currentCondition = "";
                    }
                }
                else
                {
                    currentCondition += token + " ";
                    parenCount += token.Count(c => c == '(');
                    parenCount -= token.Count(c => c == ')');
                }
            }

            if (!string.IsNullOrEmpty(currentCondition.Trim()))
            {
                conditions.Add(currentCondition.Trim());
            }

            return conditions;
        }

        /// <summary>
        /// Analyzes a WHERE clause condition for index opportunities
        /// </summary>
        /// <param name="condition">WHERE condition</param>
        /// <param name="fullQuery">Full SQL query for context</param>
        /// <returns>Analysis result with index suggestion</returns>
        private QueryAnalysisResult? AnalyzeWhereClause(string condition, string fullQuery)
        {
            if (string.IsNullOrEmpty(condition))
                return null;

            // Extract table and column from condition
            var tableName = ExtractTableName(fullQuery);
            var columnName = ExtractColumnFromCondition(condition);

            if (string.IsNullOrEmpty(columnName))
                return null;

            // Check for different types of conditions that benefit from indexes
            var indexSuggestion = AnalyzeConditionType(condition, tableName, columnName);
            
            if (indexSuggestion != null)
            {
                return new QueryAnalysisResult
                {
                    Query = fullQuery,
                    IssueType = "MissingIndex",
                    Severity = GetIndexSeverity(condition),
                    TableName = tableName,
                    ColumnName = columnName,
                    Warning = $"No index on '{tableName}.{columnName}'. Consider creating an index.",
                    Suggestion = indexSuggestion,
                    Context = $"Condition: {condition}"
                };
            }

            return null;
        }

        /// <summary>
        /// Analyzes the type of condition and suggests appropriate index
        /// </summary>
        /// <param name="condition">WHERE condition</param>
        /// <param name="tableName">Table name</param>
        /// <param name="columnName">Column name</param>
        /// <returns>Index creation suggestion</returns>
        private string? AnalyzeConditionType(string condition, string tableName, string columnName)
        {
            var upperCondition = condition.ToUpper();

            // Equality conditions
            if (upperCondition.Contains("=") && !upperCondition.Contains("!=") && !upperCondition.Contains("<>"))
            {
                return $"CREATE INDEX IX_{tableName}_{columnName} ON {tableName}({columnName})";
            }

            // LIKE conditions (especially with leading wildcards)
            if (upperCondition.Contains("LIKE"))
            {
                if (upperCondition.Contains("'%") || upperCondition.Contains("'%"))
                {
                    return $"CREATE INDEX IX_{tableName}_{columnName} ON {tableName}({columnName}) -- Note: LIKE with leading % may not use index efficiently";
                }
                return $"CREATE INDEX IX_{tableName}_{columnName} ON {tableName}({columnName})";
            }

            // Range conditions
            if (upperCondition.Contains(">") || upperCondition.Contains("<") || 
                upperCondition.Contains(">=") || upperCondition.Contains("<=") ||
                upperCondition.Contains("BETWEEN"))
            {
                return $"CREATE INDEX IX_{tableName}_{columnName} ON {tableName}({columnName})";
            }

            // IN conditions
            if (upperCondition.Contains("IN"))
            {
                return $"CREATE INDEX IX_{tableName}_{columnName} ON {tableName}({columnName})";
            }

            // IS NULL conditions
            if (upperCondition.Contains("IS NULL") || upperCondition.Contains("IS NOT NULL"))
            {
                return $"CREATE INDEX IX_{tableName}_{columnName} ON {tableName}({columnName}) WHERE {columnName} IS NOT NULL";
            }

            return null;
        }

        /// <summary>
        /// Extracts column name from a WHERE condition
        /// </summary>
        /// <param name="condition">WHERE condition</param>
        /// <returns>Column name or empty string</returns>
        private string ExtractColumnFromCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition))
                return string.Empty;

            // Pattern: column_name = value or column_name LIKE value
            var match = Regex.Match(condition, @"(\w+)\s*(?:=|LIKE|>|<|>=|<=|IN|BETWEEN|IS)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // Pattern: value = column_name (reverse order)
            match = Regex.Match(condition, @"(?:=|LIKE|>|<|>=|<=|IN|BETWEEN|IS)\s*(\w+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }

        /// <summary>
        /// Extracts table name from SQL query
        /// </summary>
        /// <param name="query">SQL query</param>
        /// <returns>Table name or empty string</returns>
        private string ExtractTableName(string query)
        {
            if (string.IsNullOrEmpty(query))
                return string.Empty;

            // Match FROM table_name pattern
            var match = Regex.Match(query, @"FROM\s+(\w+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }

        /// <summary>
        /// Determines severity level for index suggestions
        /// </summary>
        /// <param name="condition">WHERE condition</param>
        /// <returns>Severity level</returns>
        private string GetIndexSeverity(string condition)
        {
            var upperCondition = condition.ToUpper();

            // High severity for equality and range conditions
            if (upperCondition.Contains("=") || upperCondition.Contains(">") || 
                upperCondition.Contains("<") || upperCondition.Contains("BETWEEN"))
            {
                return "High";
            }

            // Medium severity for LIKE and IN conditions
            if (upperCondition.Contains("LIKE") || upperCondition.Contains("IN"))
            {
                return "Medium";
            }

            // Low severity for IS NULL conditions
            if (upperCondition.Contains("IS NULL"))
            {
                return "Low";
            }

            return "Medium";
        }
    }
}
