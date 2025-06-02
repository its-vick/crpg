using System;
using System.Reflection;
using Crpg.Module.Common.Models;
using NUnit.Framework;

namespace Crpg.Module.UTest.MissileBounce;

[TestFixture]
public class PureArmorFactorTests
{
    private static MethodInfo? _computeArmorFactorMethod;

    [OneTimeSetUp]
    public void Setup()
    {
        _computeArmorFactorMethod = typeof(PureMissileBounceCalculator)
            .GetMethod("ComputeArmorFactor", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new Exception("ComputeArmorFactor method not found");
    }

    // expected to begin chance 55
    [TestCase(0f, PureBodyPart.Head, 0.0f)]
    [TestCase(27.5f, PureBodyPart.Head, 0.0f)] // ratio = 0.5
    [TestCase(55f, PureBodyPart.Head, 0.5f)]
    [TestCase(64f, PureBodyPart.Head, 0.73f)]
    [TestCase(110f, PureBodyPart.Head, 1.5f)]
    [TestCase(130f, PureBodyPart.Head, 1.78f)] // > 2x expected

    // expected to begin chance 65
    [TestCase(0f, PureBodyPart.Chest, 0.0f)]
    [TestCase(65f, PureBodyPart.Chest, 0.5f)]
    [TestCase(130f, PureBodyPart.Chest, 1.5f)]

    // expected to begin chance 25
    [TestCase(30f, PureBodyPart.ArmLeft, 0.77f)]
    [TestCase(20f, PureBodyPart.ArmLeft, 0.0f)]
    [TestCase(5f, PureBodyPart.ArmLeft, 0.0f)]

    // expected to begin chance 25
    [TestCase(30f, PureBodyPart.Legs, 0.77f)]
    [TestCase(20f, PureBodyPart.Legs, 0.0f)]
    [TestCase(5f, PureBodyPart.Legs, 0.0f)]
    public void ComputeArmorFactor_VariousCases(float armor, PureBodyPart part, float expected)
    {
        if (_computeArmorFactorMethod != null)
        {
            float result = (float)_computeArmorFactorMethod.Invoke(null, new object[] { armor, part })!;
            Console.WriteLine($"Armor: {armor}, Part: {part}, Result: {result:F2}");
            Assert.That(result, Is.EqualTo(expected).Within(0.01f));
        }
    }
}
