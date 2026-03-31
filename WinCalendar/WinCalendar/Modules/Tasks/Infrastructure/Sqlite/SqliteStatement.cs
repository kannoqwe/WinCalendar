using System;
using System.Runtime.InteropServices;

namespace Planner.App.Modules.Tasks.Infrastructure.Sqlite;

internal sealed class SqliteStatement : IDisposable
{
    private readonly SqliteConnection _connection;
    private nint _handle;

    public SqliteStatement(SqliteConnection connection, string sql)
    {
        _connection = connection;

        int result = SqliteNative.sqlite3_prepare_v2(connection.Handle, sql, -1, out _handle, out _);
        SqliteException.ThrowIfError(result, connection.Handle, "Preparing statement");
    }

    public void BindText(int index, string value)
    {
        SqliteException.ThrowIfError(
            SqliteNative.sqlite3_bind_text(_handle, index, value, -1, SqliteNative.Transient),
            _connection.Handle,
            "Binding text parameter");
    }

    public void BindNullableText(int index, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            BindNull(index);
            return;
        }

        BindText(index, value);
    }

    public void BindBoolean(int index, bool value)
    {
        BindInt(index, value ? 1 : 0);
    }

    public void BindInt(int index, int value)
    {
        SqliteException.ThrowIfError(
            SqliteNative.sqlite3_bind_int(_handle, index, value),
            _connection.Handle,
            "Binding int parameter");
    }

    public void BindNull(int index)
    {
        SqliteException.ThrowIfError(
            SqliteNative.sqlite3_bind_null(_handle, index),
            _connection.Handle,
            "Binding null parameter");
    }

    public void ExecuteNonQuery()
    {
        int result = SqliteNative.sqlite3_step(_handle);

        if (result == SqliteNative.Done)
            return;

        SqliteException.ThrowIfError(result, _connection.Handle, "Executing statement");
    }

    public bool Read()
    {
        int result = SqliteNative.sqlite3_step(_handle);

        if (result == SqliteNative.Row)
            return true;

        if (result == SqliteNative.Done)
            return false;

        SqliteException.ThrowIfError(result, _connection.Handle, "Reading row");
        return false;
    }

    public string? GetText(int index)
    {
        if (SqliteNative.sqlite3_column_type(_handle, index) == SqliteNative.Null)
            return null;

        nint value = SqliteNative.sqlite3_column_text(_handle, index);
        return Marshal.PtrToStringUTF8(value);
    }

    public int GetInt(int index)
    {
        return SqliteNative.sqlite3_column_int(_handle, index);
    }

    public void Dispose()
    {
        if (_handle == 0)
            return;

        SqliteException.ThrowIfError(SqliteNative.sqlite3_finalize(_handle), _connection.Handle, "Finalizing statement");
        _handle = 0;
    }
}
