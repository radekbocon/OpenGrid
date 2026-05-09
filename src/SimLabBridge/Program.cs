using SimLabBridge;

Console.WriteLine("Starting SimLab Bridge...");

using var bridge = new AccSharedMemoryBridge();
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await bridge.StartReadingAsync(cts.Token);

Console.WriteLine("Bridge stopped.");
