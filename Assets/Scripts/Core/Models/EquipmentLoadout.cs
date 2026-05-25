namespace DungeonFit.Core.Models;

public sealed class EquipmentLoadout
{
    public string? WeaponId { get; set; }

    public string? ArmorId { get; set; }

    public string? AccessoryId { get; set; }

    public string? GetEquippedId(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => WeaponId,
            EquipmentSlot.Armor => ArmorId,
            EquipmentSlot.Accessory => AccessoryId,
            _ => null,
        };
    }

    public void Equip(EquipmentItem item)
    {
        switch (item.Slot)
        {
            case EquipmentSlot.Weapon:
                WeaponId = item.Id;
                break;
            case EquipmentSlot.Armor:
                ArmorId = item.Id;
                break;
            case EquipmentSlot.Accessory:
                AccessoryId = item.Id;
                break;
        }
    }

    public void Unequip(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon:
                WeaponId = null;
                break;
            case EquipmentSlot.Armor:
                ArmorId = null;
                break;
            case EquipmentSlot.Accessory:
                AccessoryId = null;
                break;
        }
    }

    public bool IsEquipped(string itemId)
    {
        return WeaponId == itemId || ArmorId == itemId || AccessoryId == itemId;
    }
}
