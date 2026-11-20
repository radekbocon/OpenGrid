namespace OpenGrid.Services.Telemetry;

public static class AcTracks
{
    private static readonly Dictionary<string, string> TrackNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Assetto Corsa
        ["barcelona"] = "Circuit de Barcelona-Catalunya",
        ["ks_barcelona"] = "Circuit de Barcelona-Catalunya",
        ["brands_hatch"] = "Brands Hatch",
        ["ks_brands_hatch"] = "Brands Hatch",
        ["donington"] = "Donington Park",
        ["ks_donington"] = "Donington Park",
        ["fuji"] = "Fuji Speedway",
        ["ks_fuji"] = "Fuji Speedway",
        ["hungaroring"] = "Hungaroring",
        ["ks_hungaroring"] = "Hungaroring",
        ["imola"] = "Autodromo Enzo e Dino Ferrari",
        ["ks_imola"] = "Autodromo Enzo e Dino Ferrari",
        ["laguna_seca"] = "Laguna Seca",
        ["ks_laguna_seca"] = "Laguna Seca",
        ["monza"] = "Autodromo Nazionale Monza",
        ["ks_monza"] = "Autodromo Nazionale Monza",
        ["mount_panorama"] = "Mount Panorama Circuit",
        ["ks_mount_panorama"] = "Mount Panorama Circuit",
        ["nurburgring"] = "Nürburgring Grand Prix",
        ["ks_nurburgring"] = "Nürburgring Grand Prix",
        ["nordschleife"] = "Nürburgring Nordschleife",
        ["ks_nordschleife"] = "Nürburgring Nordschleife",
        ["red_bull_ring"] = "Red Bull Ring",
        ["ks_red_bull_ring"] = "Red Bull Ring",
        ["silverstone"] = "Silverstone Circuit",
        ["ks_silverstone"] = "Silverstone Circuit",
        ["spa"] = "Circuit de Spa-Francorchamps",
        ["ks_spa"] = "Circuit de Spa-Francorchamps",
        ["suzuka"] = "Suzuka Circuit",
        ["ks_suzuka"] = "Suzuka Circuit",
        ["vallelunga"] = "Autodromo Vallelunga Piero Taruffi",
        ["ks_vallelunga"] = "Autodromo Vallelunga Piero Taruffi",
        ["zandvoort"] = "Circuit Zandvoort",
        ["ks_zandvoort"] = "Circuit Zandvoort",

        // Assetto Corsa Competizione
        ["bathurst"] = "Mount Panorama Circuit",
        ["misano"] = "Misano World Circuit Marco Simoncelli",
        ["paul_ricard"] = "Circuit Paul Ricard",
    };

    public static string GetName(string trackKey)
    {
        if (string.IsNullOrWhiteSpace(trackKey))
        {
            return trackKey;
        }

        return TrackNames.TryGetValue(trackKey, out var name) ? name : trackKey;
    }
}
