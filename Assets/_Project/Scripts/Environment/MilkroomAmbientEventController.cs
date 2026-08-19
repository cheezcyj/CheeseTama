using UnityEngine;

namespace CheeseTama.Environment
{
    public sealed class MilkroomAmbientEventController : MonoBehaviour
    {
        [SerializeField] private Transform themeVfxRoot;
        [SerializeField] private string activeThemeId = MilkroomThemeController.MorningThemeId;

        public void Configure(Transform root)
        {
            themeVfxRoot = root;
            SetTheme(activeThemeId);
        }

        public void SetTheme(string themeId)
        {
            activeThemeId = themeId;
            if (themeVfxRoot == null)
            {
                return;
            }

            var rainy = themeId == MilkroomThemeController.RainyThemeId;
            var night = themeId == MilkroomThemeController.NightThemeId;
            var evening = themeId == MilkroomThemeController.EveningThemeId;
            var starlight = themeId == MilkroomThemeController.StarlightThemeId;
            var winter = themeId == MilkroomThemeController.WinterThemeId;
            var vintage = themeId == MilkroomThemeController.VintageThemeId;

            foreach (Transform child in themeVfxRoot)
            {
                var childName = child.name;
                child.gameObject.SetActive(
                    childName.Contains("Rain") && rainy ||
                    childName.Contains("Night") && (night || starlight) ||
                    childName.Contains("Starlight") && starlight ||
                    childName.Contains("Evening") && evening ||
                    childName.Contains("Winter") && winter ||
                    childName.Contains("Vintage") && vintage);
            }
        }
    }
}
