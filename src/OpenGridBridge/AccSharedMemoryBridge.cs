using System.IO.MemoryMappedFiles;

namespace OpenGridBridge;

/// <summary>
/// Bridge service for reading ACC telemetry data from shared memory
/// Handles Windows/Proton compatibility by mapping shared memory to Linux /dev/shm/ files
/// </summary>
public class AccSharedMemoryBridge : IDisposable
{
    private const string PhysicsBufferName = "Local\\acpmf_physics";
    private const string GraphicsBufferName = "Local\\acpmf_graphics";
    private const string StaticBufferName = "Local\\acpmf_static";

    private const string ShmPhysicsPath = "/dev/shm/acpmf_physics";
    private const string ShmGraphicsPath = "/dev/shm/acpmf_graphics";
    private const string ShmStaticPath = "/dev/shm/acpmf_static";
    
    private const int UpdateInterval = 1000 / 120;

    private MemoryMappedFile? _physicsFile;
    private MemoryMappedFile? _graphicsFile;
    private MemoryMappedFile? _staticFile;

    private MemoryMappedViewAccessor? _physicsAccessor;
    private MemoryMappedViewAccessor? _graphicsAccessor;
    private MemoryMappedViewAccessor? _staticAccessor;

    private bool _disposed;
    private bool _isConnected;
    
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
        try
        {
            // Try to open existing memory mapped files from ACC
            _physicsFile = MemoryMappedFile.OpenExisting(PhysicsBufferName, MemoryMappedFileRights.Read);
            _graphicsFile = MemoryMappedFile.OpenExisting(GraphicsBufferName, MemoryMappedFileRights.Read);
            _staticFile = MemoryMappedFile.OpenExisting(StaticBufferName, MemoryMappedFileRights.Read);

            _physicsAccessor = _physicsFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            _graphicsAccessor = _graphicsFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            _staticAccessor = _staticFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

            _isConnected = true;
            Console.WriteLine("Connected to ACC");
            return true;
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("ACC shared memory not found. Is ACC running under Proton?");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
            return false;
        }
    }
    
    private void ReadAndWriteTelemetry()
    {
        if (!_isConnected || _physicsAccessor == null || _graphicsAccessor == null || _staticAccessor == null)
            return;

        try
        {
            var physicsBytes = ReadBytes(_physicsAccessor);
            var graphicsBytes = ReadBytes(_graphicsAccessor);
            var staticBytes = ReadBytes(_staticAccessor);

            WriteToSharedFiles(physicsBytes, graphicsBytes, staticBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading/writing telemetry: {ex.Message}");
        }
    }
    
    private static void WriteToSharedFiles(byte[] physics, byte[] graphics, byte[] staticData)
    {
        try
        {
            File.WriteAllBytes(ShmPhysicsPath, physics);
            File.WriteAllBytes(ShmGraphicsPath, graphics);
            File.WriteAllBytes(ShmStaticPath, staticData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to shared files: {ex.Message}");
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

        _physicsAccessor?.Dispose();
        _graphicsAccessor?.Dispose();
        _staticAccessor?.Dispose();

        File.Delete(ShmPhysicsPath);
        File.Delete(ShmGraphicsPath);
        File.Delete(ShmStaticPath);
        
        _physicsFile?.Dispose();
        _graphicsFile?.Dispose();
        _staticFile?.Dispose();

        _isConnected = false;
        _disposed = true;
    }
}
