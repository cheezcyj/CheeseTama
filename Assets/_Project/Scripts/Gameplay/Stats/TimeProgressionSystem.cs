using System;
using CheeseTama.Utilities;

namespace CheeseTama.Gameplay.Stats
{
    public sealed class TimeProgressionSystem
    {
        public void ApplyOfflineProgress(CheeseTamaModel tama, DateTimeOffset now)
        {
            if (tama == null || tama.stats == null)
            {
                return;
            }

            var lastSaved = TimeUtility.ParseOrDefault(tama.lastSavedAtIso, now);
            var minutes = Math.Max(0, (now - lastSaved).TotalMinutes);
            var careTicks = (int)(minutes / 60);

            if (careTicks <= 0)
            {
                return;
            }

            tama.stats.hunger -= careTicks * 4;
            tama.stats.mood -= careTicks * 2;
            tama.stats.cleanliness -= careTicks * 2;
            tama.stats.sleepiness += careTicks * 3;

            if (tama.stats.hunger <= 10 || tama.stats.cleanliness <= 10)
            {
                tama.stats.health -= careTicks * 2;
            }

            tama.stats.ClampAll();
            tama.lastSavedAtIso = now.ToString("O");
        }
    }
}

