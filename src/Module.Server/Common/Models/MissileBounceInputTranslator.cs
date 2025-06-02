using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common.Models;

public static class MissileBounceInputTranslator
{
    public static BounceInputs FromGame(
        float dot,
        float armorEffectiveness,
        ArmorComponent.ArmorMaterialTypes material,
        DamageTypes damageType,
        ItemObject.ItemTypeEnum missileType,
        float damageAmount,
        float speed,
        BoneBodyPartType part)
    {
        return new BounceInputs
        {
            Dot = dot,
            ArmorEffectivenessAmount = armorEffectiveness,
            ArmorMaterial = (PureArmorMaterial)(int)material,
            DamageType = (PureDamageType)(int)damageType,
            MissileType = (PureItemTypeEnum)(int)missileType,
            MissileDamageAmount = damageAmount,
            MissileSpeed = speed,
            BodyPartHit = ConvertBodyPart(part),
        };
    }

    private static PureBodyPart ConvertBodyPart(BoneBodyPartType part) => part switch
    {
        BoneBodyPartType.Head => PureBodyPart.Head,
        BoneBodyPartType.Chest => PureBodyPart.Chest,
        BoneBodyPartType.Abdomen => PureBodyPart.Abdomen,
        BoneBodyPartType.ArmLeft => PureBodyPart.ArmLeft,
        BoneBodyPartType.ArmRight => PureBodyPart.ArmRight,
        BoneBodyPartType.Legs => PureBodyPart.Legs,
        BoneBodyPartType.Neck => PureBodyPart.Neck,
        _ => PureBodyPart.Chest,
    };
}
