using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Journey;

namespace CheeseTama.Save
{
    [Serializable]
    public sealed class FirstDayJourneySaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public bool legacySuppressed;
        public bool introShown;
        public bool completed;
        public bool rewardClaimed;
        public string completedAtIso = string.Empty;
        public List<string> completedTaskIds = new List<string>();

        public static FirstDayJourneySaveData CreateForNewPlayer()
        {
            return new FirstDayJourneySaveData();
        }

        public static FirstDayJourneySaveData CreateCompletedForLegacySave()
        {
            return new FirstDayJourneySaveData
            {
                legacySuppressed = true,
                introShown = true,
                completed = true,
                rewardClaimed = true
            };
        }

        public bool EnsureRuntimeDefaults()
        {
            var changed = false;
            if (schemaVersion != CurrentSchemaVersion)
            {
                schemaVersion = CurrentSchemaVersion;
                changed = true;
            }

            if (completedTaskIds == null)
            {
                completedTaskIds = new List<string>();
                changed = true;
            }

            if (completedAtIso == null)
            {
                completedAtIso = string.Empty;
                changed = true;
            }

            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (var index = completedTaskIds.Count - 1; index >= 0; index -= 1)
            {
                var taskId = completedTaskIds[index];
                if (!FirstDayJourneySystem.IsKnownTaskId(taskId) || !unique.Add(taskId))
                {
                    completedTaskIds.RemoveAt(index);
                    changed = true;
                }
            }

            if (legacySuppressed)
            {
                changed |= !introShown || !completed || !rewardClaimed;
                introShown = true;
                completed = true;
                rewardClaimed = true;
                return changed;
            }

            var shouldBeComplete = FirstDayJourneySystem.HasCompletedEveryTask(this);
            if (completed != shouldBeComplete)
            {
                completed = shouldBeComplete;
                changed = true;
            }

            if (!completed && rewardClaimed)
            {
                rewardClaimed = false;
                changed = true;
            }

            return changed;
        }
    }
}
