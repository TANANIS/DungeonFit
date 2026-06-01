using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Content;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Rules;

public sealed class DungeonRouteRules
{
    public const int MinRouteSlots = 4;
    public const int MaxRouteSlots = 6;
    public const int MinSets = 1;
    public const int MaxSets = 8;
    public const int DefaultSets = 4;
    public const int MinReps = 1;
    public const int MaxReps = 50;
    public const int DefaultReps = 12;
    public const int DefaultRestSeconds = 90;
    public const int BeatsPerRep = 8;

    public static readonly int[] RestSecondOptions = { 60, 90, 120, 300 };

    private readonly MusicCatalog _musicCatalog;
    private readonly ExerciseCatalog _exerciseCatalog = new();

    public DungeonRouteRules()
        : this(new MusicCatalog())
    {
    }

    public DungeonRouteRules(MusicCatalog musicCatalog)
    {
        _musicCatalog = musicCatalog;
    }

    public DungeonRouteSlot CreateDefaultSlot(string dungeonTypeId)
    {
        var defaultTrack = _musicCatalog.GetAll()[0];
        return new DungeonRouteSlot(
            dungeonTypeId,
            DefaultSets,
            DefaultReps,
            defaultTrack.Id,
            DefaultRestSeconds,
            _exerciseCatalog.GetDefaultForDungeon(dungeonTypeId).Id);
    }

    public DungeonRouteSlot Normalize(DungeonRouteSlot slot)
    {
        var track = _musicCatalog.GetById(_musicCatalog.ResolveId(slot.MusicId));
        var exercise = _exerciseCatalog.GetById(slot.DungeonTypeId, slot.ExerciseId);
        return slot with
        {
            TargetSets = Clamp(slot.TargetSets, MinSets, MaxSets),
            TargetReps = Clamp(slot.TargetReps, MinReps, MaxReps),
            MusicId = track.Id,
            RestSeconds = NormalizeRestSeconds(slot.RestSeconds),
            ExerciseId = exercise.Id,
        };
    }

    public IReadOnlyList<DungeonRouteSlot> NormalizeRoute(IEnumerable<DungeonRouteSlot> slots)
    {
        return slots
            .Where(slot => !string.IsNullOrWhiteSpace(slot.DungeonTypeId))
            .Select(Normalize)
            .ToArray();
    }

    public bool CanStartRoute(IReadOnlyList<DungeonRouteSlot> slots)
    {
        return slots.Count >= MinRouteSlots && slots.Count <= MaxRouteSlots;
    }

    public WorkoutTimingProfile CreateTimingProfile(DungeonRouteSlot slot, int fallbackBpm)
    {
        var track = _musicCatalog.GetById(_musicCatalog.ResolveId(slot.MusicId));
        var bpm = track.Bpm > 0 ? track.Bpm : fallbackBpm;

        return new WorkoutTimingProfile(
            bpm,
            BeatsPerRep,
            slot.TargetReps,
            slot.TargetSets,
            NormalizeRestSeconds(slot.RestSeconds));
    }

    private static int NormalizeRestSeconds(int restSeconds)
    {
        return RestSecondOptions.Contains(restSeconds)
            ? restSeconds
            : RestSecondOptions.OrderBy(option => System.Math.Abs(option - restSeconds)).First();
    }

    private static int Clamp(int value, int min, int max)
    {
        return value < min ? min : value > max ? max : value;
    }
}
