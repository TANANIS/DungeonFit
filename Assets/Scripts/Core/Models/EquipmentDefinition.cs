using System.Collections.Generic;

namespace DungeonFit.Core.Models;

public sealed record EquipmentDefinition(
    string Id,
    string DisplayName,
    EquipmentSlot Slot,
    int BasePower,
    int BaseSellPrice,
    IReadOnlyList<EquipmentModifier> Modifiers);
