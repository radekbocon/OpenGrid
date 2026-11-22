using System.Runtime.InteropServices;
using Serilog;

namespace OpenGrid.Services.Telemetry;

public static class ShmHelper
{
    public static T? ReadStructFromFile<T>(string filePath) where T : struct
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var size = Marshal.SizeOf<T>();
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var bytes = new byte[size];
            var read = fs.Read(bytes, 0, size);
            return read < size ? null : BytesToStruct<T>(bytes, size);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reading struct from file: {Message}", ex.Message);
            return null;
        }
    }
    
    public static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not delete file: {Path}", path);
        }
    }
    
    private static T? BytesToStruct<T>(byte[] data, int size) where T : struct
    {
        try
        {
            if (data.Length < size)
            {
                return null;
            }

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var ptr = handle.AddrOfPinnedObject();
                var result = Marshal.PtrToStructure<T>(ptr);
                return result;
            }
            finally
            {
                handle.Free();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error converting bytes to struct: {Message}", ex.Message);
            return null;
        }
    }
}