using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace SimLab.Models;

/// <summary>
/// Assetto Corsa shared memory structures
/// These structs match the binary layout of Assetto Corsa's physics and graphics buffers
/// </summary>

public enum AC_STATUS
{
    AC_OFF = 0,
    AC_REPLAY = 1,
    AC_LIVE = 2,
    AC_PAUSE = 3
}

public enum AC_SESSION_TYPE
{
    AC_UNKNOWN = -1,
    AC_PRACTICE = 0,
    AC_QUALIFY = 1,
    AC_RACE = 2,
    AC_HOTLAP = 3,
    AC_TIME_ATTACK = 4,
    AC_DRIFT = 5,
    AC_DRAG = 6
}

public enum ACC_FLAG_TYPE
{
    ACC_NO_FLAG = 0,
    ACC_BLUE_FLAG = 1,
    ACC_YELLOW_FLAG = 2,
    ACC_BLACK_FLAG = 3,
    ACC_WHITE_FLAG = 4,
    ACC_CHECKERED_FLAG = 5,
    ACC_PENALTY_FLAG = 6,
    ACC_GREEN_FLAG = 7,
    ACC_ORANGE_FLAG = 8
}

public enum ACC_PENALTY_TYPE
{
    ACC_NONE = 0,
    ACC_DRIVETHROUGH_CUTTING = 1,
    ACC_STOPANDGO_10_CUTTING = 2,
    ACC_STOPANDGO_20_CUTTING = 3,
    ACC_STOPANDGO_30_CUTTING = 4,
    ACC_DISQUALIFIED_CUTTING = 5,
    ACC_REMOVEBESTLAPTIME_CUTTING = 6,
    ACC_DRIVETHROUGH_PITSPEEDING = 7,
    ACC_STOPANDGO_10_PITSPEEDING = 8,
    ACC_STOPANDGO_20_PITSPEEDING = 9,
    ACC_STOPANDGO_30_PITSPEEDING = 10,
    ACC_DISQUALIFIED_PITSPEEDING = 11,
    ACC_REMOVEBESTLAPTIME_PITSPEEDING = 12,
    ACC_DISQUALIFIED_IGNOREDMANDATORYPIT = 13,
    ACC_POSTRACETIME = 14,
    ACC_DISQUALIFIED_TROLLING = 15,
    ACC_DISQUALIFIED_PITENTRY = 16,
    ACC_DISQUALIFIED_PITEXIT = 17,
    ACC_DISQUALIFIED_WRONGWAY = 18,
    ACC_DRIVETHROUGH_IGNOREDDRIVERSTINT = 19,
    ACC_DISQUALIFIED_IGNOREDDRIVERSTINT = 20,
    ACC_DISQUALIFIED_EXCEEDEDDRIVERSTINTLIMIT = 21
}

public enum ACC_TRACK_GRIP_STATUS
{
    ACC_GREEN = 0,
    ACC_FAST = 1,
    ACC_OPTIMUM = 2,
    ACC_GREASY = 3,
    ACC_DAMP = 4,
    ACC_WET = 5,
    ACC_FLOODED = 6
}

public enum ACC_RAIN_INTENSITY
{
    ACC_NO_RAIN = 0,
    ACC_DRIZZLE = 1,
    ACC_LIGHT_RAIN = 2,
    ACC_MEDIUM_RAIN = 3,
    ACC_HEAVY_RAIN = 4,
    ACC_THUNDERSTORM = 5
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct acsVec3
{
    public float x;
    public float y;
    public float z;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
public struct acsVehicleInfo
{
    public int carId;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string driverName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string carModel;
    public float speedMS;
    public int bestLapMS;
    public int lapCount;
    public int currentLapInvalid;
    public int currentLapTimeMS;
    public int lastLapTimeMS;
    public acsVec3 worldPosition;
    public int isCarInPitline;
    public int isCarInPit;
    public int carLeaderboardPosition;
    public int carRealTimeLeaderboardPosition;
    public float spLineLength;
    public int isConnected;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] suspensionDamage;
    public float engineLifeLeft;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] tyreInflation;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SPageFilePhysics
{
    public int packetId;
    public float gas;
    public float brake;
    public float fuel;
    public int gear;
    public int rpms;
    public float steerAngle;
    public float speedKmh;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] velocity;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] accG;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] wheelSlip;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] wheelLoad;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] wheelsPressure;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] wheelAngularSpeed;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] tyreWear;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] tyreDirtyLevel;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] tyreCoreTemperature;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] camberRAD;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] suspensionTravel;
    public float drs;
    public float tc;
    public float heading;
    public float pitch;
    public float roll;
    public float cgHeight;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    public float[] carDamage;
    public int numberOfTyresOut;
    public int pitLimiterOn;
    public float abs;
    public float kersCharge;
    public float kersInput;
    public int autoShifterOn;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public float[] rideHeight;
    public float turboBoost;
    public float ballast;
    public float airDensity;
    public float airTemp;
    public float roadTemp;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] localAngularVel;
    public float finalFF;
    public float performanceMeter;
    public int engineBrake;
    public int ersRecoveryLevel;
    public int ersPowerLevel;
    public int ersHeatCharging;
    public int ersIsCharging;
    public float kersCurrentKJ;
    public int drsAvailable;
    public int drsEnabled;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] brakeTemp;
    public float clutch;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] tyreTempI;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] tyreTempM;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] tyreTempO;
    public int isAIControlled;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public acsVec3[] tyreContactPoint;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public acsVec3[] tyreContactNormal;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public acsVec3[] tyreContactHeading;
    public float brakeBias;
    public acsVec3 localVelocity;
    public int P2PActivation; //Not used in ACC
    public int P2PStatus; //Not used in ACC
    public float CurrentMaxRPM; //Not used in ACC
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] MZ;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] FX;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] FY;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] SlipRatio;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] SlipAngle;
    public int TCInAction; //Not used in ACC
    public int ABSInAction; //Not used in ACC
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] SuspensionDamage;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] TyreTemp;
    public float WaterTemp;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] BrakePressure;
    public int frontBrakeCompound;
    public int rearBrakeCompound;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] padLife;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] discLife;
    public int ignitionOn;
    public int starterEngineOn;
    public int isEngineRunning;
    public float kerbVibration;
    public float slipVibration;
    public float gVibration;
    public float absbVibration;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
