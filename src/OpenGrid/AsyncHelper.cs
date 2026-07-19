using System.Runtime.CompilerServices;
using Serilog;

namespace OpenGrid;

public static class AsyncHelper
{
    public static async void FireAndForgetSafe(this Task task, [CallerMemberName] string? callerName = null)
    {
        try
        {
            if (!task.IsCompleted || task.IsFaulted)
            {
                await task.ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error in FireAndForgetSafe from {CallerName}", callerName ?? "unknown");
        }
    }
}