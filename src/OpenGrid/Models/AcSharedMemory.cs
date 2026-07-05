using System.Numerics;
using System.Runtime.InteropServices;
using OpenGrid.Models.Telemetry;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace OpenGrid.Models;

/// <summary>
/// Assetto Corsa shared memory structures
/// These structs match the binary layout of Assetto Corsa's physics and graphics buffers
/// </summary>
public enum TrackGripStatus
{
    GREEN = 0,
    FAST = 1,
    OPTIMUM = 2,
    GREASY = 3,
    DAMP = 4,
    WET = 5,
    FLOODED = 6
}

public enum RainIntensity
{
    NO_RAIN = 0,
    DRIZZLE = 1,
    LIGHT_RAIN = 2,
    MEDIUM_RAIN = 3,
    HEAVY_RAIN = 4,
    THUNDERSTORM = 5
}

public enum PenaltyType
{
    None = 0,
    DriveThrough_Cutting = 1,
    StopAndGo_10_Cutting = 2,
    StopAndGo_20_Cutting = 3,
    StopAndGo_30_Cutting = 4,
    Disqualified_Cutting = 5,
    RemoveBestLaptime_Cutting = 6,
    DriveThrough_PitSpeeding = 7,
    StopAndGo_10_PitSpeeding = 8,
    StopAndGo_20_PitSpeeding = 9,
    StopAndGo_30_PitSpeeding = 10,
    Disqualified_PitSpeeding = 11,
    RemoveBestLaptime_PitSpeeding = 12,
    Disqualified_IgnoredMandatoryPit = 13,
    PostRaceTime = 14,
    Disqualified_Trolling = 15,
    Disqualified_PitEntry = 16,
    Disqualified_PitExit = 17,
    Disqualified_Wrongway = 18,
    DriveThrough_IgnoredDriverStint = 19,
    Disqualified_IgnoredDriverStint = 20,
    Disqualified_ExceededDriverStintLimit = 21
}

public enum FlagType
{
    NO_FLAG = 0,
    BLUE_FLAG = 1,
    YELLOW_FLAG = 2,
    BLACK_FLAG = 3,
    WHITE_FLAG = 4,
    CHECKERED_FLAG = 5,
    PENALTY_FLAG = 6
}

public enum GameStatus
{
    OFF = 0,
    REPLAY = 1,
    LIVE = 2,
    PAUSE = 3
}

[StructLayout(LayoutKind.Sequential)]
[Serializable]
public struct AcVector3
{
    public float X;
    public float Y;
    public float Z;

    public static implicit operator AcVector3(Vector3 vector3) =>
        new() { X = vector3.X, Y = vector3.Y, Z = vector3.Z };
    public static implicit operator Vector3(AcVector3 acVector3) => 
        new() { X = acVector3.X, Y = acVector3.Y, Z = acVector3.Z };
}

[StructLayout(LayoutKind.Sequential)]
[Serializable]
public struct AcTireStat
{
    public float FrontLeft;
    public float FrontRight;
    public float RearLeft;
    public float RearRight;
    
