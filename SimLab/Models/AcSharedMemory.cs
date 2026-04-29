using System;
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

public enum AC_FLAG_TYPE
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

[StructLayout (LayoutKind.Sequential)]
public struct Coordinates
{
    public float X;
    public float Y;
    public float Z;
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

        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] Velocity;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] AccG;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] WheelSlip;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] WheelLoad;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] WheelsPressure;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] WheelAngularSpeed;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreWear;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreDirtyLevel;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreCoreTemperature;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] CamberRad;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] SuspensionTravel;

        public float Drs;
        public float TC;
        public float Heading;
        public float Pitch;
        public float Roll;
        public float CgHeight;

        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 5)]
        public float[] CarDamage;

        public int NumberOfTyresOut;
        public int PitLimiterOn;
        public float Abs;

        public float KersCharge;
        public float KersInput;
        public int AutoShifterOn;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] RideHeight;

        // since 1.5
        public float TurboBoost;
        public float Ballast;
        public float AirDensity;

        // since 1.6
        public float AirTemp;
        public float RoadTemp;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] LocalAngularVelocity;
        public float FinalFF;

        // since 1.7
        public float PerformanceMeter;
        public int EngineBrake;
        public int ErsRecoveryLevel;
        public int ErsPowerLevel;
        public int ErsHeatCharging;
        public int ErsisCharging;
        public float KersCurrentKJ;
        public int DrsAvailable;
        public int DrsEnabled;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] BrakeTemp;

        // since 1.10
        public float Clutch;

        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreTempI;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreTempM;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreTempO;

        // since 1.10.2
        public int IsAIControlled;

        // since 1.11
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public Coordinates[] TyreContactPoint;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public Coordinates[] TyreContactNormal;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public Coordinates[] TyreContactHeading;
        public float BrakeBias;

        // since 1.12
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] LocalVelocity;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
public struct SPageFileGraphic
{
    public int PacketId;
    public AC_STATUS Status;
    public AC_SESSION_TYPE Session;
    [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 15)]
    public String CurrentTime;
    [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 15)]
    public String LastTime;
    [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 15)]
    public String BestTime;
    [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 15)]
    public String Split;
    public int CompletedLaps;
    public int Position;
    public int iCurrentTime;
    public int iLastTime;
    public int iBestTime;
    public float SessionTimeLeft;
    public float DistanceTraveled;
    public int IsInPit;
    public int CurrentSectorIndex;
    public int LastSectorTime;
    public int NumberOfLaps;
    [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 33)]
    public String TyreCompound;

    public float ReplayTimeMultiplier;
    public float NormalizedCarPosition;
    [MarshalAs (UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] CarCoordinates;

    public float PenaltyTime;
    public AC_FLAG_TYPE Flag;
    public int IdealLineOn;

    // since 1.5
    public int IsInPitLane;
    public float SurfaceGrip;
    
    // since 1.13
    public int MandatoryPitDone;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
[Serializable]
public struct SPageFileStatic
{
        [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 15)]
        public string SMVersion;
        [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 15)]
        public string ACVersion;

        // session static info
        public int NumberOfSessions;
        public int NumCars;
        [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 33)]
        public string CarModel;
        [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 33)]
        public string Track;
        [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 33)]
        public string PlayerName;
        [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 33)]
        public string PlayerSurname;
        [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 33)]
        public string PlayerNick;

        public int SectorCount;

        // car static info
        public float MaxTorque;
        public float MaxPower;
        public int MaxRpm;
        public float MaxFuel;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] SuspensionMaxTravel;
        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreRadius;

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
        [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 15)]
        public string TrackConfiguration;

        // since 1.10.2
        public float ErsMaxJ;

        // since 1.13
        public int IsTimedRace;
        public int HasExtraLap;
        [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 33)]
        public string CarSkin;
        public int ReversedGridPositions;
        public int PitWindowStart;
        public int PitWindowEnd;
}
