using UnityEngine;

namespace CheeseTama.Data
{
    [CreateAssetMenu(menuName = "CheeseTama/환경/밀크룸 테마 정의")]
    public sealed class MilkroomThemeDefinition : ScriptableObject
    {
        [Header("식별 정보")]
        public string id = "milkroom_morning";
        public string displayName = "아침 밀크룸";

        [Header("해금")]
        public bool hideFromThemeSelectUntilUnlocked;
        public bool isHiddenTheme;

        [Header("색상")]
        public Color wallColor = new Color(0.82f, 0.61f, 0.42f);
        public Color floorColor = new Color(0.58f, 0.34f, 0.18f);
        public Color rugColor = new Color(0.92f, 0.82f, 0.63f);
        public Color windowColor = new Color(0.64f, 0.83f, 0.95f);
        public Color glowColor = new Color(1f, 0.82f, 0.48f);
        public Color propAccentColor = new Color(1f, 0.72f, 0.18f);

        [Header("조명")]
        public Color ambientColor = new Color(0.86f, 0.74f, 0.56f);
        public Color cameraBackgroundColor = new Color(0.96f, 0.92f, 0.84f);
    }
}
