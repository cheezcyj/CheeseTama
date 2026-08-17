using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheeseTama.Gameplay.Growth
{
    /// <summary>
    /// Visitor-facing visual profiles for the six existing normal evolution ids.
    /// Catalog order follows EvolutionSystem's stable tie-break order.
    /// </summary>
    public static class NormalEvolutionVisualCatalog
    {
        private static EvolutionVisualAccentDefinition Accent(
            string name,
            PrimitiveType primitive,
            float x,
            float y,
            float width,
            float height,
            float depth,
            EvolutionVisualColorRole colorRole,
            float rotation = 0f)
        {
            return new EvolutionVisualAccentDefinition(
                name,
                primitive,
                new Vector3(x, y, 0.08f),
                new Vector3(width, height, depth),
                new Vector3(0f, 0f, rotation),
                colorRole);
        }

        public static readonly NormalEvolutionVisualProfile Cream = new NormalEvolutionVisualProfile(
            EvolutionSystem.CreamEvolutionId,
            "크림치즈타마",
            new Color(1f, 0.92f, 0.72f, 1f),
            new Color(1f, 0.76f, 0.62f, 1f),
            new Color(1f, 0.86f, 0.72f, 1f),
            new Color(1f, 0.98f, 0.88f, 1f),
            NormalEvolutionVisualPattern.CreamPearls,
            NormalEvolutionExpressionHint.Gentle,
            NormalEvolutionReactionStyle.SoftBloom,
            Accent("Cream Pearl Left", PrimitiveType.Sphere, -0.23f, 0.34f, 0.13f, 0.13f, 0.06f, EvolutionVisualColorRole.Highlight),
            Accent("Cream Pearl Center", PrimitiveType.Sphere, 0f, 0.43f, 0.15f, 0.15f, 0.07f, EvolutionVisualColorRole.Highlight),
            Accent("Cream Pearl Right", PrimitiveType.Sphere, 0.23f, 0.34f, 0.13f, 0.13f, 0.06f, EvolutionVisualColorRole.Highlight),
            Accent("Cream Blush Left", PrimitiveType.Sphere, -0.42f, -0.03f, 0.12f, 0.08f, 0.035f, EvolutionVisualColorRole.Pattern),
            Accent("Cream Blush Right", PrimitiveType.Sphere, 0.42f, -0.03f, 0.12f, 0.08f, 0.035f, EvolutionVisualColorRole.Pattern));

        public static readonly NormalEvolutionVisualProfile Cheddar = new NormalEvolutionVisualProfile(
            EvolutionSystem.CheddarEvolutionId,
            "체다치즈타마",
            new Color(1f, 0.64f, 0.22f, 1f),
            new Color(0.78f, 0.28f, 0.08f, 1f),
            new Color(1f, 0.82f, 0.22f, 1f),
            new Color(1f, 0.94f, 0.56f, 1f),
            NormalEvolutionVisualPattern.CheddarFreckles,
            NormalEvolutionExpressionHint.Bright,
            NormalEvolutionReactionStyle.EnergeticBounce,
            Accent("Cheddar Freckle A", PrimitiveType.Sphere, -0.36f, 0.25f, 0.12f, 0.11f, 0.045f, EvolutionVisualColorRole.Pattern),
            Accent("Cheddar Freckle B", PrimitiveType.Sphere, 0.31f, 0.2f, 0.16f, 0.13f, 0.05f, EvolutionVisualColorRole.Pattern),
            Accent("Cheddar Freckle C", PrimitiveType.Sphere, -0.2f, -0.28f, 0.1f, 0.09f, 0.04f, EvolutionVisualColorRole.Pattern),
            Accent("Cheddar Freckle D", PrimitiveType.Sphere, 0.4f, -0.2f, 0.09f, 0.08f, 0.035f, EvolutionVisualColorRole.Pattern),
            Accent("Cheddar Play Crest", PrimitiveType.Capsule, 0.02f, 0.46f, 0.09f, 0.2f, 0.065f, EvolutionVisualColorRole.Highlight, -18f));

        public static readonly NormalEvolutionVisualProfile Ricotta = new NormalEvolutionVisualProfile(
            EvolutionSystem.RicottaEvolutionId,
            "리코타치즈타마",
            new Color(0.98f, 0.96f, 0.86f, 1f),
            new Color(0.9f, 0.82f, 0.68f, 1f),
            new Color(1f, 0.89f, 0.72f, 1f),
            Color.white,
            NormalEvolutionVisualPattern.RicottaCurds,
            NormalEvolutionExpressionHint.Shy,
            NormalEvolutionReactionStyle.GentleSway,
            Accent("Ricotta Curd A", PrimitiveType.Sphere, -0.28f, 0.4f, 0.14f, 0.12f, 0.07f, EvolutionVisualColorRole.Highlight),
            Accent("Ricotta Curd B", PrimitiveType.Sphere, -0.08f, 0.47f, 0.12f, 0.14f, 0.065f, EvolutionVisualColorRole.Highlight),
            Accent("Ricotta Curd C", PrimitiveType.Sphere, 0.14f, 0.44f, 0.15f, 0.13f, 0.07f, EvolutionVisualColorRole.Highlight),
            Accent("Ricotta Curd D", PrimitiveType.Sphere, 0.32f, 0.35f, 0.1f, 0.11f, 0.055f, EvolutionVisualColorRole.Highlight),
            Accent("Ricotta Soft Mark", PrimitiveType.Sphere, 0.36f, -0.15f, 0.1f, 0.08f, 0.035f, EvolutionVisualColorRole.Pattern));

        public static readonly NormalEvolutionVisualProfile Mozzarella = new NormalEvolutionVisualProfile(
            EvolutionSystem.MozzarellaEvolutionId,
            "모짜렐라치즈타마",
            new Color(0.88f, 0.95f, 1f, 1f),
            new Color(0.55f, 0.78f, 0.92f, 1f),
            new Color(0.72f, 0.9f, 1f, 1f),
            new Color(0.97f, 1f, 1f, 1f),
            NormalEvolutionVisualPattern.MozzarellaRibbons,
            NormalEvolutionExpressionHint.Balanced,
            NormalEvolutionReactionStyle.StretchPulse,
            Accent("Mozzarella Ribbon Left", PrimitiveType.Capsule, -0.35f, 0.12f, 0.075f, 0.25f, 0.055f, EvolutionVisualColorRole.Accent, 22f),
            Accent("Mozzarella Ribbon Right", PrimitiveType.Capsule, 0.35f, 0.12f, 0.075f, 0.25f, 0.055f, EvolutionVisualColorRole.Accent, -22f),
            Accent("Mozzarella Ribbon Top", PrimitiveType.Capsule, 0f, 0.44f, 0.075f, 0.22f, 0.055f, EvolutionVisualColorRole.Highlight, 90f),
            Accent("Mozzarella Drop", PrimitiveType.Sphere, 0.28f, -0.27f, 0.09f, 0.13f, 0.04f, EvolutionVisualColorRole.Pattern));

        public static readonly NormalEvolutionVisualProfile Blue = new NormalEvolutionVisualProfile(
            EvolutionSystem.BlueEvolutionId,
            "블루치즈타마",
            new Color(0.6f, 0.72f, 0.82f, 1f),
            new Color(0.25f, 0.39f, 0.56f, 1f),
            new Color(0.46f, 0.62f, 0.78f, 1f),
            new Color(0.78f, 0.9f, 0.96f, 1f),
            NormalEvolutionVisualPattern.BlueMarbling,
            NormalEvolutionExpressionHint.Refined,
            NormalEvolutionReactionStyle.QuietRipple,
            Accent("Blue Marble A", PrimitiveType.Capsule, -0.32f, 0.25f, 0.07f, 0.22f, 0.04f, EvolutionVisualColorRole.Pattern, 38f),
            Accent("Blue Marble B", PrimitiveType.Capsule, 0.25f, 0.2f, 0.07f, 0.25f, 0.04f, EvolutionVisualColorRole.Pattern, -42f),
            Accent("Blue Marble C", PrimitiveType.Capsule, -0.15f, -0.27f, 0.06f, 0.2f, 0.035f, EvolutionVisualColorRole.Pattern, -30f),
            Accent("Blue Marble D", PrimitiveType.Sphere, 0.4f, -0.18f, 0.1f, 0.08f, 0.035f, EvolutionVisualColorRole.Accent),
            Accent("Blue Crown Drop", PrimitiveType.Sphere, 0.04f, 0.45f, 0.1f, 0.13f, 0.05f, EvolutionVisualColorRole.Highlight));

        public static readonly NormalEvolutionVisualProfile Coffee = new NormalEvolutionVisualProfile(
            EvolutionSystem.CoffeeEvolutionId,
            "커피치즈타마",
            new Color(0.58f, 0.36f, 0.22f, 1f),
            new Color(0.26f, 0.12f, 0.07f, 1f),
            new Color(0.86f, 0.67f, 0.45f, 1f),
            new Color(1f, 0.9f, 0.7f, 1f),
            NormalEvolutionVisualPattern.CoffeeSwirl,
            NormalEvolutionExpressionHint.Focused,
            NormalEvolutionReactionStyle.FocusedNod,
            Accent("Coffee Bean Left", PrimitiveType.Capsule, -0.31f, 0.18f, 0.1f, 0.18f, 0.055f, EvolutionVisualColorRole.Pattern, 28f),
            Accent("Coffee Bean Right", PrimitiveType.Capsule, 0.34f, -0.1f, 0.1f, 0.18f, 0.055f, EvolutionVisualColorRole.Pattern, -32f),
            Accent("Coffee Cream Swirl A", PrimitiveType.Capsule, -0.08f, 0.43f, 0.07f, 0.2f, 0.05f, EvolutionVisualColorRole.Highlight, 58f),
            Accent("Coffee Cream Swirl B", PrimitiveType.Capsule, 0.11f, 0.42f, 0.065f, 0.18f, 0.045f, EvolutionVisualColorRole.Highlight, -55f));

        private static readonly NormalEvolutionVisualProfile[] Profiles =
        {
            Cream,
            Cheddar,
            Ricotta,
            Mozzarella,
            Blue,
            Coffee
        };

        public static IReadOnlyList<NormalEvolutionVisualProfile> All => Profiles;

        public static NormalEvolutionVisualProfile Find(string evolutionId)
        {
            return TryGet(evolutionId, out var profile) ? profile : null;
        }

        public static bool TryGet(string evolutionId, out NormalEvolutionVisualProfile profile)
        {
            if (!string.IsNullOrWhiteSpace(evolutionId))
            {
                for (var index = 0; index < Profiles.Length; index += 1)
                {
                    if (string.Equals(Profiles[index].EvolutionId, evolutionId.Trim(), StringComparison.Ordinal))
                    {
                        profile = Profiles[index];
                        return true;
                    }
                }
            }

            profile = null;
            return false;
        }
    }
}
