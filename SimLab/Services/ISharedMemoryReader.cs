using System;
using SimLab.Models;

namespace SimLab.Services;

public interface ISharedMemoryReader
{
    TelemetrySnapshot? ReadTelemetryData();
}