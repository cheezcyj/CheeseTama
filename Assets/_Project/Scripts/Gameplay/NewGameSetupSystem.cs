using CheeseTama.Save;

namespace CheeseTama.Gameplay.NewGameSetup
{
    public static class NewGameSetupSystem
    {
        public static bool TrySelectEgg(
            NewGameSetupSaveData state,
            string eggId,
            out string errorMessage)
        {
            if (!TryPrepareIncompleteState(state, out errorMessage))
            {
                return false;
            }

            if (state.currentStep != NewGameSetupStep.EggSelection)
            {
                errorMessage = "알 선택 단계에서만 알을 바꿀 수 있어요.";
                return false;
            }

            if (!NewGameSetupCatalog.TryGetEgg(eggId, out _))
            {
                errorMessage = "선택할 수 없는 알이에요.";
                return false;
            }

            if (state.selectedEggId == eggId)
            {
                return false;
            }

            state.selectedEggId = eggId;
            return true;
        }

        public static bool TrySelectFirstMilk(
            NewGameSetupSaveData state,
            string milkId,
            out string errorMessage)
        {
            if (!TryPrepareIncompleteState(state, out errorMessage))
            {
                return false;
            }

            if (state.currentStep != NewGameSetupStep.FirstMilkSelection)
            {
                errorMessage = "첫 우유 선택 단계에서만 우유를 바꿀 수 있어요.";
                return false;
            }

            if (!NewGameSetupCatalog.TryGetFirstMilk(milkId, out _))
            {
                errorMessage = "선택할 수 없는 첫 우유예요.";
                return false;
            }

            if (state.selectedFirstMilkId == milkId)
            {
                return false;
            }

            state.selectedFirstMilkId = milkId;
            return true;
        }

        public static bool TryAdvance(
            NewGameSetupSaveData state,
            out string errorMessage)
        {
            if (!TryPrepareIncompleteState(state, out errorMessage))
            {
                return false;
            }

            if (state.currentStep == NewGameSetupStep.EggSelection)
            {
                if (!NewGameSetupCatalog.TryGetEgg(state.selectedEggId, out _))
                {
                    errorMessage = "함께할 알을 먼저 골라 주세요.";
                    return false;
                }

                state.currentStep = NewGameSetupStep.FirstMilkSelection;
                return true;
            }

            if (state.currentStep != NewGameSetupStep.FirstMilkSelection)
            {
                errorMessage = "새 게임 설정 단계를 확인할 수 없어요.";
                return false;
            }

            if (!NewGameSetupCatalog.TryGetFirstMilk(state.selectedFirstMilkId, out _))
            {
                errorMessage = "첫 우유를 골라 주세요.";
                return false;
            }

            if (!NewGameSetupCatalog.TryCreateTemperamentSeed(
                    state.selectedEggId,
                    state.selectedFirstMilkId,
                    out var seed))
            {
                errorMessage = "선택한 내용으로 초기 성향을 만들 수 없어요.";
                return false;
            }

            state.temperamentSeed = seed;
            state.currentStep = NewGameSetupStep.Complete;
            state.completed = true;
            state.skipped = false;
            state.legacySuppressed = false;
            return true;
        }

        public static bool TryGoBack(
            NewGameSetupSaveData state,
            out string errorMessage)
        {
            if (!TryPrepareIncompleteState(state, out errorMessage))
            {
                return false;
            }

            if (state.currentStep != NewGameSetupStep.FirstMilkSelection)
            {
                return false;
            }

            state.currentStep = NewGameSetupStep.EggSelection;
            return true;
        }

        public static bool TrySkip(
            NewGameSetupSaveData state,
            out string errorMessage)
        {
            if (!TryPrepareIncompleteState(state, out errorMessage))
            {
                return false;
            }

            state.selectedEggId = string.Empty;
            state.selectedFirstMilkId = string.Empty;
            state.temperamentSeed = NewGameSetupCatalog.CreateNeutralSeed(
                NewGameSetupCatalog.SkippedSeedKey);
            state.currentStep = NewGameSetupStep.Complete;
            state.completed = true;
            state.skipped = true;
            state.legacySuppressed = false;
            return true;
        }

        public static bool CanAdvance(NewGameSetupSaveData state)
        {
            if (state == null)
            {
                return false;
            }

            state.EnsureRuntimeDefaults();
            if (state.completed)
            {
                return false;
            }

            return state.currentStep == NewGameSetupStep.EggSelection
                ? NewGameSetupCatalog.TryGetEgg(state.selectedEggId, out _)
                : state.currentStep == NewGameSetupStep.FirstMilkSelection
                    && NewGameSetupCatalog.TryGetFirstMilk(state.selectedFirstMilkId, out _);
        }

        private static bool TryPrepareIncompleteState(
            NewGameSetupSaveData state,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (state == null)
            {
                errorMessage = "새 게임 설정 정보를 불러오지 못했어요.";
                return false;
            }

            state.EnsureRuntimeDefaults();
            if (!state.completed)
            {
                return true;
            }

            errorMessage = "새 게임 설정이 이미 끝났어요.";
            return false;
        }
    }
}
