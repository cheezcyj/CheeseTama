using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Utilities;
using UnityEngine;

namespace CheeseTama.UI
{
    [DisallowMultipleComponent]
    public sealed class EmmentalConstellationPresenter : MonoBehaviour
    {
        public const int HoleCount = 7;
        private const string RootObjectName = "Emmental Constellation Visual";

        [SerializeField] private Transform characterVisualRoot;
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.12f, -0.5f);
        [SerializeField] private float constellationScale = 1f;

        private Transform generatedRoot;
        private Transform[] holes;
        private bool visible;

        public bool IsVisible => visible;
        public int VisibleHoleCount
        {
            get
            {
                if (!visible)
                {
                    return 0;
                }

                var count = 0;
                if (holes == null)
                {
                    return count;
                }

                for (var index = 0; index < holes.Length; index += 1)
                {
                    if (holes[index] != null && holes[index].gameObject.activeInHierarchy)
                    {
                        count += 1;
                    }
                }

                return count;
            }
        }

        public void Configure(Transform visualRoot, float scale = 1f)
        {
            characterVisualRoot = visualRoot;
            constellationScale = Mathf.Max(0.1f, scale);
            RebuildIfNeeded();
        }

        private void Awake()
        {
            RebuildIfNeeded();
            SetVisible(false);
        }

        public void Bind(CheeseTamaModel tama)
        {
            RebuildIfNeeded();
            SetVisible(StarEggEmmentalEvolutionSystem.IsEmmental(tama));
        }

        public void SetVisible(bool shouldShow)
        {
            visible = shouldShow;
            if (generatedRoot != null && generatedRoot.gameObject.activeSelf != shouldShow)
            {
                generatedRoot.gameObject.SetActive(shouldShow);
            }
        }

        private void RebuildIfNeeded()
        {
            var parent = characterVisualRoot != null ? characterVisualRoot : transform;
            if (generatedRoot != null && generatedRoot.parent != parent)
            {
                generatedRoot.SetParent(parent, false);
            }

            generatedRoot ??= parent.Find(RootObjectName);
            if (generatedRoot == null)
            {
                var rootObject = new GameObject(RootObjectName);
                generatedRoot = rootObject.transform;
                generatedRoot.SetParent(parent, false);
            }

            generatedRoot.localPosition = localOffset;
            generatedRoot.localRotation = Quaternion.identity;
            generatedRoot.localScale = Vector3.one * constellationScale;

            holes = new Transform[HoleCount];
            for (var index = 0; index < HoleCount; index += 1)
            {
                holes[index] = EnsureHole(generatedRoot, index);
            }
        }

        private static Transform EnsureHole(Transform parent, int index)
        {
            var objectName = $"Emmental Star Hole {index + 1}";
            var existing = parent.Find(objectName);
            if (existing == null)
            {
                var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                primitive.name = objectName;
                existing = primitive.transform;
                existing.SetParent(parent, false);
                var collider = primitive.GetComponent<Collider>();
                if (collider != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(collider);
                    }
                    else
                    {
                        DestroyImmediate(collider);
                    }
                }
            }

            var positions = GetConstellationPositions();
            var scales = GetHoleScales();
            existing.localPosition = positions[index];
            existing.localRotation = Quaternion.identity;
            existing.localScale = Vector3.one * scales[index];
            var renderer = existing.GetComponent<Renderer>();
            ToonMaterialUtility.Apply(
                renderer,
                index == HoleCount - 1
                    ? ToonMaterialProfile.CharacterHighlight
                    : ToonMaterialProfile.CharacterMark,
                index == HoleCount - 1
                    ? new Color(1f, 0.88f, 0.32f, 1f)
                    : new Color(0.64f, 0.38f, 0.13f, 1f));
            return existing;
        }

        private static Vector3[] GetConstellationPositions()
        {
            return new[]
            {
                new Vector3(-0.23f, 0.24f, 0f),
                new Vector3(0.05f, 0.31f, 0.01f),
                new Vector3(0.25f, 0.15f, 0f),
                new Vector3(-0.09f, 0.05f, -0.01f),
                new Vector3(0.18f, -0.09f, 0f),
                new Vector3(-0.24f, -0.18f, 0.01f),
                new Vector3(0.01f, -0.27f, -0.01f)
            };
        }

        private static float[] GetHoleScales()
        {
            return new[] { 0.095f, 0.07f, 0.085f, 0.065f, 0.08f, 0.07f, 0.105f };
        }
    }
}
