using DungeonFit.Core.Models;
using System.Collections.Generic;

namespace DungeonFit.Gameplay;

public sealed record MoonlightFountainViewModel(
    int Level,
    int Experience,
    int ExperienceToNextLevel,
    int Gold,
    int CurrentHp,
    int MaxHp,
    bool RecoveryUsed,
    bool CanUseRecovery,
    string SelectedBlessingId,
    bool CanSelectBlessing,
    IReadOnlyList<DailyBlessingOptionViewModel> Blessings);

public sealed record DailyBlessingOptionViewModel(
    string Id,
    string Name,
    string Description,
    bool IsSelected,
    bool IsDisabled);

public sealed record HerbShopViewModel(
    int Level,
    int Experience,
    int ExperienceToNextLevel,
    int Gold,
    int CurrentHp,
    int MaxHp,
    bool CanBuyBasicHeal,
    bool CanBuyFullHeal,
    bool CanBuySmallPotion,
    int SmallPotionCount,
    int PotionPurchasesToday,
    int PotionPurchaseLimit);

public sealed record RoomSupplyViewModel(
    int SmallPotionCount,
    int CarryLimit,
    bool CanUseSmallPotion);

public sealed record SupplyUseResult(
    bool Used,
    int Healed,
    int CurrentHp,
    int MaxHp,
    RoomSupplyViewModel Supply);
