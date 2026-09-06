using System.Collections.Generic;

namespace CheeseTama.Collections
{
    public enum CollectionRecordCategory
    {
        Milk,
        Evolution,
        Event,
        Hidden
    }

    public sealed class CollectionSystem
    {
        public void RegisterMilk(CollectionSaveData collections, string milkId)
        {
            if (collections == null)
            {
                return;
            }

            collections.EnsureRuntimeDefaults();
            AddUnique(collections.milk, milkId);
        }

        public void RegisterEvolution(CollectionSaveData collections, string evolutionId)
        {
            if (collections == null)
            {
                return;
            }

            collections.EnsureRuntimeDefaults();
            AddUnique(collections.evolution, evolutionId);
        }

        public void RegisterEvent(CollectionSaveData collections, string eventId)
        {
            if (collections == null)
            {
                return;
            }

            collections.EnsureRuntimeDefaults();
            AddUnique(collections.events, eventId);
        }

        public bool IsFragmentRewardClaimed(
            CollectionSaveData collections,
            CollectionRecordCategory category,
            string recordId)
        {
            if (collections == null || string.IsNullOrWhiteSpace(recordId))
            {
                return false;
            }

            collections.EnsureRuntimeDefaults();
            return collections.claimedFragmentRewardKeys.Contains(BuildFragmentRewardKey(category, recordId));
        }

        public bool TryClaimFragmentReward(
            CollectionSaveData collections,
            CollectionRecordCategory category,
            string recordId)
        {
            if (collections == null || string.IsNullOrWhiteSpace(recordId))
            {
                return false;
            }

            collections.EnsureRuntimeDefaults();
            if (!IsRecordDiscovered(collections, category, recordId)
                || IsFragmentRewardClaimed(collections, category, recordId))
            {
                return false;
            }

            collections.claimedFragmentRewardKeys.Add(BuildFragmentRewardKey(category, recordId));
            return true;
        }

        public int ClaimAllFragmentRewards(CollectionSaveData collections, int maximumRewards)
        {
            if (collections == null || maximumRewards <= 0)
            {
                return 0;
            }

            collections.EnsureRuntimeDefaults();
            var claimedCount = 0;
            claimedCount += ClaimFragmentRewards(
                collections,
                CollectionRecordCategory.Milk,
                maximumRewards - claimedCount);
            claimedCount += ClaimFragmentRewards(
                collections,
                CollectionRecordCategory.Evolution,
                maximumRewards - claimedCount);
            claimedCount += ClaimFragmentRewards(
                collections,
                CollectionRecordCategory.Event,
                maximumRewards - claimedCount);
            claimedCount += ClaimFragmentRewards(
                collections,
                CollectionRecordCategory.Hidden,
                maximumRewards - claimedCount);

            return claimedCount;
        }

        public int ClaimFragmentRewards(
            CollectionSaveData collections,
            CollectionRecordCategory category,
            int maximumRewards)
        {
            if (collections == null || maximumRewards <= 0)
            {
                return 0;
            }

            collections.EnsureRuntimeDefaults();
            if (category == CollectionRecordCategory.Milk)
            {
                return ClaimRewardsFromRecords(collections, category, collections.milk, maximumRewards);
            }

            if (category == CollectionRecordCategory.Evolution)
            {
                return ClaimRewardsFromRecords(collections, category, collections.evolution, maximumRewards);
            }

            if (category == CollectionRecordCategory.Event)
            {
                return ClaimRewardsFromRecords(collections, category, collections.events, maximumRewards);
            }

            if (category != CollectionRecordCategory.Hidden)
            {
                return 0;
            }

            var claimedCount = 0;
            foreach (var entry in collections.hiddenUnlockedOnly)
            {
                if (claimedCount >= maximumRewards)
                {
                    break;
                }

                if (entry != null && TryClaimFragmentReward(collections, category, entry.id))
                {
                    claimedCount += 1;
                }
            }

            return claimedCount;
        }

