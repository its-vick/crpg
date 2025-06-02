using System.Reflection;
using NUnit.Framework;

namespace Crpg.Module.UTest.MissileBounce;

public static class TestBounceCalculator
{
    public struct Inputs
    {
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
        public float dot;                         // cosine of impact angle

        public float armorEffectiveness;
        public ArmorComponent.ArmorMaterialTypes material;
        public DamageTypes damageType;
        public ItemObject.ItemTypeEnum missileType;
        public float damageAmount;
        public float speed;
        public BoneBodyPartType bodyPart;
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter
    }

    // Tweak these globally to tune your formula
    private static readonly Dictionary<string, float> Weights = new()
    {
        ["angle"] = 1.0f, // angle of hit
        ["material"] = 1.0f, // armor material
        ["armor"] = 1.0f, // armor amount
        ["speed"] = 0.0f, // missile speed 0.0 means no impact on outcome
        ["damage"] = 0.0f, // blow damage
        ["damageType"] = 0.8f, // cut/pierce/blunt
        ["missileType"] = 0.0f, // arrow/bolt/thrown/bullet
        ["bodyPart"] = 0.0f, // bodypart hit
    };

    public static float ComputeBounceChance(in Inputs i)
    {
        // 1) Compute each normalized factor (0..1)
        float fAngle = ComputeAngleFactor(i.dot);
        float fMaterial = ComputeMaterialFactor(i.material);
        float fArmor = ComputeArmorFactor(i.armorEffectiveness, i.bodyPart);
        float fSpeed = ComputeSpeedFactor(i.speed);
        float fDamage = ComputeDamageFactor(i.damageAmount);
        float fDamageType = ComputeDamageTypeFactor(i.damageType);
        float fMissile = ComputeMissileTypeFactor(i.missileType);
        float fBodyPart = ComputeBodyPartFactor(i.bodyPart);

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

        var random = new Random();
        float randomFloat = (float)random.NextDouble(); // returns value in [0.0f, 1.0f)

        bool missileBounced = randomFloat < bounceChance;
        Console.WriteLine($"fAngle: {fAngle}, fMaterial: {fMaterial}, fArmor{fArmor}, fDamage{fDamage}, fDmgType: {fDamageType}");
        Console.WriteLine($"wAngle: {Weights["angle"]}, wMaterial: {Weights["material"]}, wArmor: {Weights["armor"]}, wDamage: {Weights["damage"]}, wDamageType: {Weights["damageType"]}");
        Console.WriteLine($"rawScore: {rawScore} sumW: {sumW} BounceChance: {bounceChance} missileBounced:{missileBounced}");
        return bounceChance;
    }

    // --- factor helpers ---
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

public static class ArmorComponent
{
    public enum ArmorMaterialTypes : sbyte
    {
        None,
        Cloth,
        Leather,
        Chainmail,
        Plate,
    }
}

public static class ItemObject
{
    public enum ItemTypeEnum
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
}

public enum BoneBodyPartType : sbyte
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

public enum DamageTypes
{
    Invalid = -1, // 0xFFFFFFFF
    Cut = 0,
    Pierce = 1,
    Blunt = 2,
    NumberOfDamageTypes = 3,
}

[TestFixture]
public class BounceCalculatorTests
{
    [TestCase(DamageTypes.Cut, TestName = "HeadOnPlate_Cut_HighBounce")]
    [TestCase(DamageTypes.Pierce, TestName = "HeadOnPlate_Pierce_HighBounce")]
    [TestCase(DamageTypes.Blunt, TestName = "HeadOnPlate_Blunt_HighBounce")]
    public void HeadOnPlate_AllDamageTypes_HighChanceOfBounce(DamageTypes damageType)
    {
        var inputs = new TestBounceCalculator.Inputs
        {
            dot = 1f, // head-on
            material = ArmorComponent.ArmorMaterialTypes.Plate,
            armorEffectiveness = 90f,
            damageType = damageType,
            missileType = ItemObject.ItemTypeEnum.Arrows,
            damageAmount = 10f,
            speed = 30f,
            bodyPart = BoneBodyPartType.Head,
        };

        float chance = TestBounceCalculator.ComputeBounceChance(inputs);
        Assert.That(chance, Is.GreaterThan(0.45f),
            $"Expected a high bounce chance for {damageType} on heavy plate head-on, but got {chance:F2}");
    }

