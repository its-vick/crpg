﻿using Crpg.Module.Api.Models.Characters;
using Crpg.Module.Api.Models.Items;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace Crpg.Module.Common;

internal static class CrpgCharacterBuilder
{
    private static readonly Dictionary<CrpgItemSlot, EquipmentIndex> ItemSlotToIndex = new()
    {
        [CrpgItemSlot.Head] = EquipmentIndex.Head,
        [CrpgItemSlot.Shoulder] = EquipmentIndex.Cape,
        [CrpgItemSlot.Body] = EquipmentIndex.Body,
        [CrpgItemSlot.Hand] = EquipmentIndex.Gloves,
        [CrpgItemSlot.Leg] = EquipmentIndex.Leg,
        [CrpgItemSlot.MountHarness] = EquipmentIndex.HorseHarness,
        [CrpgItemSlot.Mount] = EquipmentIndex.Horse,
        [CrpgItemSlot.Weapon0] = EquipmentIndex.Weapon0,
        [CrpgItemSlot.Weapon1] = EquipmentIndex.Weapon1,
        [CrpgItemSlot.Weapon2] = EquipmentIndex.Weapon2,
        [CrpgItemSlot.Weapon3] = EquipmentIndex.Weapon3,
        [CrpgItemSlot.WeaponExtra] = EquipmentIndex.ExtraWeaponSlot,
    };

    public static CharacterSkills CreateCharacterSkills(CrpgCharacterCharacteristics characteristics)
    {
        CharacterSkills skills = new();
        skills.SetPropertyValue(CrpgSkills.Strength, characteristics.Attributes.Strength);
        skills.SetPropertyValue(CrpgSkills.Agility, characteristics.Attributes.Agility);

        skills.SetPropertyValue(CrpgSkills.IronFlesh, characteristics.Skills.IronFlesh);
        skills.SetPropertyValue(CrpgSkills.PowerStrike, characteristics.Skills.PowerStrike);
        skills.SetPropertyValue(CrpgSkills.PowerDraw, characteristics.Skills.PowerDraw);
        skills.SetPropertyValue(CrpgSkills.PowerThrow, characteristics.Skills.PowerThrow);
        skills.SetPropertyValue(DefaultSkills.Athletics, characteristics.Skills.Athletics * 20 + 2 * characteristics.Attributes.Agility);
        skills.SetPropertyValue(DefaultSkills.Riding, characteristics.Skills.Riding * 20);
        skills.SetPropertyValue(CrpgSkills.WeaponMaster, characteristics.Skills.WeaponMaster);
        skills.SetPropertyValue(CrpgSkills.MountedArchery, characteristics.Skills.MountedArchery);
        skills.SetPropertyValue(CrpgSkills.Shield, characteristics.Skills.Shield);

        skills.SetPropertyValue(DefaultSkills.OneHanded, characteristics.WeaponProficiencies.OneHanded);
        skills.SetPropertyValue(DefaultSkills.TwoHanded, characteristics.WeaponProficiencies.TwoHanded);
        skills.SetPropertyValue(DefaultSkills.Polearm, characteristics.WeaponProficiencies.Polearm);
        skills.SetPropertyValue(DefaultSkills.Bow, characteristics.WeaponProficiencies.Bow);
        skills.SetPropertyValue(DefaultSkills.Crossbow, characteristics.WeaponProficiencies.Crossbow);
        skills.SetPropertyValue(DefaultSkills.Throwing, characteristics.WeaponProficiencies.Throwing);

        return skills;
    }

    public static Equipment CreateCharacterEquipment(IList<CrpgEquippedItem> equippedItems)
    {
        Equipment equipment = new();
        foreach (var equippedItem in equippedItems)
        {
            var index = ItemSlotToIndex[equippedItem.Slot];
            AddEquipment(equipment, index, equippedItem.UserItem.ItemId);
        }

        return equipment;
    }

    public static Equipment CreateBotCharacterEquipment(IList<CrpgEquippedItem> equippedItems)
    {
        Equipment equipment = new();
        foreach (var equippedItem in equippedItems)
        {
            var index = ItemSlotToIndex[equippedItem.Slot];
            AddBotEquipment(equipment, index, equippedItem.UserItem.ItemId);
        }

        return equipment;
    }

    public static void AssignArmorsToTroopOrigin(CrpgBattleAgentOrigin origin, List<CrpgEquippedItem> items)
    {
        if (items == null)
        {
            return;
        }

        foreach (var item in items)
        {
            if (item == null)
            {
                continue;
            }

            var itemObject = MBObjectManager.Instance.GetObject<ItemObject>(item.UserItem.ItemId);

            if (itemObject == null)
            {
                continue;
            }

            if (itemObject.ItemType is ItemObject.ItemTypeEnum.HeadArmor or ItemObject.ItemTypeEnum.HandArmor or ItemObject.ItemTypeEnum.Cape or ItemObject.ItemTypeEnum.BodyArmor or ItemObject.ItemTypeEnum.LegArmor)
            {
                CrpgItemArmorComponent crpgItemArmorComponent = new()
                {
                    ArmArmor = itemObject.ArmorComponent.ArmArmor,
                    BodyArmor = itemObject.ArmorComponent.BodyArmor,
                    LegArmor = itemObject.ArmorComponent.LegArmor,
                    HeadArmor = itemObject.ArmorComponent.HeadArmor,
                };
                origin.ArmorItems.Add((crpgItemArmorComponent, itemObject.ItemType));
            }
        }
    }

    private static void AddEquipment(Equipment equipments, EquipmentIndex idx, string itemId)
    {
        var itemObject = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
        if (itemObject == null)
        {
            Debug.Print($"Cannot equip unknown item '{itemId}'");
            return;
        }

        if (!Equipment.IsItemFitsToSlot(idx, itemObject))
        {
            Debug.Print($"Cannot equip item '{itemId} on slot {idx}");
            return;
        }

        EquipmentElement equipmentElement = new(itemObject);
        equipments.AddEquipmentToSlotWithoutAgent(idx, equipmentElement);
    }

    private static void AddBotEquipment(Equipment equipments, EquipmentIndex idx, string itemId)
    {
        var itemObject = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
        if (itemObject == null)
        {
            Debug.Print($"Cannot equip unknown item '{itemId}'");
            return;
        }

        if (!Equipment.IsItemFitsToSlot(idx, itemObject))
        {
            Debug.Print($"Cannot equip item '{itemId} on slot {idx}");
            return;
        }

        if (itemObject.ItemType == ItemObject.ItemTypeEnum.Bow)
        {
            itemObject = MBObjectManager.Instance.GetObject<ItemObject>(itemId.Replace("crpg", "dtv"));
            if (itemObject == null)
            {
                Debug.Print($"Cannot find appropriate bot item for '{itemId}'");
                return;
            }
        }

        EquipmentElement equipmentElement = new(itemObject);
        equipments.AddEquipmentToSlotWithoutAgent(idx, equipmentElement);
    }
}
