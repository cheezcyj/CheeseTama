using System;
using CheeseTama.Environment;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Decorations
{
    public enum MilkroomThemeUnlockFailure
    {
        None = 0,
        InvalidTheme = 1,
        RouteLocked = 2,
        InsufficientStarDrops = 3,
        AlreadyOwned = 4
    }

    public readonly struct MilkroomThemeUnlockResult
    {
        public MilkroomThemeUnlockResult(
            bool succeeded,
            MilkroomThemeUnlockFailure failure,
            string themeId,
            int spentStarDrops,
            int remainingStarDrops,
            string message)
        {
            Succeeded = succeeded;
            Failure = failure;
            ThemeId = themeId ?? string.Empty;
            SpentStarDrops = Math.Max(0, spentStarDrops);
            RemainingStarDrops = Math.Max(0, remainingStarDrops);
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }
        public MilkroomThemeUnlockFailure Failure { get; }
        public string ThemeId { get; }
        public int SpentStarDrops { get; }
        public int RemainingStarDrops { get; }
        public string Message { get; }
    }

    public sealed class MilkroomThemeUnlockSystem
    {
        public bool IsVisible(CheeseTamaSaveData saveData, string themeId)
        {
            var definition = MilkroomThemeCatalog.Find(themeId);
            if (definition == null)
            {
                return false;
            }

            return !definition.RequiresStarRoute || saveData?.unlocks?.starMilkUnlocked == true;
        }

        public bool IsOwned(CheeseTamaSaveData saveData, string themeId)
        {
            var definition = MilkroomThemeCatalog.Find(themeId);
            if (definition == null)
            {
                return false;
            }

            if (definition.IsOwnedByDefault)
            {
                return true;
            }

            saveData?.EnsureRuntimeDefaults();
            var ownedThemeIds = saveData?.decorations?.ownedThemeIds;
            if (ownedThemeIds == null)
            {
                return false;
            }

            for (var index = 0; index < ownedThemeIds.Count; index += 1)
            {
                if (string.Equals(ownedThemeIds[index], definition.Id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public MilkroomThemeUnlockResult TryUnlock(CheeseTamaSaveData saveData, string themeId)
        {
            var definition = MilkroomThemeCatalog.Find(themeId);
            if (saveData == null || definition == null)
            {
                return new MilkroomThemeUnlockResult(
                    false,
                    MilkroomThemeUnlockFailure.InvalidTheme,
                    string.Empty,
                    0,
                    saveData?.economy?.starDrops ?? 0,
                    "선택한 테마를 찾을 수 없습니다.");
            }

            saveData.EnsureRuntimeDefaults();
            if (!IsVisible(saveData, definition.Id))
            {
                return new MilkroomThemeUnlockResult(
                    false,
                    MilkroomThemeUnlockFailure.RouteLocked,
                    definition.Id,
                    0,
                    saveData.economy.starDrops,
                    "아직 이 테마의 흔적을 발견하지 못했습니다.");
            }

            if (IsOwned(saveData, definition.Id))
            {
                return new MilkroomThemeUnlockResult(
                    false,
                    MilkroomThemeUnlockFailure.AlreadyOwned,
                    definition.Id,
                    0,
                    saveData.economy.starDrops,
                    "이미 보유한 테마입니다.");
            }

            if (saveData.economy.starDrops < definition.StarDropCost)
            {
                return new MilkroomThemeUnlockResult(
                    false,
                    MilkroomThemeUnlockFailure.InsufficientStarDrops,
                    definition.Id,
                    0,
                    saveData.economy.starDrops,
                    $"별방울이 {definition.StarDropCost - saveData.economy.starDrops}개 부족합니다.");
            }

            saveData.economy.starDrops -= definition.StarDropCost;
            saveData.decorations.ownedThemeIds.Add(definition.Id);
            return new MilkroomThemeUnlockResult(
                true,
                MilkroomThemeUnlockFailure.None,
                definition.Id,
                definition.StarDropCost,
                saveData.economy.starDrops,
                $"{definition.DisplayName}을(를) 해금했습니다.");
        }
    }
}
