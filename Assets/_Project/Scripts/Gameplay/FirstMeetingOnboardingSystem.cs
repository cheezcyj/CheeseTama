using CheeseTama.Save;

namespace CheeseTama.Gameplay
{
    public enum FirstMeetingOnboardingSignal
    {
        Continue,
        MilkFeedSucceeded,
        CareSucceeded,
        CollectionOpened,
        Skip
    }

    public static class FirstMeetingOnboardingSystem
    {
        public static bool TryApply(
            CheeseTamaSaveData saveData,
            FirstMeetingOnboardingSignal signal,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (saveData == null)
            {
                errorMessage = "저장 데이터를 불러오지 못했습니다.";
                return false;
            }

            saveData.EnsureRuntimeDefaults();
            var onboarding = saveData.onboarding;
            if (onboarding.completed)
            {
                return false;
            }

            if (signal == FirstMeetingOnboardingSignal.Skip)
            {
                Complete(onboarding, true);
                return true;
            }

            switch (onboarding.currentStep)
            {
                case FirstMeetingOnboardingStep.Welcome:
                    if (signal != FirstMeetingOnboardingSignal.Continue)
                    {
                        return false;
                    }

                    onboarding.currentStep = FirstMeetingOnboardingStep.FeedMilk;
                    return true;

                case FirstMeetingOnboardingStep.FeedMilk:
                    if (signal != FirstMeetingOnboardingSignal.MilkFeedSucceeded)
                    {
                        return false;
                    }

                    onboarding.currentStep = FirstMeetingOnboardingStep.Care;
                    return true;

                case FirstMeetingOnboardingStep.Care:
                    if (signal != FirstMeetingOnboardingSignal.CareSucceeded)
                    {
                        return false;
                    }

                    onboarding.currentStep = FirstMeetingOnboardingStep.Collection;
                    return true;

                case FirstMeetingOnboardingStep.Collection:
                    if (signal != FirstMeetingOnboardingSignal.CollectionOpened)
                    {
                        return false;
                    }

                    Complete(onboarding, false);
                    return true;

                default:
                    return false;
            }
        }

        public static bool StartReplay(CheeseTamaSaveData saveData)
        {
            if (saveData == null)
            {
                return false;
            }

            saveData.EnsureRuntimeDefaults();
            var onboarding = saveData.onboarding;
            onboarding.currentStep = FirstMeetingOnboardingStep.Welcome;
            onboarding.completed = false;
            onboarding.skipped = false;
            onboarding.replaying = true;
            onboarding.schemaVersion = OnboardingSaveData.CurrentSchemaVersion;
            return true;
        }

        private static void Complete(OnboardingSaveData onboarding, bool skipped)
        {
            onboarding.currentStep = FirstMeetingOnboardingStep.Complete;
            onboarding.completed = true;
            onboarding.skipped = skipped;
            onboarding.replaying = false;
        }
    }
}
