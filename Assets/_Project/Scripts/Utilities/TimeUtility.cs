using System;

namespace CheeseTama.Utilities
{
    public static class TimeUtility
    {
        public static string NowIso()
        {
            return DateTimeOffset.Now.ToString("O");
        }

        public static DateTimeOffset ParseOrDefault(string value, DateTimeOffset fallback)
        {
            return DateTimeOffset.TryParse(value, out var parsed) ? parsed : fallback;
        }
    }
}

