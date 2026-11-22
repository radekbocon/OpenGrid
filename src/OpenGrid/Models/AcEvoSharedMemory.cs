using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global

namespace OpenGrid.Models;

public enum AcEvoStatus
{
    Off = 0,
    Replay = 1,
    Live = 2,
    Pause = 3,
}

public enum AcEvoSessionType
{
    Unknown = -1,
    TimeAttack = 0,
    Race = 1,
    HotStint = 2,
    Cruise = 3,
}

public enum AcEvoFlagType
{
    NoFlag = 0,
    WhiteFlag = 1,
    GreenFlag = 2,
    RedFlag = 3,
    BlueFlag = 4,
    YellowFlag = 5,
    BlackFlag = 6,
    BlackWhiteFlag = 7,
    CheckeredFlag = 8,
    OrangeCircleFlag = 9,
    RedYellowStripesFlag = 10,
}

public enum AcEvoCarLocation
{
    Unassigned = 0,
    Pitlane = 1,
    PitEntry = 2,
    PitExit = 3,
    Track = 4,
}

public enum AcEvoEngineType
{
    InternalCombustion = 0,
    ElectricMotor = 1,
}

public enum AcEvoStartingGrip
{
    Green = 0,
    Fast = 1,
    Optimum = 2,
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct AcEvoCarId
{
    public ulong IdA;
    public ulong IdB;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SPageFilePhysicsEvo
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
    public float Tc;
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
    public float FinalFf;
    public float PerformanceMeter;

    public int EngineBrake;
    public int ErsRecoveryLevel;
    public int ErsPowerLevel;
    public int ErsHeatCharging;
    public int ErsIsCharging;
    public float KersCurrentKj;

    public int DrsAvailable;
    public int DrsEnabled;

    public AcTireStat BrakeTemp;
    public float Clutch;

    public AcTireStat TyreTempI;
    public AcTireStat TyreTempM;
    public AcTireStat TyreTempO;

    public int IsAiControlled;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public AcVector3[] TyreContactPoint;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public AcVector3[] TyreContactNormal;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public AcVector3[] TyreContactHeading;

    public float BrakeBias;

    public AcVector3 LocalVelocity;

    public int P2PActivations;
    public int P2PStatus;

    public int CurrentMaxRpm;

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
    public AcTireStat BrakeTorque;

    public int FrontBrakeCompound;
    public int RearBrakeCompound;
    public AcTireStat PadLife;
    public AcTireStat DiscLife;
    public int IgnitionOn;
    public int StarterEngineOn;
    public int IsEngineRunning;

    public float KerbVibration;
    public float SlipVibrations;
    public float RoadVibrations;
    public float AbsVibrations;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
public struct SmEvoTyreState
{
    public float Slip;

    [MarshalAs(UnmanagedType.I1)]
    public bool Lock;

    public float TyrePression;
    public float TyreTemperatureC;
    public float BrakeTemperatureC;
    public float BrakePressure;
    public float TyreTemperatureLeft;
    public float TyreTemperatureCenter;
    public float TyreTemperatureRight;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string TyreCompoundFront;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string TyreCompoundRear;

    public float TyreNormalizedPressure;
    public float TyreNormalizedTemperatureLeft;
    public float TyreNormalizedTemperatureCenter;
    public float TyreNormalizedTemperatureRight;
    public float BrakeNormalizedTemperature;
    public float TyreNormalizedTemperatureCore;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
    public byte[] PlaceHolder;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SmEvoDamageState
{
    public float DamageFront;
    public float DamageRear;
    public float DamageLeft;
    public float DamageRight;
    public float DamageCenter;
    public float DamageSuspensionLf;
    public float DamageSuspensionRf;
    public float DamageSuspensionLr;
    public float DamageSuspensionRr;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 92)]
    public byte[] PlaceHolder;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SmEvoPitInfo
{
    public sbyte Damage;
    public sbyte Fuel;
    public sbyte TyresLf;
    public sbyte TyresRf;
    public sbyte TyresLr;
    public sbyte TyresRr;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 58)]
    public byte[] PlaceHolder;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SmEvoElectronics
{
    public sbyte TcLevel;
    public sbyte TcCutLevel;
    public sbyte AbsLevel;
    public sbyte EscLevel;
    public sbyte EbbLevel;
    public float BrakeBias;
    public sbyte EngineMapLevel;
    public float TurboLevel;
    public sbyte ErsDeploymentMap;
    public float ErsRechargeMap;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsErsHeatChargingOn;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsErsOvertakeModeOn;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsDrsOpen;

    public sbyte DiffPowerLevel;
    public sbyte DiffCoastLevel;
    public sbyte FrontBumpDamperLevel;
    public sbyte FrontReboundDamperLevel;
    public sbyte RearBumpDamperLevel;
    public sbyte RearReboundDamperLevel;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsIgnitionOn;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsPitlimiterOn;

    public sbyte ActivePerformanceMode;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 88)]
    public byte[] PlaceHolder;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SmEvoInstrumentation
{
    public sbyte MainLightStage;
    public sbyte SpecialLightStage;
    public sbyte CockpitLightStage;
    public sbyte WiperLevel;

    [MarshalAs(UnmanagedType.I1)]
    public bool RainLights;

    [MarshalAs(UnmanagedType.I1)]
    public bool DirectionLightLeft;

    [MarshalAs(UnmanagedType.I1)]
    public bool DirectionLightRight;

    [MarshalAs(UnmanagedType.I1)]
    public bool FlashingLights;

    [MarshalAs(UnmanagedType.I1)]
    public bool WarningLights;

    public sbyte SelectedDisplayIndex;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public sbyte[] DisplayCurrentPageIndex;

    [MarshalAs(UnmanagedType.I1)]
    public bool AreHeadlightsVisible;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 101)]
    public byte[] PlaceHolder;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
