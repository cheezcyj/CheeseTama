using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheeseTama.Gameplay.Growth
{
    public enum NormalEvolutionVisualPattern
    {
        CreamPearls = 0,
        CheddarFreckles = 1,
        RicottaCurds = 2,
        MozzarellaRibbons = 3,
        BlueMarbling = 4,
        CoffeeSwirl = 5
    }

    public enum NormalEvolutionExpressionHint
    {
        Gentle = 0,
        Bright = 1,
        Shy = 2,
        Balanced = 3,
        Refined = 4,
        Focused = 5
    }

    public enum NormalEvolutionReactionStyle
    {
        SoftBloom = 0,
        EnergeticBounce = 1,
        GentleSway = 2,
        StretchPulse = 3,
        QuietRipple = 4,
        FocusedNod = 5
    }

    public enum EvolutionVisualColorRole
    {
        Pattern = 0,
        Accent = 1,
        Highlight = 2
    }

    /// <summary>
    /// One rounded, renderer-only accent expressed in normalized model bounds.
    /// Cube, plane, and quad primitives are deliberately rejected so a missing
    /// authored mesh can never appear as a rectangular placeholder.
    /// </summary>
    public sealed class EvolutionVisualAccentDefinition
    {
        public EvolutionVisualAccentDefinition(
            string name,
            PrimitiveType primitive,
            Vector3 normalizedPosition,
            Vector3 normalizedScale,
            Vector3 eulerAngles,
            EvolutionVisualColorRole colorRole)
        {
            if (!IsSoftPrimitive(primitive))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(primitive),
                    primitive,
                    "Evolution accents must use Sphere, Capsule, or Cylinder primitives.");
            }

            Name = string.IsNullOrWhiteSpace(name) ? "Evolution Accent" : name.Trim();
            Primitive = primitive;
            NormalizedPosition = normalizedPosition;
            NormalizedScale = new Vector3(
                Mathf.Max(0.01f, Mathf.Abs(normalizedScale.x)),
                Mathf.Max(0.01f, Mathf.Abs(normalizedScale.y)),
                Mathf.Max(0.01f, Mathf.Abs(normalizedScale.z)));
            EulerAngles = eulerAngles;
            ColorRole = colorRole;
        }

        public string Name { get; }
        public PrimitiveType Primitive { get; }
        public Vector3 NormalizedPosition { get; }
        public Vector3 NormalizedScale { get; }
        public Vector3 EulerAngles { get; }
        public EvolutionVisualColorRole ColorRole { get; }

        public static bool IsSoftPrimitive(PrimitiveType primitive)
        {
            return primitive == PrimitiveType.Sphere
                || primitive == PrimitiveType.Capsule
                || primitive == PrimitiveType.Cylinder;
        }
    }

    public sealed class NormalEvolutionVisualProfile
    {
        private readonly EvolutionVisualAccentDefinition[] accents;

        public NormalEvolutionVisualProfile(
            string evolutionId,
            string displayName,
            Color bodyTint,
            Color patternTint,
            Color accentTint,
            Color highlightTint,
            NormalEvolutionVisualPattern pattern,
            NormalEvolutionExpressionHint expressionHint,
            NormalEvolutionReactionStyle reactionStyle,
            params EvolutionVisualAccentDefinition[] accents)
        {
            if (string.IsNullOrWhiteSpace(evolutionId))
            {
                throw new ArgumentException("A stable evolution id is required.", nameof(evolutionId));
            }

            EvolutionId = evolutionId.Trim();
            DisplayName = displayName ?? string.Empty;
            BodyTint = bodyTint;
            PatternTint = patternTint;
            AccentTint = accentTint;
            HighlightTint = highlightTint;
            Pattern = pattern;
            ExpressionHint = expressionHint;
            ReactionStyle = reactionStyle;
            this.accents = accents ?? Array.Empty<EvolutionVisualAccentDefinition>();
        }

        public string EvolutionId { get; }
        public string DisplayName { get; }
        public Color BodyTint { get; }
        public Color PatternTint { get; }
        public Color AccentTint { get; }
        public Color HighlightTint { get; }
        public NormalEvolutionVisualPattern Pattern { get; }
        public NormalEvolutionExpressionHint ExpressionHint { get; }
        public NormalEvolutionReactionStyle ReactionStyle { get; }
        public IReadOnlyList<EvolutionVisualAccentDefinition> Accents => accents;

        public Color ResolveColor(EvolutionVisualColorRole role)
        {
            return role switch
            {
                EvolutionVisualColorRole.Accent => AccentTint,
                EvolutionVisualColorRole.Highlight => HighlightTint,
                _ => PatternTint
            };
        }
    }

    public readonly struct NormalEvolutionReactionPose
    {
        public NormalEvolutionReactionPose(Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            LocalPosition = localPosition;
            LocalEulerAngles = localEulerAngles;
            LocalScale = localScale;
        }

        public Vector3 LocalPosition { get; }
        public Vector3 LocalEulerAngles { get; }
        public Vector3 LocalScale { get; }
    }

    public static class NormalEvolutionReactionMotion
    {
        public static NormalEvolutionReactionPose Evaluate(
            NormalEvolutionReactionStyle style,
            float normalizedTime)
        {
            var t = Mathf.Clamp01(normalizedTime);
            var arc = Mathf.Sin(t * Mathf.PI);
            var doubleArc = Mathf.Sin(t * Mathf.PI * 2f);
            var position = Vector3.zero;
            var euler = Vector3.zero;
            var scale = Vector3.one;

            switch (style)
            {
                case NormalEvolutionReactionStyle.SoftBloom:
                    position.y = arc * 0.025f;
                    scale = Vector3.one * (1f + arc * 0.12f);
                    break;
                case NormalEvolutionReactionStyle.EnergeticBounce:
                    position.y = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f)) * 0.085f;
                    euler.z = doubleArc * 8f;
                    scale = new Vector3(1f + arc * 0.08f, 1f - arc * 0.05f, 1f);
                    break;
                case NormalEvolutionReactionStyle.GentleSway:
                    position.x = doubleArc * 0.025f;
                    euler.z = doubleArc * 7f;
                    break;
                case NormalEvolutionReactionStyle.StretchPulse:
                    scale = new Vector3(1f - arc * 0.06f, 1f + arc * 0.18f, 1f - arc * 0.04f);
                    break;
                case NormalEvolutionReactionStyle.QuietRipple:
                    euler.y = doubleArc * 9f;
                    scale = Vector3.one * (1f + arc * 0.07f);
                    break;
                case NormalEvolutionReactionStyle.FocusedNod:
                    position.y = -arc * 0.018f;
                    euler.x = arc * 11f;
                    scale = new Vector3(1f, 1f - arc * 0.04f, 1f + arc * 0.03f);
                    break;
            }

            return new NormalEvolutionReactionPose(position, euler, scale);
        }
    }
}
