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

        public Transform BackgroundRoot => backgroundRoot;
        public Transform MidgroundRoot => midgroundRoot;
        public Transform PlayAreaRoot => playAreaRoot;
        public Transform ForegroundRoot => foregroundRoot;
        public Transform ThemeVfxRoot => themeVfxRoot;

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
    }
}