public struct SmEvoSessionState
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string PhaseName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string TimeLeft;

    public int TimeLeftMs;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string WaitTime;

    public int TotalLap;
    public int CurrentLap;
    public int LightsOn;
    public int LightsMode;
    public float LapLengthKm;
    public int EndSessionFlag;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string TimeToNextSession;

    [MarshalAs(UnmanagedType.I1)]
    public bool DisconnectedFromServer;

    [MarshalAs(UnmanagedType.I1)]
    public bool RestartSeasonEnabled;

    [MarshalAs(UnmanagedType.I1)]
    public bool UiEnableDrive;

    [MarshalAs(UnmanagedType.I1)]
    public bool UiEnableSetup;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsReadyToNextBlinking;

    [MarshalAs(UnmanagedType.I1)]
    public bool ShowWaitingForPlayers;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 140)]
    public byte[] PlaceHolder;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
public struct SmEvoTimingState
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string CurrentLaptime;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string DeltaCurrent;

    public int DeltaCurrentP;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string LastLaptime;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string DeltaLast;

    public int DeltaLastP;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string BestLaptime;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string IdealLaptime;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string TotalTime;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsInvalid;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 137)]
    public byte[] PlaceHolder;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SmEvoAssistsState
{
    public byte AutoGear;
    public byte AutoBlip;
    public byte AutoClutch;
    public byte AutoClutchOnStart;
    public byte ManualIgnitionEStart;
    public byte AutoPitLimiter;
    public byte StandingStartAssist;

    public float AutoSteer;
    public float ArcadeStabilityControl;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
    public byte[] PlaceHolder;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
public struct SPageFileGraphicEvo
{
    public int PacketId;
    public AcEvoStatus Status;

    public ulong FocusedCarIdA;
    public ulong FocusedCarIdB;

    public ulong PlayerCarIdA;
    public ulong PlayerCarIdB;

    public ushort Rpm;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsRpmLimiterOn;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsChangeUpRpm;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsChangeDownRpm;

    [MarshalAs(UnmanagedType.I1)]
    public bool TcActive;

    [MarshalAs(UnmanagedType.I1)]
    public bool AbsActive;

    [MarshalAs(UnmanagedType.I1)]
    public bool EscActive;

    [MarshalAs(UnmanagedType.I1)]
    public bool LaunchActive;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsIgnitionOn;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsEngineRunning;

    [MarshalAs(UnmanagedType.I1)]
    public bool KersIsCharging;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsWrongWay;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsDrsAvailable;

    [MarshalAs(UnmanagedType.I1)]
    public bool BatteryIsCharging;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsMaxKjPerLapReached;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsMaxChargeKjPerLapReached;

    public short DisplaySpeedKmh;
    public short DisplaySpeedMph;
    public short DisplaySpeedMs;

    public float PitSpeedingDelta;
    public short GearInt;

    public float RpmPercent;
    public float GasPercent;
    public float BrakePercent;
    public float HandbrakePercent;
    public float ClutchPercent;
    public float SteeringPercent;

    public float FfbStrength;
    public float CarFfbMultiplier;

    public float WaterTemperaturePercent;

    public float WaterPressureBar;
    public float FuelPressureBar;

    public sbyte WaterTemperatureC;
    public sbyte AirTemperatureC;
    public float OilTemperatureC;
    public float OilPressureBar;
    public float ExhaustTemperatureC;

    public float GForcesX;
    public float GForcesY;
    public float GForcesZ;

    public float TurboBoost;
    public float TurboBoostLevel;
    public float TurboBoostPerc;

    public int SteerDegrees;
    public float CurrentKm;
    public uint TotalKm;
    public uint TotalDrivingTimeS;

    public int TimeOfDayHours;
    public int TimeOfDayMinutes;
    public int TimeOfDaySeconds;

    public int DeltaTimeMs;
    public int CurrentLapTimeMs;
    public int PredictedLapTimeMs;

    public float FuelLiterCurrentQuantity;
    public float FuelLiterCurrentQuantityPercent;
    public float FuelLiterPerKm;
    public float KmPerFuelLiter;

    public float CurrentTorque;
    public int CurrentBhp;

    public SmEvoTyreState TyreLf;
    public SmEvoTyreState TyreRf;
    public SmEvoTyreState TyreLr;
    public SmEvoTyreState TyreRr;

    public float Npos;

    public float KersChargePerc;
    public float KersCurrentPerc;

    public float ControlLockTime;

    public SmEvoDamageState CarDamage;

    public AcEvoCarLocation CarLocation;

    public SmEvoPitInfo PitInfo;

    public float FuelLiterUsed;
    public float FuelLiterPerLap;
    public float LapsPossibleWithFuel;

    public float BatteryTemperature;
    public float BatteryVoltage;

    public float InstantaneousFuelLiterPerKm;
    public float InstantaneousKmPerFuelLiter;

    public float GearRpmWindow;

    public SmEvoInstrumentation Instrumentation;
    public SmEvoInstrumentation InstrumentationMinLimit;
    public SmEvoInstrumentation InstrumentationMaxLimit;

    public SmEvoElectronics Electronics;
    public SmEvoElectronics ElectronicsMinLimit;
    public SmEvoElectronics ElectronicsMaxLimit;
    public SmEvoElectronics ElectronicsIsModifiable;

    public int TotalLapCount;
    public uint CurrentPos;
    public uint TotalDrivers;

    public int LastLaptimeMs;
    public int BestLaptimeMs;

    public AcEvoFlagType Flag;
    public AcEvoFlagType GlobalFlag;

    public uint MaxGears;
    public AcEvoEngineType EngineType;

    [MarshalAs(UnmanagedType.I1)]
    public bool HasKers;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsLastLap;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string PerformanceModeName;

    public float DiffCoastRawValue;
    public float DiffPowerRawValue;

    public int RaceCutGainedTimeMs;
    public int DistanceToDeadline;
    public float RaceCutCurrentDelta;

    public SmEvoSessionState SessionState;
    public SmEvoTimingState TimingState;

    public int PlayerPing;
    public int PlayerLatency;
    public int PlayerCpuUsage;
    public int PlayerCpuUsageAvg;
    public int PlayerQos;
    public int PlayerQosAvg;
    public int PlayerFps;
    public int PlayerFpsAvg;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string DriverName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string DriverSurname;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string CarModel;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsInPitBox;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsInPitLane;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsValidLap;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
    public AcVector3[] CarCoordinates;

    public float GapAhead;
    public float GapBehind;

    public byte ActiveCars;

    public float FuelPerLap;
    public float FuelEstimatedLaps;

    public SmEvoAssistsState AssistsState;

    public float MaxFuel;
    public float MaxTurboBoost;

    [MarshalAs(UnmanagedType.I1)]
    public bool UseSingleCompound;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
    public AcEvoCarId[] CarIds;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
public struct SPageFileStaticEvo
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string SmVersion;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string AcEvoVersion;

    public AcEvoSessionType Session;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string SessionName;

    public byte EventId;
    public byte SessionId;

    public AcEvoStartingGrip StartingGrip;

    public float StartingAmbientTemperatureC;
    public float StartingGroundTemperatureC;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsStaticWeather;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsTimedRace;

    [MarshalAs(UnmanagedType.I1)]
    public bool IsOnline;

    public int NumberOfSessions;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string Nation;

    public float Longitude;
    public float Latitude;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string Track;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string TrackConfiguration;

    public float TrackLengthM;
}
