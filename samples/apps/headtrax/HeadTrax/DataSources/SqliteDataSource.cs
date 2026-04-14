using System.Text;
using Duct.Data;
using Microsoft.Data.Sqlite;

namespace HeadTrax.DataSources;

/// <summary>
/// IDataSource&lt;Dictionary&lt;string, object?&gt;&gt; backed by a local SQLite database.
/// Translates DataRequest into parameterized SQL with server-side sort, filter,
/// search, count, and projection.
///
/// Written to live in the sample app for now but structured so it could move
/// into Duct.Data.Providers as a first-class provider later.
/// </summary>
internal sealed class SqliteDataSource :
    IDataSource<Dictionary<string, object?>>,
    IKeyedDataSource<Dictionary<string, object?>>,
    IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection _connection;

    // Column whitelist to prevent SQL injection
    private static readonly HashSet<string> AllowedColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "employee_number", "first_name", "last_name", "email", "phone",
        "title", "department", "location", "hire_date", "salary", "manager_id",
        "level", "status", "birth_date", "gender", "performance_rating",
        "stock_options", "is_remote", "cost_center", "created_at", "updated_at",
    };

    public SqliteDataSource(string dbPath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Shared,
        }.ToString();

        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        // Performance pragmas
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA cache_size=-32000;";
        cmd.ExecuteNonQuery();
    }

    /// <summary>Running total of rows fetched across all GetPageAsync calls.</summary>
    public int TotalRowsFetched { get; private set; }

    /// <summary>Optional callback fired after each page fetch with the new total.</summary>
    public Action<int>? OnRowsFetchedChanged { get; set; }

    public DataSourceCapabilities Capabilities =>
        DataSourceCapabilities.ServerSort |
        DataSourceCapabilities.ServerFilter |
        DataSourceCapabilities.ServerSearch |
        DataSourceCapabilities.ServerCount |
        DataSourceCapabilities.ServerSelect;

    public RowKey GetRowKey(Dictionary<string, object?> item) =>
        item.TryGetValue("id", out var id) && id is not null
            ? new RowKey(id.ToString()!)
            : throw new InvalidOperationException("Row missing 'id' key.");

    // ── Paged query ─────────────────────────────────────────────────────────

    public Task<DataPage<Dictionary<string, object?>>> GetPageAsync(
        DataRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sql = new StringBuilder();
        var countSql = new StringBuilder();
        var parameters = new List<SqliteParameter>();

        // SELECT columns
        var selectCols = "*";
        if (request.Select is { Count: > 0 })
        {
            var cols = new List<string> { "id" }; // always include id
            foreach (var col in request.Select)
            {
                var safe = ValidateColumn(col);
                if (safe != "id") cols.Add(safe);
            }
            selectCols = string.Join(", ", cols);
        }

        sql.Append($"SELECT {selectCols} FROM employees");
        countSql.Append("SELECT COUNT(*) FROM employees");

        // WHERE clauses
        var whereClauses = new List<string>();
        var paramIndex = 0;

        // Full-text search
        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            whereClauses.Add("id IN (SELECT rowid FROM employees_fts WHERE employees_fts MATCH @p" + paramIndex + ")");
            var escaped = request.SearchQuery.Replace("'", "").Replace("\"", "").Trim();
            parameters.Add(new SqliteParameter($"@p{paramIndex}", $"{escaped}*"));
            paramIndex++;
        }

        // Filters
        if (request.Filters is { Count: > 0 })
        {
            foreach (var filter in request.Filters)
            {
                var col = ValidateColumn(filter.Field);
                var (clause, filterParams) = BuildFilterClause(col, filter, ref paramIndex);
                whereClauses.Add(clause);
                parameters.AddRange(filterParams);
            }
        }

        if (whereClauses.Count > 0)
        {
            var whereStr = " WHERE " + string.Join(" AND ", whereClauses);
            sql.Append(whereStr);
            countSql.Append(whereStr);
        }

        // ORDER BY
        if (request.Sort is { Count: > 0 })
        {
            var orderParts = request.Sort.Select(s =>
            {
                var col = ValidateColumn(s.Field);
                var dir = s.Direction == SortDirection.Descending ? "DESC" : "ASC";
                return $"{col} {dir}";
            });
            sql.Append(" ORDER BY ").Append(string.Join(", ", orderParts));
        }
        else
        {
            sql.Append(" ORDER BY id ASC");
        }

        // LIMIT / OFFSET (continuation token = offset)
        var offset = 0;
        if (request.ContinuationToken is not null)
            int.TryParse(request.ContinuationToken, out offset);

        sql.Append($" LIMIT @limit OFFSET @offset");

        // Execute count query
        int totalCount;
        using (var countCmd = _connection.CreateCommand())
        {
            countCmd.CommandText = countSql.ToString();
            foreach (var p in parameters) countCmd.Parameters.Add(CloneParam(p));
            totalCount = Convert.ToInt32(countCmd.ExecuteScalar());
        }

        // Execute data query
        var items = new List<Dictionary<string, object?>>();
        using (var dataCmd = _connection.CreateCommand())
        {
            dataCmd.CommandText = sql.ToString();
            foreach (var p in parameters) dataCmd.Parameters.Add(CloneParam(p));
            dataCmd.Parameters.Add(new SqliteParameter("@limit", request.PageSize));
            dataCmd.Parameters.Add(new SqliteParameter("@offset", offset));

            using var reader = dataCmd.ExecuteReader();
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = new Dictionary<string, object?>(reader.FieldCount);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[name] = value;
                }
                items.Add(row);
            }
        }

        TotalRowsFetched += items.Count;
        OnRowsFetchedChanged?.Invoke(TotalRowsFetched);

        var nextOffset = offset + items.Count;
        var continuationToken = nextOffset < totalCount ? nextOffset.ToString() : null;

        return Task.FromResult(new DataPage<Dictionary<string, object?>>(items, continuationToken, totalCount));
    }

    // ── Keyed access ────────────────────────────────────────────────────────

    public Task<Dictionary<string, object?>?> GetByKeyAsync(
        RowKey key, CancellationToken cancellationToken = default)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM employees WHERE id = @id";
        cmd.Parameters.Add(new SqliteParameter("@id", key.Value));

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return Task.FromResult<Dictionary<string, object?>?>(null);

        var row = new Dictionary<string, object?>(reader.FieldCount);
        for (var i = 0; i < reader.FieldCount; i++)
            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

        return Task.FromResult<Dictionary<string, object?>?>(row);
    }

    public Task<IReadOnlyList<Dictionary<string, object?>>> GetByKeysAsync(
        IEnumerable<RowKey> keys, CancellationToken cancellationToken = default)
    {
        var keyList = keys.ToList();
        if (keyList.Count == 0)
            return Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(Array.Empty<Dictionary<string, object?>>());

        var placeholders = string.Join(",", keyList.Select((_, i) => $"@k{i}"));
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT * FROM employees WHERE id IN ({placeholders})";
        for (var i = 0; i < keyList.Count; i++)
            cmd.Parameters.Add(new SqliteParameter($"@k{i}", keyList[i].Value));

        var items = new List<Dictionary<string, object?>>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>(reader.FieldCount);
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            items.Add(row);
        }

        return Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(items);
    }

    // ── Filter translation ──────────────────────────────────────────────────

    private static (string clause, List<SqliteParameter> parameters) BuildFilterClause(
        string column, FilterDescriptor filter, ref int paramIndex)
    {
        var parameters = new List<SqliteParameter>();

        string clause = filter.Operator switch
        {
            FilterOperator.Equals =>
                WithParam($"{column} = @p{paramIndex}", filter.Value, ref paramIndex, parameters),
            FilterOperator.NotEquals =>
                WithParam($"{column} != @p{paramIndex}", filter.Value, ref paramIndex, parameters),
            FilterOperator.Contains =>
                WithParam($"{column} LIKE '%' || @p{paramIndex} || '%'", filter.Value, ref paramIndex, parameters),
            FilterOperator.StartsWith =>
                WithParam($"{column} LIKE @p{paramIndex} || '%'", filter.Value, ref paramIndex, parameters),
            FilterOperator.EndsWith =>
                WithParam($"{column} LIKE '%' || @p{paramIndex}", filter.Value, ref paramIndex, parameters),
            FilterOperator.GreaterThan =>
                WithParam($"{column} > @p{paramIndex}", filter.Value, ref paramIndex, parameters),
            FilterOperator.GreaterThanOrEqual =>
                WithParam($"{column} >= @p{paramIndex}", filter.Value, ref paramIndex, parameters),
            FilterOperator.LessThan =>
                WithParam($"{column} < @p{paramIndex}", filter.Value, ref paramIndex, parameters),
            FilterOperator.LessThanOrEqual =>
                WithParam($"{column} <= @p{paramIndex}", filter.Value, ref paramIndex, parameters),
            FilterOperator.Between =>
                WithTwoParams($"{column} BETWEEN @p{paramIndex} AND @p{paramIndex + 1}",
                    filter.Value, filter.ValueTo, ref paramIndex, parameters),
            FilterOperator.IsNull => $"{column} IS NULL",
            FilterOperator.IsNotNull => $"{column} IS NOT NULL",
            _ => "1=1",
        };

        return (clause, parameters);
    }

    private static string WithParam(string template, object? value, ref int paramIndex,
        List<SqliteParameter> parameters)
    {
        parameters.Add(new SqliteParameter($"@p{paramIndex}", value ?? DBNull.Value));
        paramIndex++;
        return template;
    }

    private static string WithTwoParams(string template, object? value1, object? value2,
        ref int paramIndex, List<SqliteParameter> parameters)
    {
        parameters.Add(new SqliteParameter($"@p{paramIndex}", value1 ?? DBNull.Value));
        parameters.Add(new SqliteParameter($"@p{paramIndex + 1}", value2 ?? DBNull.Value));
        paramIndex += 2;
        return template;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string ValidateColumn(string field)
    {
        if (AllowedColumns.Contains(field)) return field;
        throw new ArgumentException($"Invalid column name: '{field}'");
    }

    private static SqliteParameter CloneParam(SqliteParameter p) =>
        new(p.ParameterName, p.Value);

    public void Dispose()
    {
        _connection.Dispose();
    }
}
