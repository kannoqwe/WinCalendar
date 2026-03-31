using System;
using System.Runtime.InteropServices;

namespace Planner.App.Modules.Tasks.Infrastructure.Sqlite;

internal sealed class SqliteException : Exception
{
    public SqliteException(string message, int errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public int ErrorCode { get; }

    public static void ThrowIfError(int result, nint connectionHandle, string operation)
    {
        if (result == SqliteNative.Ok)
            return;

        string message = "Unknown SQLite error.";

        if (connectionHandle != 0)
        {
            nint rawMessage = SqliteNative.sqlite3_errmsg(connectionHandle);
            message = Marshal.PtrToStringUTF8(rawMessage) ?? message;
        }

        throw new SqliteException($"{operation} failed with SQLite error {result}: {message}", result);
    }
}
