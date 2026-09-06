using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CheeseTama.UI
{
    public sealed class FullScreenPointerDownRelay : MonoBehaviour, IPointerDownHandler
    {
        private Action<PointerEventData> pointerDownHandler;

        public void Configure(Action<PointerEventData> handler)
        {
            pointerDownHandler = handler;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerDownHandler?.Invoke(eventData);
        }
    }
}
