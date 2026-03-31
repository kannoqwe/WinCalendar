using System;

namespace Planner.App.Modules.Tasks.Infrastructure.Sqlite;

internal sealed class SqliteConnection : IDisposable
{
    private nint _handle;

    public SqliteConnection(string databasePath)
    {
        int flags = SqliteNative.OpenReadWrite | SqliteNative.OpenCreate | SqliteNative.OpenFullMutex;
        int result = SqliteNative.sqlite3_open_v2(databasePath, out _handle, flags, 0);

        if (result != SqliteNative.Ok)
        {
            nint failedHandle = _handle;
            _handle = 0;

            if (failedHandle != 0)
                SqliteNative.sqlite3_close_v2(failedHandle);

            SqliteException.ThrowIfError(result, failedHandle, "Opening database");
        }
    }

    public nint Handle => _handle;

    public SqliteStatement Prepare(string sql)
    {
        ObjectDisposedException.ThrowIf(_handle == 0, this);
        return new SqliteStatement(this, sql);
    }

    public void ExecuteNonQuery(string sql)
    {
        using SqliteStatement statement = Prepare(sql);
        statement.ExecuteNonQuery();
    }

    public void Dispose()
    {
        if (_handle == 0)
            return;

        SqliteException.ThrowIfError(SqliteNative.sqlite3_close_v2(_handle), _handle, "Closing database");
        _handle = 0;
    }
}