        public int CountUnclaimedFragmentRewards(CollectionSaveData collections)
        {
            return CountUnclaimedFragmentRewards(collections, CollectionRecordCategory.Milk)
                + CountUnclaimedFragmentRewards(collections, CollectionRecordCategory.Evolution)
                + CountUnclaimedFragmentRewards(collections, CollectionRecordCategory.Event)
                + CountUnclaimedFragmentRewards(collections, CollectionRecordCategory.Hidden);
        }

        public int CountUnclaimedFragmentRewards(
            CollectionSaveData collections,
            CollectionRecordCategory category)
        {
            if (collections == null)
            {
                return 0;
            }

            collections.EnsureRuntimeDefaults();
            if (category == CollectionRecordCategory.Milk)
            {
                return CountUnclaimedRecords(collections, category, collections.milk);
            }

            if (category == CollectionRecordCategory.Evolution)
            {
                return CountUnclaimedRecords(collections, category, collections.evolution);
            }

            if (category == CollectionRecordCategory.Event)
            {
                return CountUnclaimedRecords(collections, category, collections.events);
            }

            if (category != CollectionRecordCategory.Hidden)
            {
                return 0;
            }

            var count = 0;
            var visitedIds = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var entry in collections.hiddenUnlockedOnly)
            {
                if (entry != null
                    && !string.IsNullOrWhiteSpace(entry.id)
                    && visitedIds.Add(entry.id)
                    && !IsFragmentRewardClaimed(collections, category, entry.id))
                {
                    count += 1;
                }
            }

            return count;
        }

        public int CountDiscoveredRecords(CollectionSaveData collections)
        {
            if (collections == null)
            {
                return 0;
            }

            collections.EnsureRuntimeDefaults();
            return collections.milk.Count
                + collections.evolution.Count
                + collections.events.Count
                + collections.hiddenUnlockedOnly.Count;
        }

        private int ClaimRewardsFromRecords(
            CollectionSaveData collections,
            CollectionRecordCategory category,
            IList<string> records,
            int maximumRewards)
        {
            if (records == null || maximumRewards <= 0)
            {
                return 0;
            }

            var claimedCount = 0;
            foreach (var recordId in records)
            {
                if (claimedCount >= maximumRewards)
                {
                    break;
                }

                if (TryClaimFragmentReward(collections, category, recordId))
                {
                    claimedCount += 1;
                }
            }

            return claimedCount;
        }

        private int CountUnclaimedRecords(
            CollectionSaveData collections,
            CollectionRecordCategory category,
            IList<string> records)
        {
            if (records == null)
            {
                return 0;
            }

            var count = 0;
            var visitedIds = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var recordId in records)
            {
                if (!string.IsNullOrWhiteSpace(recordId)
                    && visitedIds.Add(recordId)
                    && !IsFragmentRewardClaimed(collections, category, recordId))
                {
                    count += 1;
                }
            }

            return count;
        }

        private static bool IsRecordDiscovered(
            CollectionSaveData collections,
            CollectionRecordCategory category,
            string recordId)
        {
            if (category == CollectionRecordCategory.Milk)
            {
                return collections.milk.Contains(recordId);
            }

            if (category == CollectionRecordCategory.Evolution)
            {
                return collections.evolution.Contains(recordId);
            }

            if (category == CollectionRecordCategory.Event)
            {
                return collections.events.Contains(recordId);
            }

            if (category != CollectionRecordCategory.Hidden)
            {
                return false;
            }

            foreach (var entry in collections.hiddenUnlockedOnly)
            {
                if (entry != null && entry.id == recordId)
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildFragmentRewardKey(CollectionRecordCategory category, string recordId)
        {
            var categoryKey = category switch
            {
                CollectionRecordCategory.Evolution => "evolution",
                CollectionRecordCategory.Event => "event",
                CollectionRecordCategory.Hidden => "hidden",
                _ => "milk"
            };
            return $"{categoryKey}:{recordId}";
        }

        private static void AddUnique(ICollection<string> target, string id)
        {
            if (target == null || string.IsNullOrWhiteSpace(id) || target.Contains(id))
            {
                return;
            }

            target.Add(id);
        }
    }
}
