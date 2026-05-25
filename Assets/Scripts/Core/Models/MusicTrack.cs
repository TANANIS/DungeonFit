namespace DungeonFit.Core.Models;

public sealed record MusicTrack(
	string Id,
	string DisplayName,
	int Bpm,
	string ResourcePath = "",
	double BeatOffsetSeconds = 0,
	double UsableStartSeconds = 8,
	double UsableEndSeconds = 0,
	double LoopStartSeconds = 0,
	double LoopEndSeconds = 0,
	float VolumeOffsetDb = 0);
