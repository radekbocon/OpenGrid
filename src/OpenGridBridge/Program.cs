using OpenGridBridge;

Console.WriteLine("Starting OpenGrid Bridge...");

using var bridge = new AccSharedMemoryBridge();
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await bridge.StartReadingAsync(cts.Token);

Console.WriteLine("Bridge stopped.");
