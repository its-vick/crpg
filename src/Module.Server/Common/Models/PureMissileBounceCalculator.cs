using TaleWorlds.Library;
using MathF = TaleWorlds.Library.MathF;
namespace Crpg.Module.Common.Models;

public static class PureMissileBounceCalculator
{
    private static readonly Dictionary<string, float> Weights = new()
    {
        ["angle"] = 1.0f,
        ["material"] = 1.0f,
        ["armor"] = 1.0f,
        ["speed"] = 0.0f,
        ["damage"] = 0.0f,
        ["damageType"] = 0.5f,
        ["missileType"] = 0.0f,
        ["bodyPart"] = 0.0f,
    };

    public static float ComputeBounceChance(in BounceInputs i)
    {
        float fAngle = ComputeAngleFactor(i.Dot);
        float fMaterial = ComputeMaterialFactor(i.ArmorMaterial);
        float fArmor = ComputeArmorFactor(i.ArmorEffectivenessAmount, i.BodyPartHit);
        float fSpeed = ComputeSpeedFactor(i.MissileSpeed);
        float fDamage = ComputeDamageFactor(i.MissileDamageAmount);
        float fDamageType = ComputeDamageTypeFactor(i.DamageType);
        float fMissile = ComputeMissileTypeFactor(i.MissileType);
        float fBodyPart = ComputeBodyPartFactor(i.BodyPartHit);

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
        Console.WriteLine($"Inputs: {i.ArmorMaterial}:{i.ArmorEffectivenessAmount}, {i.DamageType}, {i.Dot}");
        Console.WriteLine($"Factors: Angle: {fAngle}, Material: {fMaterial}, Armor: {fArmor}, Speed: {fSpeed}, " +
                          $"Damage: {fDamage}, DamageType: {fDamageType}, MissileType: {fMissile}, BodyPart: {fBodyPart}");
        Console.WriteLine($"rawScore: {rawScore} sumW: {sumW} BounceChance: {bounceChance}");
        return bounceChance;
    }

    // --- Helpers ---

    private static float ComputeAngleFactor(float dot)
        => Math.Clamp((0.3f - dot) / 0.3f, 0f, 1f);

    private static float ComputeMaterialFactor(PureArmorMaterial m) => m switch
    {
        PureArmorMaterial.None => 0.0f,
        PureArmorMaterial.Cloth => 0.0f,
        PureArmorMaterial.Leather => 0.0f,
        PureArmorMaterial.Chainmail => 0.2f,
        PureArmorMaterial.Plate => 1.0f,
        _ => 0.0f,
    };

    private static float ComputeArmorFactor(float armor, PureBodyPart part)
    {
        float expected = part switch
        {
            PureBodyPart.Head => 55f,
            PureBodyPart.Chest => 65f,
            PureBodyPart.Abdomen => 60f,
            PureBodyPart.ArmLeft or PureBodyPart.ArmRight => 25f,
            PureBodyPart.Legs => 25f,
            _ => 25f,
        };

        if (expected <= 0f)
            return 0f;

        float ratio = armor / expected;

        if (ratio < 1f)
        {
            // Below expected: no chance
            return 0f;
        }
        else
        {
            // Above expected: start curve at ratio = 1 (e.g., 0.5 base)
            float excess = ratio - 1f;
            float growthPower = 0.8f;
            return 0.5f + MathF.Pow(excess, growthPower);
        }

        /* // original code
                float ratio = expected > 0f ? armor / expected : 0f;
                return ratio switch
                {
                    < 0.5f => 0f,
                    <= 2f => (ratio - 0.5f) / 1.5f,
                    _ => 1f,
                };
        */
    }

    private static float ComputeSpeedFactor(float speed)
        => Math.Clamp(1f - speed / 80f, 0f, 1f);

    private static float ComputeDamageFactor(float damage)
        => Math.Clamp(damage / 50f, 0f, 1f);

    private static float ComputeDamageTypeFactor(PureDamageType t) => t switch
    {
        PureDamageType.Cut => 0.4f,
        PureDamageType.Pierce => 0.2f,
        PureDamageType.Blunt => 0.1f,
        _ => 0.1f,
    };

    private static float ComputeMissileTypeFactor(PureItemTypeEnum t) => t switch
    {
        PureItemTypeEnum.Arrows => 0.3f,
        PureItemTypeEnum.Bolts => 0.2f,
        PureItemTypeEnum.Bullets => 0.1f,
        PureItemTypeEnum.Thrown => 0.1f,
        _ => 0.1f,
    };

    private static float ComputeBodyPartFactor(PureBodyPart part)
        => 0.0f; // flat value for now
}

public enum PureDamageType
{
    Invalid = -1,
    Cut = 0,
    Pierce = 1,
    Blunt = 2,
}

public enum PureArmorMaterial
{
    None,
    Cloth,
    Leather,
    Chainmail,
    Plate,
}

public enum PureBodyPart
{
    None = -1, // 0xFF
    CriticalBodyPartsBegin = 0,
    Head = 0,
    Neck = 1,
    Chest = 2,
    Abdomen = 3,
    ShoulderLeft = 4,
    ShoulderRight = 5,
    ArmLeft = 6,
    CriticalBodyPartsEnd = 6,
    ArmRight = 7,
    Legs = 8,
    NumOfBodyPartTypes = 9,
}

public enum PureItemTypeEnum
{
    Invalid,
    Horse,
    OneHandedWeapon,
    TwoHandedWeapon,
    Polearm,
    Arrows,
    Bolts,
    Shield,
    Bow,
    Crossbow,
    Thrown,
    Goods,
    HeadArmor,
    BodyArmor,
    LegArmor,
    HandArmor,
    Pistol,
    Musket,
    Bullets,
    Animal,
    Book,
    ChestArmor,
    Cape,
    HorseHarness,
    Banner,
}

public struct BounceInputs
{
    public float Dot;   // Cosine of impact angle
    public float ArmorEffectivenessAmount;
    public PureArmorMaterial ArmorMaterial;
    public PureDamageType DamageType;
    public PureItemTypeEnum MissileType;
    public float MissileDamageAmount;
    public float MissileSpeed;
    public PureBodyPart BodyPartHit;
}
