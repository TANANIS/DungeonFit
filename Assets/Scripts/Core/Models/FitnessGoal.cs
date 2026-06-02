using System.Collections.Generic;

namespace DungeonFit.Core.Models;

public static class FitnessGoal
{
    public const string MuscleGain = "muscle_gain";
    public const string FatLoss = "fat_loss";
    public const string Cardio = "cardio";
    public const string GeneralHealth = "general_health";

    private static readonly HashSet<string> ValidIds = new()
    {
        MuscleGain,
        FatLoss,
        Cardio,
        GeneralHealth,
    };

    public static IReadOnlyList<string> AllIds { get; } = new[]
    {
        MuscleGain,
        FatLoss,
        Cardio,
        GeneralHealth,
    };

    public static bool IsValid(string? goalId)
    {
        return !string.IsNullOrWhiteSpace(goalId) && ValidIds.Contains(goalId);
    }

    public static string Normalize(string? goalId)
    {
        return IsValid(goalId) ? goalId! : GeneralHealth;
    }

    public static string GetLabel(string? goalId)
    {
        return Normalize(goalId) switch
        {
            MuscleGain => "增肌",
            FatLoss => "減脂",
            Cardio => "心肺",
            _ => "健康維持",
        };
    }

    public static string GetAdvice(string? goalId)
    {
        return Normalize(goalId) switch
        {
            MuscleGain => "穩定完成組數，優先選力量訓練並逐步增加負荷。",
            FatLoss => "維持訓練頻率，搭配體重趨勢觀察，不用被單日波動牽著走。",
            Cardio => "注意節奏與休息，讓完成率和心肺耐力一起累積。",
            _ => "規律紀錄身體狀態，讓訓練在力量、活動量與恢復之間保持平衡。",
        };
    }
}
