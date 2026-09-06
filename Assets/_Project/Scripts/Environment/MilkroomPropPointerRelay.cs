using UnityEngine;

namespace CheeseTama.Environment
{
    [DisallowMultipleComponent]
    internal sealed class MilkroomPropPointerRelay : MonoBehaviour
    {
        [SerializeField] private MeshCollider targetCollider;

        private MilkroomPropInteraction owner;

        internal MeshCollider Configure(MilkroomPropInteraction targetOwner, Mesh targetMesh)
        {
            owner = targetOwner;
            if (targetCollider == null)
            {
                targetCollider = gameObject.AddComponent<MeshCollider>();
            }

            // Reuse the cooked mesh when reconfiguring the same prop. Clearing an
            // already-disabled collider first can leave its native mesh unbound.
            if (targetCollider.sharedMesh != targetMesh)
            {
                targetCollider.sharedMesh = targetMesh;
            }
            targetCollider.convex = false;
            targetCollider.isTrigger = false;
            targetCollider.enabled = true;
            enabled = true;
            return targetCollider;
        }

        internal void Deactivate(MilkroomPropInteraction targetOwner)
        {
            if (owner != targetOwner)
            {
                return;
            }

            if (owner != null)
            {
                owner.NotifyPrecisePointerExit(this);
            }

            owner = null;
            if (targetCollider != null)
            {
                targetCollider.enabled = false;
            }

            enabled = false;
        }

        private void OnMouseEnter()
        {
            if (owner != null)
            {
                owner.NotifyPrecisePointerEnter(this);
            }
        }

        private void OnMouseExit()
        {
            if (owner != null)
            {
                owner.NotifyPrecisePointerExit(this);
            }
        }

        private void OnMouseDown()
        {
            if (owner != null)
            {
                owner.NotifyPrecisePointerDown();
            }
        }

        private void OnMouseUpAsButton()
        {
            if (owner != null)
            {
                owner.NotifyPrecisePointerUpAsButton();
            }
        }

        private void OnDisable()
        {
            if (owner != null)
            {
                owner.NotifyPrecisePointerExit(this);
            }
        }
    }
}