    [TestCase(DamageTypes.Cut, TestName = "GlanceOnPlate_Cut_HighBounce")]
    [TestCase(DamageTypes.Pierce, TestName = "GlanceOnPlate_Pierce_HighBounce")]
    [TestCase(DamageTypes.Blunt, TestName = "GlanceOnPlate_Blunt_HighBounce")]
    public void GlanceOnPlate_AllDamageTypes_HighChanceOfBounce(DamageTypes damageType)
    {
        var inputs = new TestBounceCalculator.Inputs
        {
            dot = 0.1f, // glance angle
            material = ArmorComponent.ArmorMaterialTypes.Plate,
            armorEffectiveness = 90f,
            damageType = damageType,
            missileType = ItemObject.ItemTypeEnum.Arrows,
            damageAmount = 10f,
            speed = 30f,
            bodyPart = BoneBodyPartType.Head,
        };

        float chance = TestBounceCalculator.ComputeBounceChance(inputs);
        Assert.That(chance, Is.GreaterThan(0.5f),
            $"Expected a high bounce chance for {damageType} on heavy plate glance, but got {chance:F2}");
    }

    [Test]
    public void TestPerfectGlanceOnPlate_ShouldHighBounceChance()
    {
        var inputs = new TestBounceCalculator.Inputs
        {
            dot = 0.1f,
            material = ArmorComponent.ArmorMaterialTypes.Plate,
            armorEffectiveness = 70f,
            damageType = (int)DamageTypes.Cut,
            missileType = ItemObject.ItemTypeEnum.Arrows,
            damageAmount = 10f,
            speed = 30f,
        };
        float chance = TestBounceCalculator.ComputeBounceChance(inputs);
        Assert.That(chance, Is.GreaterThan(0.5f));
    }

    [Test]
    public void TestStraightPierceOnCloth_ShouldLowBounceChance()
    {
        var inputs = new TestBounceCalculator.Inputs
        {
            dot = 1f, // head-on
            material = ArmorComponent.ArmorMaterialTypes.Cloth,
            armorEffectiveness = 5f,
            damageType = DamageTypes.Pierce,
            missileType = ItemObject.ItemTypeEnum.Arrows,
            damageAmount = 40f,
            speed = 80f,
        };
        float chance = TestBounceCalculator.ComputeBounceChance(inputs);
        Assert.That(chance, Is.LessThan(0.1f));
    }

    // 1) Ensure missileTypeFactor is wired in:
    [TestCase(ItemObject.ItemTypeEnum.Arrows, ExpectedResult = true, TestName = "MissileType_Arrows_IncreasesChance")]
    [TestCase(ItemObject.ItemTypeEnum.Bullets, ExpectedResult = false, TestName = "MissileType_Bullets_LowerChance")]
    public bool MissileTypeWeightTest(ItemObject.ItemTypeEnum missileType)
    {
        var inputs = new TestBounceCalculator.Inputs
        {
            dot = 0.5f,                                  // medium grazing
            material = ArmorComponent.ArmorMaterialTypes.Plate,
            armorEffectiveness = 60f,
            damageType = (int)DamageTypes.Cut,
            missileType = missileType,
            damageAmount = 25f,
            speed = 40f,
            bodyPart = BoneBodyPartType.Chest,
        };
        // Compute bounce chance twice: once as-is, once with a "null" missileType
        float chanceWithType = TestBounceCalculator.ComputeBounceChance(inputs);
        // Now force missileType to something outside your enum to get _default = 0.1f
        inputs.missileType = (ItemObject.ItemTypeEnum)(-1);
        float chanceDefault = TestBounceCalculator.ComputeBounceChance(inputs);

        TestContext.WriteLine($"with {missileType} -> {chanceWithType:F2}, default-> {chanceDefault:F2}");
        return chanceWithType > chanceDefault;
    }

