using CheeseTama.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class SceneNavigationButton : MonoBehaviour
    {
        [SerializeField] private string targetSceneName;
        [SerializeField] private bool saveBeforeLoad = true;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            EnsureButtonListener();
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        public void Configure(string sceneName, bool shouldSaveBeforeLoad)
        {
            targetSceneName = sceneName;
            saveBeforeLoad = shouldSaveBeforeLoad;
            EnsureButtonListener();
        }

        private void EnsureButtonListener()
        {
            button ??= GetComponent<Button>();
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                Debug.LogWarning("이동할 씬 이름이 비어 있습니다.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
            {
                Debug.LogWarning($"'{targetSceneName}' 씬이 빌드 설정에 없습니다. CheeseTama > 시작 씬 빌드를 실행하세요.");
                return;
            }

            if (saveBeforeLoad && GameManager.Instance != null)
            {
                GameManager.Instance.SaveGame();
            }

            SceneManager.LoadScene(targetSceneName);
        }
    }
}
