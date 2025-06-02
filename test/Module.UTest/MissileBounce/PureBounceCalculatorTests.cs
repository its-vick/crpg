// Crpg.Module.UTest/BounceTests.cs
using System.Reflection;
using Crpg.Module.Common.Models;
using NUnit.Framework;

namespace Crpg.Module.UTest.MissileBounce;

[TestFixture]
public class PureBounceCalculatorTests
{
    [TestCase(PureDamageType.Cut, TestName = "HeadOnPlate_Cut_HighBounce")]
    [TestCase(PureDamageType.Pierce, TestName = "HeadOnPlate_Pierce_HighBounce")]
    [TestCase(PureDamageType.Blunt, TestName = "HeadOnPlate_Blunt_HighBounce")]
    public void HeadOnPlate_AllDamageTypes_HighChanceOfBounce(PureDamageType damageType)
    {
        var input = new BounceInputs
        {
            Dot = 1f, // head-on
            ArmorMaterial = PureArmorMaterial.Plate,
            ArmorEffectivenessAmount = 90f,
            DamageType = damageType,
            MissileType = PureItemTypeEnum.Arrows,
            MissileDamageAmount = 30f,
            MissileSpeed = 50f,
            BodyPartHit = (int)PureBodyPart.Head,
        };

        float chance = PureMissileBounceCalculator.ComputeBounceChance(input);
        Console.WriteLine($"bounceChance: {chance}");
        Assert.That(chance, Is.GreaterThan(0.5f),
            $"Expected a high bounce chance for {damageType} on heavy plate head-on, but got {chance:F2}");
    }

    // Material Impact
    [TestCase(PureArmorMaterial.Plate, 0.68f, TestName = "MaterialFactor_Plate_High")]
    [TestCase(PureArmorMaterial.Chainmail, 0.2f, TestName = "MaterialFactor_Chainmail_Medium")]
    [TestCase(PureArmorMaterial.Leather, 0.1f, TestName = "MaterialFactor_Leather_Low")]
    [TestCase(PureArmorMaterial.Cloth, 0.1f, TestName = "MaterialFactor_Cloth_Low")]
    [TestCase(PureArmorMaterial.None, 0.1f, TestName = "MaterialFactor_None_Low")]
    public void MaterialFactor_ExpectedValues(PureArmorMaterial material, float expected)
    {
        var input = new BounceInputs
        {
            Dot = 0f,
            ArmorMaterial = material,
            ArmorEffectivenessAmount = 70f,
            DamageType = PureDamageType.Cut,
            MissileType = PureItemTypeEnum.Arrows,
            MissileDamageAmount = 10f,
            MissileSpeed = 10f,
            BodyPartHit = PureBodyPart.Chest,
        };

        float chance = PureMissileBounceCalculator.ComputeBounceChance(input);
        Console.WriteLine($"bounceChance: {chance} material: {material}");
        Assert.That(chance, Is.EqualTo(expected).Within(0.2f));
    }

    // Angle Impact
    [TestCase(1f, 0.0f, TestName = "AngleFactor_HeadOn_ZeroBounce")]
    [TestCase(0.5f, 0.67f, TestName = "AngleFactor_MediumAngle_MediumBounce")]
    [TestCase(0f, 1.0f, TestName = "AngleFactor_Perpendicular_HighBounce")]
    public void AngleFactor_Variation(float dot, float expectedFactor)
    {
        float result = typeof(PureMissileBounceCalculator)
            .GetMethod("ComputeAngleFactor", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { dot }) as float? ?? -1f;

        Console.WriteLine($"ComputeAngleFactor: {result} dot: {dot}");
        Assert.That(result, Is.EqualTo(expectedFactor).Within(0.01f));
    }

    // Armor Effectiveness Scaling
    [TestCase(20f, PureBodyPart.Chest, TestName = "ArmorEffectiveness_Low")]
    [TestCase(65f, PureBodyPart.Chest, TestName = "ArmorEffectiveness_Expected")]
    [TestCase(130f, PureBodyPart.Chest, TestName = "ArmorEffectiveness_Over")]
    public void ArmorFactor_Variation(float armorAmount, PureBodyPart part)
    {
        var input = new BounceInputs
        {
            Dot = 0.2f,
            ArmorMaterial = PureArmorMaterial.Chainmail,
            ArmorEffectivenessAmount = armorAmount,
            DamageType = PureDamageType.Pierce,
            MissileType = PureItemTypeEnum.Bolts,
            MissileDamageAmount = 25f,
            MissileSpeed = 40f,
            BodyPartHit = part,
        };

        float chance = PureMissileBounceCalculator.ComputeBounceChance(input);
        Console.WriteLine($"ArmorFactor Chance: {chance:F2} armorAmount: {armorAmount}");
        Assert.That(chance, Is.InRange(0f, 1f));
    }

    // Missile Type Variations
    [TestCase(PureItemTypeEnum.Arrows)]
    [TestCase(PureItemTypeEnum.Bolts)]
    [TestCase(PureItemTypeEnum.Bullets)]
    [TestCase(PureItemTypeEnum.Thrown)]
    public void MissileType_Variations(PureItemTypeEnum missile)
    {
        var input = new BounceInputs
        {
            Dot = 0.2f,
            ArmorMaterial = PureArmorMaterial.Plate,
            ArmorEffectivenessAmount = 60f,
            DamageType = PureDamageType.Cut,
            MissileType = missile,
            MissileDamageAmount = 20f,
            MissileSpeed = 30f,
            BodyPartHit = PureBodyPart.Chest,
        };

        float chance = PureMissileBounceCalculator.ComputeBounceChance(input);
        Assert.That(chance, Is.InRange(0f, 1f));
    }
}