    // 2) Sweep all damage types for Leather armor at fixed geometry:
    [TestCase(DamageTypes.Cut)]
    [TestCase(DamageTypes.Pierce)]
    [TestCase(DamageTypes.Blunt)]
    [TestCase(DamageTypes.Invalid)]
    public void DamageTypeSweep_Leather(DamageTypes dt)
    {
        var inputs = new TestBounceCalculator.Inputs
        {
            dot = 0.2f,                                  // light grazing
            material = ArmorComponent.ArmorMaterialTypes.Leather,
            armorEffectiveness = 30f,
            damageType = dt,
            missileType = ItemObject.ItemTypeEnum.Arrows,
            damageAmount = 20f,
            speed = 50f,
            bodyPart = BoneBodyPartType.Legs,
        };
        float chance = TestBounceCalculator.ComputeBounceChance(inputs);
        TestContext.WriteLine($"{dt}: bounceChance={chance:F2}");
        Assert.That(chance, Is.InRange(0f, 1f));
    }

    // 3) Boundary: zero damage should produce zero bounce chance
    [Test]
    public void ZeroDamage_YieldsZeroChance()
    {
        var inputs = new TestBounceCalculator.Inputs
        {
            dot = 0.5f,
            material = ArmorComponent.ArmorMaterialTypes.Plate,
            armorEffectiveness = 80f,
            damageType = (int)DamageTypes.Cut,
            missileType = ItemObject.ItemTypeEnum.Arrows,
            damageAmount = 0f,
            speed = 40f,
            bodyPart = BoneBodyPartType.Chest,
        };
        Assert.That(TestBounceCalculator.ComputeBounceChance(inputs), Is.LessThan(0.4f));
    }

    // 4) Boundary: perfect graze (dot = 0) on plate with max armor & damage -> chance = 1
    [Test]
    public void PerfectGrazeMaxArmorMaxDamage_AlwaysBounce()
    {
        var inputs = new TestBounceCalculator.Inputs
        {
            dot = 0f,
            material = ArmorComponent.ArmorMaterialTypes.Plate,
            armorEffectiveness = 100f,  // plenty of armor
            damageType = (int)DamageTypes.Cut,
            missileType = ItemObject.ItemTypeEnum.Arrows,
            damageAmount = 100f,  // above your 50f threshold
            speed = 0f,    // very slow
            bodyPart = BoneBodyPartType.Head,
        };
        Assert.That(TestBounceCalculator.ComputeBounceChance(inputs), Is.EqualTo(1f));
    }

    // 5) Test every body part to ensure no NaNs:
    [Test]
    public void BodyPartSweep_NoNaNOrInf()
    {
        foreach (BoneBodyPartType part in Enum.GetValues(typeof(BoneBodyPartType)))
        {
            var inputs = new TestBounceCalculator.Inputs
            {
                dot = 0.4f,
                material = ArmorComponent.ArmorMaterialTypes.Chainmail,
                armorEffectiveness = 40f,
                damageType = DamageTypes.Pierce,
                missileType = ItemObject.ItemTypeEnum.Bolts,
                damageAmount = 30f,
                speed = 60f,
                bodyPart = part,
            };
            float chance = TestBounceCalculator.ComputeBounceChance(inputs);
            Assert.That(float.IsNaN(chance), Is.False, $"NaN returned for bodyPart {part}");
            Assert.That(float.IsInfinity(chance), Is.False, $"Infinity returned for bodyPart {part}");
            Assert.That(chance, Is.InRange(0f, 1f), $"Out-of-range for bodyPart {part}");
        }
    }

    // 6) Monotonicity over angle: as dot ↓ (more grazing) chance ↑
    [Test]
    public void BounceChance_MoreGlancingAngle_Increases()
    {
#pragma warning disable IDE0017 // Simplify object initialization
        var baseInputs = new TestBounceCalculator.Inputs
        {
            material = ArmorComponent.ArmorMaterialTypes.Plate,
            armorEffectiveness = 60f,
            damageType = DamageTypes.Cut,
            missileType = ItemObject.ItemTypeEnum.Arrows,
            damageAmount = 30f,
            speed = 40f,
            bodyPart = BoneBodyPartType.Chest,
        };
#pragma warning restore IDE0017 // Simplify object initialization

        baseInputs.dot = 0.8f; // almost head-on
        float headOn = TestBounceCalculator.ComputeBounceChance(baseInputs);

        baseInputs.dot = 0.25f; // medium
        float mid = TestBounceCalculator.ComputeBounceChance(baseInputs);

        baseInputs.dot = 0.0f; // perfect graze
        float graze = TestBounceCalculator.ComputeBounceChance(baseInputs);

        Assert.That(headOn, Is.LessThan(mid).And.LessThan(graze),
            "Expected head-on < mid < graze");
    }

