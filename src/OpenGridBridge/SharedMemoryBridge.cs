using System.IO.MemoryMappedFiles;

namespace OpenGridBridge;

/// <summary>
/// Bridge service for copying telemetry data from Windows shared memory buffers
/// to Linux /dev/shm/ files.
/// </summary>
public class SharedMemoryBridge : IDisposable
{
    private const int UpdateInterval = 1000 / 120;

    private readonly KeyValuePair<string, string>[] _buffers;
    private readonly List<OpenedBuffer> _openedBuffers = [];

    private bool _disposed;
    private bool _isConnected;

    /// <param name="buffers">
    /// Mapping of Windows shared memory buffer names to the Linux /dev/shm/ paths they should be copied to.
    /// </param>
    public SharedMemoryBridge(IReadOnlyDictionary<string, string> buffers)
    {
        ArgumentNullException.ThrowIfNull(buffers);

        _buffers = buffers.ToArray();
        Console.WriteLine($"Configured {_buffers.Length} shared memory buffers");
    }

    /// <summary>
    /// Start a continuous telemetry reading loop
    /// </summary>
    public async Task StartReadingAsync(CancellationToken cancellationToken = default)
    {
        while (!await InitializeAsync())
        {
            await Task.Delay(1000, cancellationToken);
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ReadAndWriteTelemetry();

                await Task.Delay(UpdateInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        finally
        {
            Dispose();
        }
    }

    private async Task<bool> InitializeAsync()
    {
        try
        {
            return await Task.Run(Initialize);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing: {ex.Message}");
            return false;
        }
    }

    private bool Initialize()
    {
        if (_buffers.Length == 0)
        {
            Console.WriteLine("No shared memory buffers configured");
            return false;
        }

        var openedBuffers = new List<OpenedBuffer>(_buffers.Length);

        try
        {
            foreach (var buffer in _buffers)
            {
                var file = MemoryMappedFile.OpenExisting(buffer.Key, MemoryMappedFileRights.Read);

                try
                {
                    var accessor = file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                    openedBuffers.Add(new OpenedBuffer(file, accessor));
                }
                catch
                {
                    file.Dispose();
                    throw;
                }
            }

            _openedBuffers.AddRange(openedBuffers);
            _isConnected = true;
            Console.WriteLine("Connected to game shared memory");
            return true;
        }
        catch (FileNotFoundException)
        {
            DisposeOpenedBuffers(openedBuffers);
            Console.WriteLine("Shared memory not found. Is the game running under Proton?");
            return false;
        }
        catch (Exception ex)
        {
            DisposeOpenedBuffers(openedBuffers);
            Console.WriteLine($"Connection failed: {ex.Message}");
            return false;
        }
    }

    private void ReadAndWriteTelemetry()
    {
        if (!_isConnected || _openedBuffers.Count != _buffers.Length)
            return;

        try
        {
            for (var i = 0; i < _buffers.Length; i++)
            {
                var bytes = ReadBytes(_openedBuffers[i].Accessor);
                File.WriteAllBytes(_buffers[i].Value, bytes);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading/writing telemetry: {ex.Message}");
        }
    }

    private static byte[] ReadBytes(MemoryMappedViewAccessor accessor)
    {
        var buffer = new byte[accessor.Capacity];
        accessor.ReadArray(0, buffer, 0, buffer.Length);
        return buffer;
    }

    public void Dispose()
    {
        if (_disposed) return;

        DisposeOpenedBuffers(_openedBuffers);

        foreach (var buffer in _buffers)
        {
            File.Delete(buffer.Value);
        }

        _isConnected = false;
        _disposed = true;
    }

    private static void DisposeOpenedBuffers(List<OpenedBuffer> buffers)
    {
        foreach (var buffer in buffers)
        {
            buffer.Accessor.Dispose();
            buffer.File.Dispose();
        }

        buffers.Clear();
    }

    private sealed record OpenedBuffer(MemoryMappedFile File, MemoryMappedViewAccessor Accessor);
}
