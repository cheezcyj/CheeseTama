namespace CheeseTama.Gameplay
{
    public static class CheeseTamaNameSystem
    {
        public const int MaximumNameLength = 12;

        public static bool TryNormalize(
            string requestedName,
            out string normalizedName,
            out string errorMessage)
        {
            normalizedName = requestedName?.Trim() ?? string.Empty;
            if (normalizedName.Length == 0)
            {
                errorMessage = "이름을 한 글자 이상 입력해 주세요.";
                return false;
            }

            if (normalizedName.Length > MaximumNameLength)
            {
                errorMessage = $"이름은 {MaximumNameLength}자까지 지을 수 있습니다.";
                return false;
            }

            for (var i = 0; i < normalizedName.Length; i += 1)
            {
                if (!char.IsControl(normalizedName[i]))
                {
                    continue;
                }

                errorMessage = "이름에는 줄바꿈이나 제어 문자를 사용할 수 없습니다.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
