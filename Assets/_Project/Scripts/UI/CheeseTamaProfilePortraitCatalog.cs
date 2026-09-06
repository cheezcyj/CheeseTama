using System.Collections.Generic;
using CheeseTama.Gameplay.Growth;
using UnityEngine;

namespace CheeseTama.UI
{
    public static class CheeseTamaProfilePortraitCatalog
    {
        public const string ResourceRoot = "UI/ProfilePortraits";

        private const float PixelsPerUnit = 100f;

        private static readonly Dictionary<CheeseTamaGrowthStage, Sprite> Portraits =
            new Dictionary<CheeseTamaGrowthStage, Sprite>();

        public static Sprite LoadPortrait(CheeseTamaGrowthStage stage)
        {
            if (Portraits.TryGetValue(stage, out var cachedPortrait) && cachedPortrait != null)
            {
                return cachedPortrait;
            }

            var texture = Resources.Load<Texture2D>(GetResourcePath(stage));
            if (texture == null || texture.width <= 0 || texture.height <= 0)
            {
                return null;
            }

            var portrait = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            portrait.name = $"cheesetama_profile_{stage.ToString().ToLowerInvariant()}_v001";
            Portraits[stage] = portrait;
            return portrait;
        }

        public static string GetResourcePath(CheeseTamaGrowthStage stage)
        {
            return $"{ResourceRoot}/cheesetama_profile_{ResolveStageSlug(stage)}_v001";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeCache()
        {
            Portraits.Clear();
        }

        private static string ResolveStageSlug(CheeseTamaGrowthStage stage)
        {
            switch (stage)
            {
                case CheeseTamaGrowthStage.Hatchling:
                    return "hatchling";
                case CheeseTamaGrowthStage.Soft:
                    return "soft";
                case CheeseTamaGrowthStage.Grown:
                    return "grown";
                case CheeseTamaGrowthStage.Mature:
                    return "mature";
                case CheeseTamaGrowthStage.Final:
                    return "final";
                default:
                    return "egg";
            }
        }
    }
}
