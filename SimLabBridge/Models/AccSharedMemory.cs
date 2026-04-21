using System;
using System.Runtime.InteropServices;

namespace SimLab.Models;

/// <summary>
/// ACC (Assetto Corsa Competizione) shared memory structures
/// These structs match the binary layout of ACC's physics and graphics buffers
/// </summary>

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SPageFilePhysics
{
    public int PacketId;
    public float Gas;
    public float Brake;
    public float Clutch;
    public float Rps;
    public float MaxRps;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] RpsPerGear;
    public float SpeedMS;
    public int Gear;
    public int Status; // 0 = OFF, 1 = REPLAY, 2 = LIVE, 3 = PAUSE
    public int OrientationX; // as int instead of float
    public int OrientationY;
    public int OrientationZ;
    public int LocalVelocityX; // as int instead of float
    public int LocalVelocityY;
    public int LocalVelocityZ;
    public int WorldVelocityX; // as int instead of float
    public int WorldVelocityY;
    public int WorldVelocityZ;
    public float AngularVelocityX;
    public float AngularVelocityY;
    public float AngularVelocityZ;
    public float AngularAccelerationX;
    public float AngularAccelerationY;
    public float AngularAccelerationZ;
    public float LocalAccelerationX;
    public float LocalAccelerationY;
    public float LocalAccelerationZ;
    public int PositionX; // as int instead of float
    public int PositionY;
    public int PositionZ;
    public float VelocityX;
    public float VelocityY;
    public float VelocityZ;
    public int Ax;
    public int Ay;
    public int Az;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] SuspensionTravel;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] TireSlip;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] TireContactPatch;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] TireHeadingStatus;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] TireLateralForce;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] TireLongitudinalForce;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] BrakePressure;
    public float EngineBrake;
    public float EngineMap;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public int[] FuelPercentage;
    public float FuelCapacityLiters;
    public float FuelUreaMix;
    public float PadLife;
    public float EngineLife;
    public int SliProNormalizedTireConditionStatus;
    public int SliProTireTemperatureStatus;
    public int SliProWheelsOutCount;
    public int SliProTimedOutCount;
    public int CutTrackedByRaceControlUnit;
    public int AirDensity;
    public float AirTemperature;
    public float RoadTemperature;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] TireTemp;
    public float RainIntensity;
    public float RainLights;
    public float RainTemperature;
    public int RainType;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
public struct SPageFileGraphics
{
    public int PacketId;
    public int Status; // 0 = OFF, 1 = REPLAY, 2 = LIVE, 3 = PAUSE
    public int Session; // 0 = PRACTICE, 1 = QUALIFY, 2 = RACE, 3 = HOTLAP, 4 = TIMEATTACK, 5 = DRIFT, 6 = DRAG, 7 = FORMATIONLAP
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string CurrentTime;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string LastTime;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string BestTime;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string Split;
    public int CompletedLaps;
    public int Position;
    public int ICurrentTimeValid;
    public int ILapInvalidated;
    public int ILastTimeValid;
    public int IBestTimeValid;
    public int ISessionCurrentLapInvalid;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string MSetupFileName;
    public float AirDensity;
    public float AirTemp;
    public float RoadTemp;
    public int IsSetupMenuVisible;
    public int MainDisplayIndex;
    public int SecondaryDisplayIndex;
    public int TC;
    public int TCCut;
    public int EngineMap;
    public int ABS;
    public float FuelUreaMix;
    public int DrsEnabled;
    public int PitLimiterOn;
    public float FuelEstimatedLaps;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string DrsAvailable;
    public int IsOnTrack;
    public int IsInPit;
    public int CurrentTyreSet;
    public int StrategyTyreSet;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string NumCarsGT;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string NumCarsGT3;
    public int GapAhead;
    public int GapBehind;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
public struct SPageFileStatic
{
    public int SmVersion;
    public int AcVersion;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string TrackName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string CarModel;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string PlayerName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string PlayerNickname;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string PlayerSurname;
    public int AILevel;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string SessionType;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string CarSkin;
    public int CompletedLaps;
    public int DNFed;
    public int IsMultiplayer;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string CarSetupFileName;
    public int ReplayFromSetup;
    public int Penalties;
    public int IsInRaceWeekend;
}
