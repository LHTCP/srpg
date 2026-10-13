using NUnit.Framework;
using UnityEngine;

[Category("SrpM1All")]
public class SrpRuntimeMaterialTests
{
    [Test]
    public void RuntimePrimitive_색을_적용하면_URP호환_머티리얼로_교체된다()
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            var renderer = cube.GetComponent<Renderer>();
            var expected = new Color(0.25f, 0.5f, 0.75f, 1f);

            SrpRuntimeMaterial.ApplyColor(renderer, expected);

            Assert.IsNotNull(renderer.sharedMaterial);
            Assert.IsNotNull(renderer.sharedMaterial.shader);
            Assert.AreNotEqual("Standard", renderer.sharedMaterial.shader.name);
            Assert.IsTrue(SrpRuntimeMaterial.TryGetColor(renderer.sharedMaterial, out var actual));
            Assert.AreEqual(expected.r, actual.r, 0.001f);
            Assert.AreEqual(expected.g, actual.g, 0.001f);
            Assert.AreEqual(expected.b, actual.b, 0.001f);
            Assert.AreEqual(expected.a, actual.a, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(cube);
        }
    }
}