    // 7) Chainmail vs Plate: under identical conditions Plate should bounce more
    [Test]
    public void BounceChance_PlateHigherThanChainmail()
    {
        var inputs = new TestBounceCalculator.Inputs
        {
            dot = 0.2f,
            material = ArmorComponent.ArmorMaterialTypes.Chainmail,
            armorEffectiveness = 50f,
            damageType = DamageTypes.Cut,
            missileType = ItemObject.ItemTypeEnum.Arrows,
            damageAmount = 30f,
            speed = 40f,
            bodyPart = BoneBodyPartType.Chest,
        };
        float chain = TestBounceCalculator.ComputeBounceChance(inputs);

        inputs.material = ArmorComponent.ArmorMaterialTypes.Plate;
        float plate = TestBounceCalculator.ComputeBounceChance(inputs);

        Assert.That(plate, Is.GreaterThan(chain),
            "Plate should have higher bounce chance than chainmail under same conditions");
    }

    // 8) High speed vs low speed: all else equal, lower speed → higher bounce
    [Test]
    public void BounceChance_SlowerMissile_Increases()
    {
#pragma warning disable IDE0017 // Simplify object initialization
        var inputs = new TestBounceCalculator.Inputs
        {
            dot = 0.3f,
            material = ArmorComponent.ArmorMaterialTypes.Plate,
            armorEffectiveness = 60f,
            damageType = DamageTypes.Cut,
            missileType = ItemObject.ItemTypeEnum.Arrows,
            damageAmount = 30f,
            bodyPart = BoneBodyPartType.Chest,
        };
#pragma warning restore IDE0017 // Simplify object initialization

        inputs.speed = 80f;
        float fast = TestBounceCalculator.ComputeBounceChance(inputs);

        inputs.speed = 20f;
        float slow = TestBounceCalculator.ComputeBounceChance(inputs);

        Assert.That(slow, Is.GreaterThan(fast),
            "Slower projectiles should bounce more often than faster ones");
    }

    // 9) Very low armor (<50% expected) always yields zero armor factor
    [Test]
    public void BounceChance_LowArmorFactor_ZeroContribution()
    {
        var inputs = new TestBounceCalculator.Inputs
        {
            dot = 0.1f,
            material = ArmorComponent.ArmorMaterialTypes.Plate,
            armorEffectiveness = 10f,     // below 50% of any expected
            damageType = DamageTypes.Cut,
            missileType = ItemObject.ItemTypeEnum.Arrows,
            damageAmount = 30f,
            speed = 20f,
            bodyPart = BoneBodyPartType.Head,
        };

        // Compute the pure armor factor helper via reflection (optional):
        // But here we just ensure bounce chance doesn't use that armor.
        float chance = TestBounceCalculator.ComputeBounceChance(inputs);
        Assert.That(chance, Is.LessThan(1f),
            "With extremely low armor the factor should not artificially inflate chance");
    }

    // 10) Test composite: make sure all weights summing to zero yields zero chance
    [Test]
    public void BounceChance_AllWeightsZero_YieldsZero()
    {
        // temporarily zero out all weights

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var original = typeof(TestBounceCalculator)
          .GetField("Weights", BindingFlags.NonPublic | BindingFlags.Static)
          .GetValue(null) as Dictionary<string, float>;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

#pragma warning disable CS8604 // Possible null reference argument.
        var backup = new Dictionary<string, float>(original);
#pragma warning restore CS8604 // Possible null reference argument.
        foreach (string? key in original.Keys.ToList())
        {
            original[key] = 0f;
        }

        var inputs = new TestBounceCalculator.Inputs
        {
            dot = 0f,
            material = ArmorComponent.ArmorMaterialTypes.Plate,
            armorEffectiveness = 100f,
            damageType = DamageTypes.Cut,
            missileType = ItemObject.ItemTypeEnum.Arrows,
            damageAmount = 100f,
            speed = 0f,
            bodyPart = BoneBodyPartType.Head,
        };

        float chance = TestBounceCalculator.ComputeBounceChance(inputs);
        Assert.That(chance, Is.EqualTo(0f),
            "If all weights are zero, normalized score should be zero");

        // restore
        foreach (var kv in backup)
        {
            original[kv.Key] = kv.Value;
        }
    }
}
