using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.NpcVisits;
using UnityEngine;

namespace CheeseTama.UI
{
    public static class NpcPortraitCatalog
    {
        public const string MasterResourcePath = "UI/NpcPortraits/npc_portraits_master_v001";

        private const int AtlasColumns = 2;
        private const int AtlasRows = 2;
        private const float PixelsPerUnit = 100f;

        private static readonly Dictionary<string, Sprite> Portraits =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);

        private static Texture2D masterTexture;

        public static Sprite LoadPortrait(string npcId)
        {
            if (!TryResolveCell(npcId, out var column, out var rowFromBottom))
            {
                return null;
            }

            if (Portraits.TryGetValue(npcId, out var cachedPortrait) && cachedPortrait != null)
            {
                return cachedPortrait;
            }

            if (masterTexture == null)
            {
                masterTexture = Resources.Load<Texture2D>(MasterResourcePath);
            }

            if (masterTexture == null
                || masterTexture.width < AtlasColumns
                || masterTexture.height < AtlasRows
                || masterTexture.width % AtlasColumns != 0
                || masterTexture.height % AtlasRows != 0)
            {
                return null;
            }

            var cellWidth = masterTexture.width / AtlasColumns;
            var cellHeight = masterTexture.height / AtlasRows;
            var sourceRect = new Rect(
                column * cellWidth,
                rowFromBottom * cellHeight,
                cellWidth,
                cellHeight);
            var portrait = Sprite.Create(
                masterTexture,
                sourceRect,
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            portrait.name = $"npc_portrait_{npcId}";
            Portraits[npcId] = portrait;
            return portrait;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeCache()
        {
            Portraits.Clear();
            masterTexture = null;
        }

        private static bool TryResolveCell(string npcId, out int column, out int rowFromBottom)
        {
            column = 0;
            rowFromBottom = 0;

            // Unity texture and Sprite rect coordinates start at the bottom-left.
            // The atlas' visual top row is therefore row 1, not row 0.
            if (string.Equals(npcId, NpcVisitSystem.MilkyDoctorId, StringComparison.Ordinal))
            {
                rowFromBottom = 1;
                return true;
            }

            if (string.Equals(npcId, NpcVisitSystem.FermentationFairyId, StringComparison.Ordinal))
            {
                column = 1;
                rowFromBottom = 1;
                return true;
            }

            if (string.Equals(npcId, NpcVisitSystem.MilkCatId, StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(npcId, NpcVisitSystem.StainGuestId, StringComparison.Ordinal))
            {
                column = 1;
                return true;
            }

            return false;
        }
    }
}
