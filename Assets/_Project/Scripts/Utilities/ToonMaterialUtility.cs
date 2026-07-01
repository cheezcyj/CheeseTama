using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

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

            if (!Application.isPlaying)
            {
                ApplyEditModeMaterial(renderer, profile, color);
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

            if (objectName == "Body"
                || objectName.Contains("Soft Arm")
                || objectName.Contains("Little Foot")
                || objectName.Contains("Top Curl")
                || objectName.Contains("Crown"))
            {
                return ToonMaterialProfile.CharacterBody;
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

        private static void ApplyEditModeMaterial(Renderer renderer, ToonMaterialProfile profile, Color color)
        {
#if UNITY_EDITOR
            renderer.sharedMaterial = GetOrCreateEditorMaterial(profile, color);
            renderer.SetPropertyBlock(null);
#else
            var material = renderer.sharedMaterial;
            if (material == null || !material.name.StartsWith($"M_Scene_{profile}_") || material.hideFlags == HideFlags.DontSave)
            {
                material = new Material(FindLitShader())
                {
                    name = $"M_Scene_{profile}_{renderer.name}",
                    hideFlags = HideFlags.None
                };
                ConfigureMaterial(material, profile);
                renderer.sharedMaterial = material;
            }

            SetMaterialColors(material, profile, color);
            renderer.SetPropertyBlock(null);
#endif
        }

#if UNITY_EDITOR
        private static Material GetOrCreateEditorMaterial(ToonMaterialProfile profile, Color color)
        {
            EnsureEditorMaterialFolder();
            var colorKey = ColorUtility.ToHtmlStringRGBA(color);
            var path = $"Assets/_Project/Art/GeneratedMaterials/M_Scene_{profile}_{colorKey}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(FindLitShader())
                {
                    name = $"M_Scene_{profile}_{colorKey}",
                    hideFlags = HideFlags.None
                };
                ConfigureMaterial(material, profile);
                SetMaterialColors(material, profile, color);
                AssetDatabase.CreateAsset(material, path);
                return material;
            }

            ConfigureMaterial(material, profile);
            SetMaterialColors(material, profile, color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureEditorMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Art");
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/GeneratedMaterials"))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art", "GeneratedMaterials");
            }
        }
#endif

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
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                return Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                    ?? Shader.Find("Standard")
                    ?? Shader.Find("Sprites/Default");
            }

            return Shader.Find("Standard")
                ?? Shader.Find("Legacy Shaders/Diffuse")
                ?? Shader.Find("Universal Render Pipeline/Lit")
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

        private static void SetMaterialColors(Material material, ToonMaterialProfile profile, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty(BaseColorId))
            {
                material.SetColor(BaseColorId, color);
            }

            if (material.HasProperty(ColorId))
            {
                material.SetColor(ColorId, color);
            }

            if (material.HasProperty(EmissionColorId))
            {
                material.SetColor(EmissionColorId, GetEmissionColor(profile, color));
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
                ToonMaterialProfile.EnvironmentGlow => color * 0.1f,
                ToonMaterialProfile.CharacterHighlight => color * 0.06f,
                _ => Color.black
            };
        }
    }
}
