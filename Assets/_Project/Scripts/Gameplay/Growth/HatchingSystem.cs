namespace CheeseTama.Gameplay.Growth
{
    public sealed class HatchingSystem
    {
        public const int HatchLevel = 10;
        private const int ProgressPerLevel = 100;

        public bool TryHatch(CheeseTamaModel tama)
        {
            if (tama == null || tama.isHatched || tama.level < HatchLevel)
            {
                return false;
            }

            tama.isHatched = true;
            tama.form = "soft_cheesetama";
            return true;
        }

        public static int GetHatchProgressPercent(CheeseTamaModel tama)
        {
            if (tama == null)
            {
                return 0;
            }

            if (tama.isHatched || tama.level >= HatchLevel)
            {
                return 100;
            }

            var targetProgress = (HatchLevel - 1) * ProgressPerLevel;
            var currentProgress = ((tama.level - 1) * ProgressPerLevel) + tama.levelProgress;
            return ClampToPercent((currentProgress * 100) / targetProgress);
        }

        private static int ClampToPercent(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > 100 ? 100 : value;
        }
    }
}
