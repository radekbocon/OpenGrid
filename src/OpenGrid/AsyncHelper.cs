using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Serilog;

namespace OpenGrid;

public static class AsyncHelper
{
    public static async void FireAndForgetSafe(this Task task, [CallerMemberName] string? callerName = null)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error in FireAndForgetSafe from {CallerName}", callerName ?? "unknown");
        }
    }
}