public struct SPageFileGraphic
{
    public int packetId;
    public AC_STATUS status;
    public AC_SESSION_TYPE session;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string currentTime;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string lastTime;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string bestTime;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string split;
    public int completedLaps;
    public int position;
    public int iCurrentTime;
    public int iLastTime;
    public int iBestTime;
    public float sessionTimeLeft;
    public float distanceTraveled;
    public int isInPit;
    public int currentSectorIndex;
    public int lastSectorTime;
    public int numberOfLaps;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string tyreCompound;
    public float replayTimeMultiplier;
    public float normalizedCarPosition;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] carCoordinates;
    public float PenaltyTime;
    public ACC_FLAG_TYPE Flag;
    public ACC_PENALTY_TYPE Penalty;
    public int IdealLineOn;
    public int IsInPitLane;
    public float SurfaceGrip;
    public int MandatoryPitDone;
    public float WindSpeed;
    public float WindDirection;
    public int IsSetupMenuVisible;
    public int MainDisplayIndex;
    public int SecondaryDisplayIndex;
    public int TC;
    public int TCUT;
    public int EngineMap;
    public int ABS;
    public float FuelXLap;
    public int RainLights;
    public int FlashingLights;
    public int LightsStage;
    public float ExhaustTemperature;
    public int WiperLV;
    public int DriverStingTotalTimeLeft;
    public int DriverStingTimeLeft;
    public int RainTyres;
    public int SessionIndex;
    public float UsedFuel; //Since last refuel
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string DeltaLapTime;
    public int IDeltaLapTime;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string EstimatedLapTime;
    public int IEstimatedLapTime;
    public int IsDeltaPositive;
    public int ISplit; //Last split time in ms
    public int IsValidLap;
    public float FuelEstimatedLaps;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string TrackStatus;
    public int MissingMandatoryPits;
    public int directionLightsLeft;
    public int directionLightsRight;
    public int GlobalYellow;
    public int GlobalYellow1;
    public int GlobalYellow2;
    public int GlobalYellow3;
    public int GlobalWhite;
    public int GlobalGreen;
    public int GlobalChequered;
    public int GlobalRed;
    public int mfdTyreSet;
    public float mfdFuelToAdd;
    public float mfdTyrePressureLF;
    public float mfdTyrePressureRF;
    public float mfdTyrePressureLR;
    public float mfdTyrePressureRR;
    public ACC_TRACK_GRIP_STATUS trackGripStatus;
    public ACC_RAIN_INTENSITY rainIntensity;
    public ACC_RAIN_INTENSITY rainIntensityIn10min;
    public ACC_RAIN_INTENSITY rainIntensityIn30min;
    public int currentTyreSet;
    public int strategyTyreSet;
    public int gapAhead;
    public int gapBehind;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
public struct SPageFileStatic
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string smVersion;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string acVersion;
    // session static info
    public int numberOfSessions;
    public int numCars;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string carModel;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string track;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string playerName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string playerSurname;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string playerNick;
    public int sectorCount;
    // car static info
    public float maxTorque;
    public float maxPower;
    public int maxRpm;
    public float maxFuel;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] suspensionMaxTravel;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] tyreRadius;
    // since 1.5
    public float MaxTurboBoost;
    public float Deprecated1; // AirTemp since 1.6 in physic
    public float Deprecated2; // RoadTemp since 1.6 in physic
    public int PenaltiesEnabled;
    public float AidFuelRate;
    public float AidTireRate;
    public float AidMechanicalDamage;
    public int AidAllowTyreBlankets;
    public float AidStability;
    public int AidAutoClutch;
    public int AidAutoBlip;
    // since 1.7.1
    public int HasDRS;
    public int HasERS;
    public int HasKERS;
    public float KersMaxJoules;
    public int EngineBrakeSettingsCount;
    public int ErsPowerControllerCount;
    // since 1.7.2
    public float TrackSPlineLength;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string TrackConfiguration;
    // since 1.10.2
    public float ErsMaxJ;
    // since 1.13
    public int IsTimedRace;
    public int HasExtraLap;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string CarSkin;
    public int ReversedGridPositions;
    public int PitWindowStart;
    public int PitWindowEnd;
    public int IsOnline;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string dryTyresName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string wetTyresName;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
public struct SPageFileCrewChief
{
    public int numVehicles;
    public int focuseVehicle;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
    public string serverName;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public acsVehicleInfo[] vehicle;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
    public string acInstallPath;
    public int isInternalMemoryModuleLoaded;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string pluginVersion;
}
