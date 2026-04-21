using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using SimLab.Models;

namespace SimLab.Services;

/// <summary>
/// Repository service for managing telemetry data aggregation and history
/// </summary>
public class TelemetryRepository
{
    private readonly List<SessionData> _sessionHistory = new();
    private int _lastLapNumber = -1;

    private readonly BehaviorSubject<SessionData?> _currentSessionSubject = new(null);
    private readonly Subject<LapInfo> _lapCompletedSubject = new();
    private readonly Subject<TelemetrySnapshot> _telemetryUpdatedSubject = new();

    public IObservable<SessionData?> CurrentSessionObservable => _currentSessionSubject;
    public IObservable<LapInfo> LapCompletedObservable => _lapCompletedSubject;
    public IObservable<TelemetrySnapshot> TelemetryUpdatedObservable => _telemetryUpdatedSubject;

    public SessionData? CurrentSession { get; private set; }

    public IReadOnlyList<SessionData> SessionHistory => _sessionHistory.AsReadOnly();
    public TelemetrySnapshot? CurrentSnapshot { get; private set; }

    /// <summary>
    /// Process incoming telemetry data
    /// </summary>
    public void ProcessTelemetry(TelemetrySnapshot snapshot)
    {
        CurrentSnapshot = snapshot;
        _telemetryUpdatedSubject.OnNext(snapshot);

        // Check if we need to start a new session
        if (CurrentSession == null || 
            CurrentSession.Track != snapshot.Track ||
            CurrentSession.SessionType != snapshot.SessionType)
        {
            StartNewSession(snapshot);
        }

        // Add snapshot to current session
        if (CurrentSession != null)
        {
            CurrentSession.Snapshots.Add(snapshot);

            // Check if a new lap was completed
            if (snapshot.CurrentLap > _lastLapNumber)
            {
                CompleteLap(_lastLapNumber, snapshot);
                _lastLapNumber = snapshot.CurrentLap;
            }
        }
    }

    /// <summary>
    /// Start a new session
    /// </summary>
    private void StartNewSession(TelemetrySnapshot snapshot)
    {
        // Save the previous session to history
        if (CurrentSession != null)
        {
            _sessionHistory.Add(CurrentSession);
        }

        CurrentSession = new SessionData
        {
            SessionStartTime = DateTime.UtcNow,
            Track = snapshot.Track,
            SessionType = snapshot.SessionType,
            CarModel = "", // Will be populated later
        };

        _lastLapNumber = snapshot.CurrentLap;
        _currentSessionSubject.OnNext(CurrentSession);
    }

    /// <summary>
    /// Mark a lap as complete
    /// </summary>
    private void CompleteLap(int lapNumber, TelemetrySnapshot snapshot)
    {
        if (CurrentSession == null || lapNumber < 0)
            return;

        // Find the lap time from recorded snapshots
        var lapSnapshots = CurrentSession.Snapshots.Skip(Math.Max(0, CurrentSession.Snapshots.Count - 60)).ToList();
        if (lapSnapshots.Count < 2)
            return;

        var firstSnapshot = lapSnapshots.First();
        var lastSnapshot = lapSnapshots.Last();
        var lapTime = lastSnapshot.RecordedAt - firstSnapshot.RecordedAt;

        var lapInfo = new LapInfo
        {
            LapNumber = lapNumber,
            LapTime = lapTime > TimeSpan.Zero ? lapTime : null,
            FuelUsed = 0, // Calculate based on fuel deltas
            AverageTireTemp = snapshot.TireTemperatures.Length > 0 ? snapshot.TireTemperatures.Average() : 0,
            IsValid = snapshot.IsOnTrack,
            RecordedAt = snapshot.RecordedAt
        };

        CurrentSession.Laps.Add(lapInfo);
        _lapCompletedSubject.OnNext(lapInfo);
    }

    /// <summary>
    /// Get statistics for current session
    /// </summary>
    public SessionStatistics GetCurrentSessionStatistics()
    {
        if (CurrentSession == null)
            return new SessionStatistics();

        var validLaps = CurrentSession.Laps.Where(l => l.IsValid && l.LapTime.HasValue).ToList();

        return new SessionStatistics
        {
            TotalLaps = CurrentSession.Laps.Count,
            ValidLaps = validLaps.Count,
            BestLapTime = CurrentSession.BestLap?.LapTime,
            AverageLapTime = validLaps.Any() ? TimeSpan.FromMilliseconds(validLaps.Average(l => l.LapTime.Value.TotalMilliseconds)) : null,
            SessionDuration = CurrentSession.TotalSessionTime,
            Track = CurrentSession.Track,
            SessionType = CurrentSession.SessionType
        };
    }

    /// <summary>
    /// Get recent lap times
    /// </summary>
    public IEnumerable<LapInfo> GetRecentLaps(int count = 10)
    {
        return CurrentSession?.Laps.TakeLast(count).Reverse() ?? Enumerable.Empty<LapInfo>();
    }

    /// <summary>
    /// Clear all sessions
    /// </summary>
    public void ClearHistory()
    {
        _sessionHistory.Clear();
        CurrentSession = null;
        _lastLapNumber = -1;
        _currentSessionSubject.OnNext(null);
    }
}

/// <summary>
/// Statistics for a session
/// </summary>
public class SessionStatistics
{
    public int TotalLaps { get; set; }
    public int ValidLaps { get; set; }
    public TimeSpan? BestLapTime { get; set; }
    public TimeSpan? AverageLapTime { get; set; }
    public TimeSpan? SessionDuration { get; set; }
    public string? Track { get; set; }
    public SessionType SessionType { get; set; }
}
