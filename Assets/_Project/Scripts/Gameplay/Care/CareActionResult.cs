namespace CheeseTama.Gameplay.Care
{
    public readonly struct CareActionResult
    {
        public readonly bool success;
        public readonly bool hatched;
        public readonly bool leveledUp;
        public readonly string message;

        public CareActionResult(bool success, bool hatched, string message, bool leveledUp = false)
        {
            this.success = success;
            this.hatched = hatched;
            this.leveledUp = leveledUp;
            this.message = message;
        }
    }
}
