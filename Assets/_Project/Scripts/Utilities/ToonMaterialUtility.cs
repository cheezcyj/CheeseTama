using System.Collections.Generic;
using UnityEngine;

namespace CheeseTama.Utilities
{
    public enum ToonMaterialProfile
    {
        CharacterBody,
        CharacterFace,
        CharacterHighlight,
        CharacterShadow,
        CharacterMark,
        EnvironmentMatte,
        EnvironmentWood,
        EnvironmentGlass,
        EnvironmentGlow,
        UI
    }

    public static class ToonMaterialUtility
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int SpecColorId = Shader.PropertyToID("_SpecColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private static readonly Dictionary<ToonMaterialProfile, Material> Materials = new();
        private static MaterialPropertyBlock propertyBlock;

        public static void Apply(Renderer renderer, ToonMaterialProfile profile, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.sharedMaterial = GetMaterial(profile);
            propertyBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            propertyBlock.SetColor(EmissionColorId, GetEmissionColor(profile, color));
            renderer.SetPropertyBlock(propertyBlock);
        }

        public static ToonMaterialProfile InferProfile(Renderer renderer)
        {
            if (renderer == null)
            {
                return ToonMaterialProfile.EnvironmentMatte;
            }

            var objectName = renderer.name;
            if (objectName.Contains("Eye") || objectName.Contains("Mouth") || objectName.Contains("Cheek") || objectName.Contains("Face"))
            {
                return ToonMaterialProfile.CharacterFace;
            }

            if (objectName.Contains("Highlight") || objectName.Contains("Sparkle"))
            {
                return ToonMaterialProfile.CharacterHighlight;
            }

            if (objectName.Contains("Shadow") || objectName.Contains("Outline"))
            {
                return ToonMaterialProfile.CharacterShadow;
            }

            if (objectName.Contains("Hole") || objectName.Contains("Speckle") || objectName.Contains("Mark"))
            {
                return ToonMaterialProfile.CharacterMark;
            }

            if (objectName.Contains("Glass") || objectName.Contains("Bottle") || objectName.Contains("Window Sky"))
            {
                return ToonMaterialProfile.EnvironmentGlass;
            }

            if (objectName.Contains("Glow") || objectName.Contains("Lamp") || objectName.Contains("Star") || objectName.Contains("Sun"))
            {
                return ToonMaterialProfile.EnvironmentGlow;
            }

            if (objectName.Contains("Wood") || objectName.Contains("Floor") || objectName.Contains("Shelf") || objectName.Contains("Dresser") || objectName.Contains("Chair") || objectName.Contains("Table") || objectName.Contains("Frame"))
            {
                return ToonMaterialProfile.EnvironmentWood;
            }

            return ToonMaterialProfile.EnvironmentMatte;
        }

        private static Material GetMaterial(ToonMaterialProfile profile)
        {
            if (Materials.TryGetValue(profile, out var material) && material != null)
            {
                return material;
            }

            material = new Material(FindLitShader())
            {
                name = $"M_Runtime_{profile}_Toon",
                hideFlags = HideFlags.DontSave
            };

            ConfigureMaterial(material, profile);
            Materials[profile] = material;
            return material;
        }

        private static Shader FindLitShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");
        }

        private static void ConfigureMaterial(Material material, ToonMaterialProfile profile)
        {
            if (material.HasProperty(MetallicId))
            {
                material.SetFloat(MetallicId, 0f);
            }

            if (material.HasProperty(SmoothnessId))
            {
                material.SetFloat(SmoothnessId, GetSmoothness(profile));
            }

            if (material.HasProperty(SpecColorId))
            {
                material.SetColor(SpecColorId, GetSpecularColor(profile));
            }

            if (profile == ToonMaterialProfile.EnvironmentGlow || profile == ToonMaterialProfile.CharacterHighlight)
            {
                material.EnableKeyword("_EMISSION");
            }
        }

        private static float GetSmoothness(ToonMaterialProfile profile)
        {
            return profile switch
            {
                ToonMaterialProfile.CharacterBody => 0.58f,
                ToonMaterialProfile.CharacterFace => 0.72f,
                ToonMaterialProfile.CharacterHighlight => 0.84f,
                ToonMaterialProfile.CharacterShadow => 0.18f,
                ToonMaterialProfile.CharacterMark => 0.42f,
                ToonMaterialProfile.EnvironmentGlass => 0.62f,
                ToonMaterialProfile.EnvironmentGlow => 0.5f,
                ToonMaterialProfile.EnvironmentWood => 0.24f,
                _ => 0.35f
            };
        }

        private static Color GetSpecularColor(ToonMaterialProfile profile)
        {
            return profile switch
            {
                ToonMaterialProfile.CharacterFace => new Color(0.55f, 0.38f, 0.24f),
                ToonMaterialProfile.CharacterHighlight => new Color(0.85f, 0.78f, 0.58f),
                ToonMaterialProfile.EnvironmentGlass => new Color(0.42f, 0.6f, 0.7f),
                _ => new Color(0.2f, 0.16f, 0.12f)
            };
        }

        private static Color GetEmissionColor(ToonMaterialProfile profile, Color color)
        {
            return profile switch
            {
                ToonMaterialProfile.EnvironmentGlow => color * 0.28f,
                ToonMaterialProfile.CharacterHighlight => color * 0.12f,
                _ => Color.black
            };
        }
    }
}
