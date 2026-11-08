using OpenGridBridge;

const int AcEvoAppId = 3058630;

Console.WriteLine("Starting OpenGrid Bridge...");

if (!TryParseAppId(args, out var appId))
{
    Console.WriteLine("Usage: OpenGridBridge --appid <steam-app-id>");
    return 1;
}

Console.WriteLine(appId == AcEvoAppId
    ? "Using Assetto Corsa EVO shared memory layout"
    : $"Using default shared memory layout for app {appId}");

using var bridge = new SharedMemoryBridge(GetSharedMemoryBuffers(appId));
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await bridge.StartReadingAsync(cts.Token);

Console.WriteLine("Bridge stopped.");
return 0;

static IReadOnlyDictionary<string, string> GetSharedMemoryBuffers(int appId)
{
    if (appId == AcEvoAppId)
    {
        return new Dictionary<string, string>
        {
            ["Local\\acevo_pmf_physics"] = "/dev/shm/acevo_pmf_physics",
            ["Local\\acevo_pmf_graphics"] = "/dev/shm/acevo_pmf_graphics",
            ["Local\\acevo_pmf_static"] = "/dev/shm/acevo_pmf_static",
        };
    }

    return new Dictionary<string, string>
    {
        ["Local\\acpmf_physics"] = "/dev/shm/acpmf_physics",
        ["Local\\acpmf_graphics"] = "/dev/shm/acpmf_graphics",
        ["Local\\acpmf_static"] = "/dev/shm/acpmf_static",
    };
}

static bool TryParseAppId(string[] arguments, out int appId)
{
    appId = 0;

    for (var i = 0; i < arguments.Length; i++)
    {
        var argument = arguments[i];

        if (argument.Equals("--appid", StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 >= arguments.Length || !int.TryParse(arguments[i + 1], out appId))
            {
                Console.WriteLine("Invalid value for --appid.");
                return false;
            }

            return true;
        }

        if (argument.StartsWith("--appid=", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(argument["--appid=".Length..], out appId))
            {
                Console.WriteLine("Invalid value for --appid.");
                return false;
            }

            return true;
        }
    }

    Console.WriteLine("No --appid specified. Using the default Assetto Corsa shared memory layout.");
    return true;
}
