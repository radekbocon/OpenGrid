using System.Threading;
using SimLab;
using SimLabBridge;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
Console.WriteLine("Starting SimLab Bridge...");

using var bridge = new AccSharedMemoryBridge();
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await bridge.StartReadingAsync(cts.Token);

Console.WriteLine("Bridge stopped.");
