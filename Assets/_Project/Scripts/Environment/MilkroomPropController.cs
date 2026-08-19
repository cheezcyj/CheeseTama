using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheeseTama.Environment
{
    public sealed class MilkroomPropController : MonoBehaviour
    {
        [SerializeField] private Transform backgroundRoot;
        [SerializeField] private Transform midgroundRoot;
        [SerializeField] private Transform playAreaRoot;
        [SerializeField] private Transform foregroundRoot;
        [SerializeField] private Transform themeVfxRoot;

        private readonly Dictionary<MilkroomPropRoute, MilkroomPropInteraction> interactions =
            new Dictionary<MilkroomPropRoute, MilkroomPropInteraction>();
        private Func<MilkroomPropRoute, bool> routeCallback;
        private Func<bool> blockerCallback;

        public Transform BackgroundRoot => backgroundRoot;
        public Transform MidgroundRoot => midgroundRoot;
        public Transform PlayAreaRoot => playAreaRoot;
        public Transform ForegroundRoot => foregroundRoot;
        public Transform ThemeVfxRoot => themeVfxRoot;
        public int RegisteredInteractionCount => interactions.Count;

        public void Configure(
            Transform background,
            Transform midground,
            Transform playArea,
            Transform foreground,
            Transform themeVfx)
        {
            backgroundRoot = background;
            midgroundRoot = midground;
            playAreaRoot = playArea;
            foregroundRoot = foreground;
            themeVfxRoot = themeVfx;
        }

        /// <summary>
        /// Binds the room-owned route authority. Repeated calls replace callbacks instead
        /// of adding listeners, so existing-scene repair can safely configure this again.
        /// </summary>
        public void ConfigureInteractionRouting(
            Func<MilkroomPropRoute, bool> tryOpenRoute,
            Func<bool> isInteractionBlocked)
        {
            routeCallback = tryOpenRoute;
            blockerCallback = isInteractionBlocked;
        }

        /// <summary>
        /// Adds or repairs the interaction on a prop root and registers it for keyboard
        /// route activation. Renderer and collider discovery happens only during configure.
        /// </summary>
        public MilkroomPropInteraction ConfigureInteraction(
            Transform propRoot,
            MilkroomPropRoute route,
            Collider interactionCollider = null,
            Renderer[] highlightRenderers = null)
        {
            if (propRoot == null || !MilkroomPropInteraction.IsSupportedRoute(route))
            {
                return null;
            }

            var interaction = propRoot.GetComponent<MilkroomPropInteraction>();
            if (interaction == null)
            {
                interaction = propRoot.gameObject.AddComponent<MilkroomPropInteraction>();
            }

            RemoveRegistrationsFor(interaction);
            if (interactions.TryGetValue(route, out var previous)
                && previous != null
                && previous != interaction)
            {
                previous.Unconfigure();
            }

            interaction.Configure(
                route,
                InvokeRoute,
                IsInteractionBlocked,
                interactionCollider,
                highlightRenderers);
            interactions[route] = interaction;
            return interaction;
        }

        /// <summary>
        /// Keyboard and controller adapters can call the same route contract without
        /// synthesizing a mouse event. The route remains available if a visual prop is
        /// temporarily absent, while focus presentation requires a registered prop.
        /// </summary>
        public bool TryActivateRoute(MilkroomPropRoute route)
        {
            if (!MilkroomPropInteraction.IsSupportedRoute(route) || IsInteractionBlocked())
            {
                return false;
            }

            return InvokeRoute(route);
        }

        public bool SetRouteFocused(MilkroomPropRoute route, bool focused)
        {
            if (!interactions.TryGetValue(route, out var interaction) || interaction == null)
            {
                return false;
            }

            interaction.SetKeyboardFocus(focused);
            return interaction.IsKeyboardFocused == focused;
        }

        public MilkroomPropInteraction GetInteraction(MilkroomPropRoute route)
        {
            return interactions.TryGetValue(route, out var interaction) ? interaction : null;
        }

        public void RefreshInteractionBlockingState()
        {
            foreach (var interaction in interactions.Values)
            {
                interaction?.RefreshBlockingState();
            }
        }

        private bool InvokeRoute(MilkroomPropRoute route)
        {
            return MilkroomPropInteraction.IsSupportedRoute(route)
                && routeCallback != null
                && routeCallback(route);
        }

        private bool IsInteractionBlocked()
        {
            return blockerCallback != null && blockerCallback();
        }

        private void RemoveRegistrationsFor(MilkroomPropInteraction interaction)
        {
            MilkroomPropRoute staleRoute = MilkroomPropRoute.None;
            foreach (var pair in interactions)
            {
                if (pair.Value == interaction)
                {
                    staleRoute = pair.Key;
                    break;
                }
            }

            if (staleRoute != MilkroomPropRoute.None)
            {
                interactions.Remove(staleRoute);
            }
        }
    }
}
