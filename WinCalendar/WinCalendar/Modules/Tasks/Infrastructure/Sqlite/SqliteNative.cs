using System;
using System.Runtime.InteropServices;

namespace Planner.App.Modules.Tasks.Infrastructure.Sqlite;

internal static class SqliteNative
{
    public const int Ok = 0;
    public const int Row = 100;
    public const int Done = 101;
    public const int Null = 5;

    public const int OpenReadWrite = 0x00000002;
    public const int OpenCreate = 0x00000004;
    public const int OpenFullMutex = 0x00010000;

    public static readonly nint Transient = new(-1);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_open_v2(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string filename,
        out nint db,
        int flags,
        nint vfs);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_close_v2(nint db);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_prepare_v2(
        nint db,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string sql,
        int byteCount,
        out nint statement,
        out nint tail);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_step(nint statement);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_finalize(nint statement);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_bind_text(
        nint statement,
        int index,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string value,
        int byteCount,
        nint destructor);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_bind_int(nint statement, int index, int value);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_bind_null(nint statement, int index);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint sqlite3_column_text(nint statement, int index);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_column_int(nint statement, int index);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_column_type(nint statement, int index);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint sqlite3_errmsg(nint db);
}
