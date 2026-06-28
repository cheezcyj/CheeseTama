namespace CheeseTama.Gameplay.Care
{
    public readonly struct CareActionResult
    {
        public readonly bool success;
        public readonly bool hatched;
        public readonly string message;

        public CareActionResult(bool success, bool hatched, string message)
        {
            this.success = success;
            this.hatched = hatched;
            this.message = message;
        }
    }
}

