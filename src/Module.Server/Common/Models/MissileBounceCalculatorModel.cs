/*
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Crpg.Module.Common.Models;

public static class MissileBounceCalculatorModel
{
    public struct Inputs
    {
        public float Dot;                         // cosine of impact angle
        public float ArmorEffectivenessAmount;
        public ArmorComponent.ArmorMaterialTypes ArmorMaterial;
        public DamageTypes MissileDamageType;
        public ItemObject.ItemTypeEnum MissileType;
        public float MissileDamageAmount;
        public float MissileSpeed;
        public BoneBodyPartType BodyPartHit;
    }

    // Tweak these globally to tune your formula
    // Weight of different factors in determining bounce. 0.0 means no impact.
    private static readonly Dictionary<string, float> Weights = new()
    {
        ["angle"] = 1.0f, // angle of hit
        ["material"] = 1.0f, // armor material
        ["armor"] = 1.0f, // armor amount
        ["speed"] = 0.0f, // missile speed
        ["damage"] = 0.0f, // blow damage
        ["damageType"] = 0.8f, // cut/pierce/blunt
        ["missileType"] = 0.0f, // arrow/bolt/thrown/bullet
        ["bodyPart"] = 0.0f, // bodypart hit
    };

    public static float ComputeBounceChance(in Inputs i)
    {
        // 1) Compute each normalized factor (0..1)
        float fAngle = ComputeAngleFactor(i.Dot);
        float fMaterial = ComputeMaterialFactor(i.ArmorMaterial);
        float fArmor = ComputeArmorFactor(i.ArmorEffectivenessAmount, i.BodyPartHit);
        float fSpeed = ComputeSpeedFactor(i.MissileSpeed);
        float fDamage = ComputeDamageFactor(i.MissileDamageAmount);
        float fDamageType = ComputeDamageTypeFactor(i.MissileDamageType);
        float fMissile = ComputeMissileTypeFactor(i.MissileType);
        float fBodyPart = ComputeBodyPartFactor(i.BodyPartHit);

        // 2) Weighted average
        float rawScore =
            Weights["angle"] * fAngle +
            Weights["material"] * fMaterial +
            Weights["armor"] * fArmor +
            Weights["speed"] * fSpeed +
            Weights["damage"] * fDamage +
            Weights["damageType"] * fDamageType +
            Weights["missileType"] * fMissile +
            Weights["bodyPart"] * fBodyPart;

        float sumW = Weights.Values.Sum();
        float bounceChance = Math.Clamp(rawScore / sumW, 0f, 1f);

        // Temp random calculations
        var random = new Random();
        float randomFloat = (float)random.NextDouble(); // returns value in [0.0f, 1.0f)
        bool missileBounced = randomFloat < bounceChance;
        // Console.WriteLine($"fAngle: {fAngle}, fMaterial: {fMaterial}, fArmor{fArmor}, fDamage{fDamage}, fDmgType: {fDamageType}");
        // Console.WriteLine($"wAngle: {Weights["angle"]}, wMaterial: {Weights["material"]}, wArmor: {Weights["armor"]}, wDamage: {Weights["damage"]}, wDamageType: {Weights["damageType"]}");
        // Console.WriteLine($"rawScore: {rawScore} sumW: {sumW} BounceChance: {bounceChance} missileBounced:{missileBounced}");
        return bounceChance;
    }

    // --- factor helpers ---
    // 0.0 -> 1.0
    private static float ComputeAngleFactor(float dot)
      => Math.Clamp((0.3f - dot) / 0.3f, 0f, 1f);

    private static float ComputeMaterialFactor(ArmorComponent.ArmorMaterialTypes m) => m switch
    {
        ArmorComponent.ArmorMaterialTypes.Plate => 0.9f,
        ArmorComponent.ArmorMaterialTypes.Chainmail => 0.2f,
        ArmorComponent.ArmorMaterialTypes.Leather => 0.1f,
        ArmorComponent.ArmorMaterialTypes.Cloth => 0.1f,
        _ => 0.1f,
    };

    private static float ComputeArmorFactor(float actualArmor, BoneBodyPartType part)
    {
        // ratio vs expected, then scale
        float expected = part switch
        {
            BoneBodyPartType.Head => 55f, // high amounts
            BoneBodyPartType.Chest => 65f,
            BoneBodyPartType.Abdomen => 60f,
            BoneBodyPartType.ArmLeft or BoneBodyPartType.ArmRight => 25f,
            BoneBodyPartType.Legs => 25f,
            _ => 25f,
        };
        float ratio = expected > 0f ? actualArmor / expected : 0f;
        if (ratio < 0.5f)
        {
            return 0f;
        }

        if (ratio <= 2f)
        {
            return (ratio - 0.5f) / 1.5f;
        }

        return 1f;
    }

    private static float ComputeSpeedFactor(float speed)
      => Math.Clamp(1f - speed / 80f, 0f, 1f);

    private static float ComputeDamageFactor(float damage)
      => Math.Clamp(damage / 50f, 0f, 1f);

    private static float ComputeDamageTypeFactor(DamageTypes dt) => dt switch
    {
        DamageTypes.Cut => 0.6f,
        DamageTypes.Pierce => 0.2f,
        DamageTypes.Blunt => 0.1f,
        _ => 0.1f,
    };

    private static float ComputeMissileTypeFactor(ItemObject.ItemTypeEnum mt) => mt switch
    {
        ItemObject.ItemTypeEnum.Arrows => 0.3f,
        ItemObject.ItemTypeEnum.Bolts => 0.2f,
        ItemObject.ItemTypeEnum.Bullets => 0.1f,
        ItemObject.ItemTypeEnum.Thrown => 0.2f,
        _ => 0.1f,
    };

    private static float ComputeBodyPartFactor(BoneBodyPartType part) => part switch
    {
        BoneBodyPartType.Chest => 0.1f,
        BoneBodyPartType.Head => 0.1f,
        BoneBodyPartType.Abdomen => 0.1f,
        BoneBodyPartType.ArmLeft or BoneBodyPartType.ArmRight => 0.1f,
        BoneBodyPartType.Legs => 0.1f,
        BoneBodyPartType.Neck => 0.1f,
        _ => 0.1f,
    };
}

public static class MissileBounceTestAdapter
{
    public struct PureInputs
    {
        public float Dot;
        public float ArmorEffectivenessAmount;
        public int ArmorMaterial;      // Will convert to ArmorComponent.ArmorMaterialTypes
        public int MissileDamageType;  // Will convert to DamageTypes
        public int MissileType;        // Will convert to ItemObject.ItemTypeEnum
        public float MissileDamageAmount;
        public float MissileSpeed;
        public int BodyPartHit;        // Will convert to BoneBodyPartType
    }

    public static float ComputeBounceChance(PureInputs i)
    {
        var realInput = new MissileBounceCalculatorModel.Inputs
        {
            Dot = i.Dot,
            ArmorEffectivenessAmount = i.ArmorEffectivenessAmount,
            ArmorMaterial = (ArmorComponent.ArmorMaterialTypes)i.ArmorMaterial,
            MissileDamageType = (DamageTypes)i.MissileDamageType,
            MissileType = (ItemObject.ItemTypeEnum)i.MissileType,
            MissileDamageAmount = i.MissileDamageAmount,
            MissileSpeed = i.MissileSpeed,
            BodyPartHit = (BoneBodyPartType)i.BodyPartHit,
        };

        return MissileBounceCalculatorModel.ComputeBounceChance(in realInput);
    }
}
*/