namespace DungeonFit.UI;

public sealed class RoomPhaseController
{
    public RoomPhase Current { get; private set; } = RoomPhase.None;

    public bool IsActiveWave => Current == RoomPhase.ActiveWave;

    public bool IsRestCounting => Current == RoomPhase.RestCounting;

    public bool IsAwaitingReport => Current == RoomPhase.AwaitingReport;

    public bool IsResult => Current == RoomPhase.Result;

    public void StartWave()
    {
        Current = RoomPhase.ActiveWave;
    }

    public bool TryEnterRest()
    {
        if (!IsActiveWave)
        {
            return false;
        }

        Current = RoomPhase.RestCounting;
        return true;
    }

    public void AwaitReport()
    {
        Current = RoomPhase.AwaitingReport;
    }

    public bool CanReportSet()
    {
        return IsAwaitingReport;
    }

    public void Clear()
    {
        Current = RoomPhase.None;
    }

    public void ShowResult()
    {
        Current = RoomPhase.Result;
    }
}
