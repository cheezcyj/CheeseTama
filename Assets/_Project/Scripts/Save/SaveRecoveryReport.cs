namespace CheeseTama.Save
{
    public enum SaveRecoveryOutcome
    {
        None = 0,
        RecoveredFromTemporaryFile = 1,
        RecoveredFromBackup = 2,
        CreatedFreshSaveAfterCorruption = 3
    }

    public sealed class SaveRecoveryReport
    {
        public static SaveRecoveryReport NoRecovery { get; } =
            new SaveRecoveryReport(SaveRecoveryOutcome.None, 0);

        internal SaveRecoveryReport(SaveRecoveryOutcome outcome, int quarantinedFileCount)
        {
            Outcome = outcome;
            QuarantinedFileCount = quarantinedFileCount < 0 ? 0 : quarantinedFileCount;
        }

        public SaveRecoveryOutcome Outcome { get; }

        public int QuarantinedFileCount { get; }

        public bool RecoveredExistingData => Outcome == SaveRecoveryOutcome.RecoveredFromTemporaryFile
            || Outcome == SaveRecoveryOutcome.RecoveredFromBackup;

        public bool UserNotificationRecommended => Outcome != SaveRecoveryOutcome.None;
    }
}
