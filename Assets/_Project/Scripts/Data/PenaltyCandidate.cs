using System;

namespace CheeseTama.Data
{
    [Serializable]
    public sealed class PenaltyCandidate
    {
        public string id;
        public string condition;
        public float chance = 0.1f;
    }
}

