using UnityEngine;

/// <summary>
/// 런타임에 생성하는 primitive가 Built-in 기본 머티리얼에 기대지 않도록 SRP 호환 머티리얼을 만든다.
/// Unity 기본 primitive의 Standard 머티리얼은 URP 빌드에서 보라색으로 렌더링될 수 있다.
/// </summary>
public static class SrpRuntimeMaterial
{
    const string UrpUnlitShaderName = "Universal Render Pipeline/Unlit";
    const string BuiltInUnlitShaderName = "Unlit/Color";
    const string SpritesDefaultShaderName = "Sprites/Default";

    public static Material CreateUnlit(Color color)
    {
        var shader = FindRuntimeShader();
        if (shader == null)
            throw new System.InvalidOperationException("SRPG 런타임 머티리얼에 사용할 Unlit shader를 찾지 못했습니다.");

        var material = new Material(shader)
        {
            name = "SrpRuntimeUnlit",
        };
        ConfigureSurface(material, color);
        SetColor(material, color);
        return material;
    }

    public static void ApplyColor(Renderer renderer, Color color)
    {
        if (renderer == null)
            return;

        if (!IsRuntimeMaterial(renderer.sharedMaterial))
            renderer.sharedMaterial = CreateUnlit(color);

        ConfigureSurface(renderer.sharedMaterial, color);
        SetColor(renderer.sharedMaterial, color);
    }

    public static bool TryGetColor(Material material, out Color color)
    {
        color = default;
        if (material == null)
            return false;
        if (material.HasProperty("_BaseColor"))
        {
            color = material.GetColor("_BaseColor");
            return true;
        }
        if (material.HasProperty("_Color"))
        {
            color = material.GetColor("_Color");
            return true;
        }
        return false;
    }

    static Shader FindRuntimeShader()
    {
        return Shader.Find(UrpUnlitShaderName)
            ?? Shader.Find(BuiltInUnlitShaderName)
            ?? Shader.Find(SpritesDefaultShaderName);
    }

    static bool IsRuntimeMaterial(Material material)
    {
        if (material == null || material.shader == null)
            return false;

        var shaderName = material.shader.name;
        return shaderName == UrpUnlitShaderName
            || shaderName == BuiltInUnlitShaderName
            || shaderName == SpritesDefaultShaderName;
    }

    static void ConfigureSurface(Material material, Color color)
    {
        if (material == null)
            return;

        bool transparent = color.a < 0.999f;
        material.renderQueue = transparent ? 3100 : -1;

        // URP Unlit transparency는 alpha만으로 켜지지 않으므로 런타임 material 생성 시 함께 설정한다.
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", transparent ? 1f : 0f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", transparent ? (float)UnityEngine.Rendering.BlendMode.SrcAlpha : (float)UnityEngine.Rendering.BlendMode.One);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", transparent ? (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : (float)UnityEngine.Rendering.BlendMode.Zero);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", transparent ? 0f : 1f);

        if (transparent)
        {
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
        }
        else
        {
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
    }

    static void SetColor(Material material, Color color)
    {
        if (material == null)
            return;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }
}
