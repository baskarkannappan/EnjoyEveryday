using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace EnjoyEveryday.Infrastructure.Data;

/// <summary>
/// Modular extensions for calling PostgreSQL stored procedures and table-valued functions
/// without handwriting inline SQL strings in repositories.
/// </summary>
public static class DbRoutineExtensions
{
    /// <summary>
    /// Invokes a PostgreSQL set-returning function and returns mapped entities.
    /// Executes: SELECT * FROM {functionName}(@param1, @param2...)
    /// </summary>
    public static async Task<IEnumerable<T>> QueryFunctionAsync<T>(
        this IDbConnection connection,
        string functionName,
        object? param = null,
        string[]? parameterNames = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var paramList = ResolveParameterList(param, parameterNames);
        var sql = string.IsNullOrEmpty(paramList)
            ? $"SELECT * FROM {functionName}()"
            : $"SELECT * FROM {functionName}({paramList})";

        return await connection.QueryAsync<T>(new CommandDefinition(sql, param, transaction, commandTimeout, cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Invokes a PostgreSQL set-returning function and returns the first row or default.
    /// Executes: SELECT * FROM {functionName}(@param1, @param2...)
    /// </summary>
    public static async Task<T?> QuerySingleFunctionAsync<T>(
        this IDbConnection connection,
        string functionName,
        object? param = null,
        string[]? parameterNames = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var paramList = ResolveParameterList(param, parameterNames);
        var sql = string.IsNullOrEmpty(paramList)
            ? $"SELECT * FROM {functionName}()"
            : $"SELECT * FROM {functionName}({paramList})";

        return await connection.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, param, transaction, commandTimeout, cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Invokes a PostgreSQL scalar function and returns the scalar value.
    /// Executes: SELECT {functionName}(@param1, @param2...)
    /// </summary>
    public static async Task<T?> ExecuteScalarFunctionAsync<T>(
        this IDbConnection connection,
        string functionName,
        object? param = null,
        string[]? parameterNames = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var paramList = ResolveParameterList(param, parameterNames);
        var sql = string.IsNullOrEmpty(paramList)
            ? $"SELECT {functionName}()"
            : $"SELECT {functionName}({paramList})";

        return await connection.ExecuteScalarAsync<T>(new CommandDefinition(sql, param, transaction, commandTimeout, cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Invokes a PostgreSQL stored procedure.
    /// Executes: CALL {procedureName}(@param1, @param2...)
    /// </summary>
    public static async Task<int> ExecuteProcedureAsync(
        this IDbConnection connection,
        string procedureName,
        object? param = null,
        string[]? parameterNames = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var paramList = ResolveParameterList(param, parameterNames);
        var sql = string.IsNullOrEmpty(paramList)
            ? $"CALL {procedureName}()"
            : $"CALL {procedureName}({paramList})";

        return await connection.ExecuteAsync(new CommandDefinition(sql, param, transaction, commandTimeout, cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Invokes a PostgreSQL stored procedure returning a refcursor and returns mapped entities.
    /// Executes: CALL {procedureName}(@param1, @param2..., @cur); FETCH ALL FROM "{cursorName}";
    /// </summary>
    public static async Task<IEnumerable<T>> QueryProcedureAsync<T>(
        this IDbConnection connection,
        string procedureName,
        object? param = null,
        string[]? parameterNames = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        bool ownsTransaction = false;
        if (transaction == null)
        {
            transaction = connection.BeginTransaction();
            ownsTransaction = true;
        }

        try
        {
            var cursorName = "c_" + Guid.NewGuid().ToString("N");
            var dynParams = new DynamicParameters(param);
            dynParams.Add("cur", cursorName);

            var paramList = ResolveParameterList(param, parameterNames);
            var callParams = string.IsNullOrEmpty(paramList) ? "@cur::refcursor" : $"{paramList}, @cur::refcursor";
            var callSql = $"CALL {procedureName}({callParams});";

            await connection.ExecuteAsync(new CommandDefinition(
                callSql,
                dynParams,
                transaction,
                commandTimeout,
                cancellationToken: cancellationToken));

            var fetchSql = $"FETCH ALL FROM \"{cursorName}\";";
            var rows = await connection.QueryAsync<T>(new CommandDefinition(
                fetchSql,
                transaction: transaction,
                commandTimeout: commandTimeout,
                cancellationToken: cancellationToken));

            if (ownsTransaction)
            {
                transaction.Commit();
            }

            return rows;
        }
        catch
        {
            if (ownsTransaction)
            {
                try { transaction.Rollback(); } catch { }
            }
            throw;
        }
    }

    /// <summary>
    /// Invokes a PostgreSQL stored procedure returning a refcursor and returns the first row or default.
    /// </summary>
    public static async Task<T?> QuerySingleProcedureAsync<T>(
        this IDbConnection connection,
        string procedureName,
        object? param = null,
        string[]? parameterNames = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await connection.QueryProcedureAsync<T>(
            procedureName,
            param,
            parameterNames,
            transaction,
            commandTimeout,
            cancellationToken);

        return rows.FirstOrDefault();
    }

    private static string ResolveParameterList(object? param, string[]? parameterNames)
    {
        if (parameterNames != null && parameterNames.Length > 0)
        {
            return string.Join(", ", parameterNames.Select(p => p.StartsWith("@") ? p : "@" + p));
        }

        return BuildParameterList(param);
    }

    private static string BuildParameterList(object? param)
    {
        if (param == null) return string.Empty;

        if (param is DynamicParameters dynamicParams)
        {
            return string.Join(", ", dynamicParams.ParameterNames.Select(p => "@" + p));
        }

        if (param is IEnumerable<KeyValuePair<string, object?>> dict)
        {
            return string.Join(", ", dict.Select(kv => "@" + kv.Key));
        }

        var props = param.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(p => p.CanRead);
        return string.Join(", ", props.Select(p => "@" + p.Name));
    }
}
