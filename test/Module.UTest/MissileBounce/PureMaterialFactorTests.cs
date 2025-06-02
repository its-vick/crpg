using System;
using System.Reflection;
using Crpg.Module.Common.Models;
using NUnit.Framework;

namespace Crpg.Module.UTest.MissileBounce;

[TestFixture]
public class PureMaterialFactorTests
{
    private static MethodInfo? _computeMaterialFactorMethod;

    [OneTimeSetUp]
    public void Setup()
    {
        _computeMaterialFactorMethod = typeof(PureMissileBounceCalculator)
            .GetMethod("ComputeMaterialFactor", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new Exception("ComputeMaterialFactor method not found");
    }

    [TestCase(PureArmorMaterial.None, 0.0f)]
    [TestCase(PureArmorMaterial.Cloth, 0.0f)]
    [TestCase(PureArmorMaterial.Leather, 0.0f)]
    [TestCase(PureArmorMaterial.Chainmail, 0.20f)]
    [TestCase(PureArmorMaterial.Plate, 1.0f)]
    public void ComputeMaterialFactor_VariousCases(PureArmorMaterial armorMaterial, float expected)
    {
        if (_computeMaterialFactorMethod != null)
        {
            float result = (float)_computeMaterialFactorMethod.Invoke(null, new object[] { armorMaterial })!;
            Console.WriteLine($"ArmorMaterial: {armorMaterial}, Result: {result:F2}");
            Assert.That(result, Is.EqualTo(expected).Within(0.01f));
        }
    }
}
