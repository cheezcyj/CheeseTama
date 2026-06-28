namespace CheeseTama.Gameplay.Growth
{
    public sealed class HatchingSystem
    {
        public const int HatchLevel = 10;

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
    }
}