    public static implicit operator TireStats(AcTireStat value) =>
        new() { FrontLeft = value.FrontLeft, FrontRight = value.FrontRight, RearLeft = value.RearLeft, RearRight = value.RearRight };
    public static implicit operator AcTireStat(TireStats value) => 
        new() { FrontLeft = value.FrontLeft, FrontRight = value.FrontRight, RearLeft = value.RearLeft, RearRight = value.RearRight };
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SPageFilePhysics
{
    public int PacketId;
    public float Gas;
    public float Brake;
    public float Fuel;
    public int Gear;
    public int Rpms;
    public float SteerAngle;
    public float SpeedKmh;
    public AcVector3 Velocity;
    public AcVector3 AccG;
    public AcTireStat WheelSlip;
    public AcTireStat WheelLoad;
    public AcTireStat WheelsPressure;
    public AcTireStat WheelAngularSpeed;
    public AcTireStat TyreWear;
    public AcTireStat TyreDirtyLevel;
    public AcTireStat TyreCoreTemperature;
    public AcTireStat CamberRad;
    public AcTireStat SuspensionTravel;
    public float Drs;
    public float TC;
    public float Heading;
    public float Pitch;
    public float Roll;
    public float CgHeight;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    public float[] CarDamage;

    public int NumberOfTyresOut;
    public int PitLimiterOn;
    public float Abs;
    public float KersCharge;
    public float KersInput;
    public int AutoShifterOn;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public float[] RideHeight;

    public float TurboBoost;
    public float Ballast;
    public float AirDensity;
    public float AirTemp;
    public float RoadTemp;
    public AcVector3 LocalAngularVelocity;
    public float FinalFF;
    public float PerformanceMeter;
    public int EngineBrake;
    public int ErsRecoveryLevel;
    public int ErsPowerLevel;
    public int ErsHeatCharging;
    public int ErsisCharging;
    public float KersCurrentKJ;
    public int DrsAvailable;
    public int DrsEnabled;
    public AcTireStat BrakeTemp;
    public float Clutch;
    public AcTireStat TyreTempI;
    public AcTireStat TyreTempM;
    public AcTireStat TyreTempO;
    public int IsAIControlled;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public AcVector3[] TyreContactPoint;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public AcVector3[] TyreContactNormal;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public AcVector3[] TyreContactHeading;

    public float BrakeBias;
    public AcVector3 LocalVelocity;
    public int P2PActivation;
    public int P2PStatus;
    public float CurrentMaxRpm;
    public AcTireStat Mz;
    public AcTireStat Fx;
    public AcTireStat Fy;
    public AcTireStat SlipRatio;
    public AcTireStat SlipAngle;
    public int TcinAction;
    public int AbsInAction;
    public AcTireStat SuspensionDamage;
    public AcTireStat TyreTemp;
    public float WaterTemp;
    public AcTireStat BrakePressure;
    public int FrontBrakeCompound;
    public int RearBrakeCompound;
    public AcTireStat PadLife;
    public AcTireStat DiscLife;
    public int IgnitionOn;
    public int StarterEngineOn;
    public int IsEngineRunning;
    public float KerbVibration;
    public float SlipVibrations;
    public float GVibrations;
    public float AbsVibrations;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
public struct SPageFileGraphic
{
    public int PacketId;
    public GameStatus Status;
    public SessionType Session;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string CurrentTimeString;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string LastTimeString;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string BestTimeString;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string SplitString;

    public int CompletedLaps;
    public int Position;
    public int CurrentTime;
    public int LastTime;
    public int BestTime;
    public float SessionTimeLeft;
    public float DistanceTraveled;
    public int IsInPit;
    public int CurrentSectorIndex;
    public int LastSectorTime;
    public int NumberOfLaps;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string TyreCompound;

    public float ReplayTimeMultiplier;
    public float NormalizedCarPosition;
    public int ActiveCars;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
    public AcVector3[] CarCoordinates;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
    public int[] CarIDs;

    public int PlayerCarID;
    public float PenaltyTime;
    public FlagType Flag;
    public PenaltyType Penalty;
    public int IdealLineOn;
    public int IsInPitLane;
    public float SurfaceGrip;
    public int MandatoryPitDone;
    public float WindSpeed;
    public float WindDirection;
    public int IsSetupMenuVisible;
    public int MainDisplayIndex;
    public int SecondaryDisplyIndex;
    public int TC;
    public int TCCUT;
    public int EngineMap;
    public int ABS;
    public float FuelXLap;
    public int RainLights;
    public int FlashingLights;
    public int LightsStage;
    public float ExhaustTemperature;
    public int WiperLV;
    public int DriverStintTotalTimeLeft;
    public int DriverStintTimeLeft;
    public int RainTyres;
    public int SessionIndex;
    public float UsedFuel;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string DeltaLapTimeString;

    public int DeltaLapTime;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string EstimatedLapTimeString;

    public int EstimatedLapTime;
    public int IsDeltaPositive;
    public int Split;
    public int IsValidLap;
    public float FuelEstimatedLaps;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string TrackStatus;

    public int MissingMandatoryPits;
    public float Clock;
    public int DirectionLightsLeft;
    public int DirectionLightsRight;
    public int GlobalYellow;
    public int GlobalYellow1;
    public int GlobalYellow2;
    public int GlobalYellow3;
    public int GlobalWhite;
    public int GlobalGreen;
    public int GlobalChequered;
    public int GlobalRed;
    public int MfdTyreSet;
    public float MfdFuelToAdd;
    public float MfdTyrePressureLF;
    public float MfdTyrePressureRF;
    public float MfdTyrePressureLR;
    public float MfdTyrePressureRR;
    public TrackGripStatus TrackGripStatus;
    public RainIntensity RainIntensity;
    public RainIntensity RainIntensityIn10min;
    public RainIntensity RainIntensityIn30min;
    public int CurrentTyreSet;
    public int StrategyTyreSet;
    public int GapAhead;
    public int GapBehind;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
[Serializable]
public struct SPageFileStatic
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string SMVersion;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string ACVersion;
    public int NumberOfSessions;
    public int NumCars;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string CarModel;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string Track;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string PlayerName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string PlayerSurname;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string PlayerNick;
    public int SectorCount;
    public float MaxTorque;
    public float MaxPower;
    public int MaxRpm;
    public float MaxFuel;
    public AcTireStat SuspensionMaxTravel;
    public AcTireStat TyreRadius;
    public float MaxTurboBoost;
    public float Deprecated1;
    public float Deprecated2;
    public int PenaltiesEnabled;
    public float AidFuelRate;
    public float AidTireRate;
    public float AidMechanicalDamage;
    public int AidAllowTyreBlankets;
    public float AidStability;
    public int AidAutoClutch;
    public int AidAutoBlip;
    public int HasDRS;
    public int HasERS;
    public int HasKERS;
    public float KersMaxJoules;
    public int EngineBrakeSettingsCount;
    public int ErsPowerControllerCount;
    public float TrackSPlineLength;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string TrackConfiguration;
    public float ErsMaxJ;
    public int IsTimedRace;
    public int HasExtraLap;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string CarSkin;
    public int ReversedGridPositions;
    public int PitWindowStart;
    public int PitWindowEnd;
    public int IsOnline;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string DryTyresName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string WetTyresName;
}