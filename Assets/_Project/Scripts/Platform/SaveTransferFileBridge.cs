using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Scripting;

namespace CheeseTama.Platform
{
    /// <summary>
    /// Keeps browser JavaScript and native file-system fallbacks outside save rules.
    /// Browser callbacks target this component by GameObject and method name.
    /// </summary>
    public sealed class SaveTransferFileBridge : MonoBehaviour
    {
        public const string StandaloneImportFileName = "cheesetama-import.ctsave.json";

        private const string BackupDirectoryName = "CheeseTamaBackups";
        private const string ImportCompletedMethodName = nameof(OnBrowserImportCompleted);
        private const string ImportFailedMethodName = nameof(OnBrowserImportFailed);

        private bool importRequestPending;

        public event Action<string> ImportCompleted;
        public event Action<string> ImportFailed;

        public bool TryExport(string envelopeJson, string fileName, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(envelopeJson)
                || string.IsNullOrWhiteSpace(fileName)
                || !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
            {
                message = "백업 파일 이름이나 내용이 올바르지 않습니다.";
                return false;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                if (CheeseTamaDownloadSaveTransfer(envelopeJson, fileName) == 0)
                {
                    message = "브라우저가 백업 다운로드를 시작하지 못했습니다.";
                    return false;
                }

                message = "브라우저 다운로드를 시작했습니다.";
                return true;
            }
            catch (Exception)
            {
                message = "브라우저 백업 다운로드 중 오류가 발생했습니다.";
                return false;
            }
#elif UNITY_EDITOR
            var selectedPath = UnityEditor.EditorUtility.SaveFilePanel(
                "CheeseTama 저장 백업",
                string.Empty,
                fileName,
                "json");
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                message = "백업 파일 저장을 취소했습니다.";
                return false;
            }

            return TryWriteFile(selectedPath, envelopeJson, out message);
#else
            var backupDirectory = GetBackupDirectoryPath();
            var outputPath = Path.Combine(backupDirectory, fileName);
            return TryWriteFile(outputPath, envelopeJson, out message);
#endif
        }

        public void RequestImport()
        {
            if (importRequestPending)
            {
                ImportFailed?.Invoke("이미 백업 파일 선택 창을 열었습니다.");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            importRequestPending = true;
            try
            {
                CheeseTamaPickSaveTransfer(
                    gameObject.name,
                    ImportCompletedMethodName,
                    ImportFailedMethodName,
                    SaveTransferCodec.MaximumEnvelopeBytes);
            }
            catch (Exception)
            {
                importRequestPending = false;
                ImportFailed?.Invoke("브라우저 파일 선택 창을 열지 못했습니다.");
            }
#elif UNITY_EDITOR
            var selectedPath = UnityEditor.EditorUtility.OpenFilePanel(
                "CheeseTama 저장 백업 가져오기",
                string.Empty,
                "json");
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                ImportFailed?.Invoke("백업 파일 선택을 취소했습니다.");
                return;
            }

            ReadSelectedFile(selectedPath);
#else
            var backupDirectory = GetBackupDirectoryPath();
            var importPath = Path.Combine(backupDirectory, StandaloneImportFileName);
            if (!File.Exists(importPath))
            {
                TryOpenBackupDirectory(backupDirectory);
                ImportFailed?.Invoke(
                    $"백업 폴더에 {StandaloneImportFileName} 파일을 넣은 뒤 다시 시도하세요.");
                return;
            }

            ReadSelectedFile(importPath);
#endif
        }

        [Preserve]
        public void OnBrowserImportCompleted(string envelopeJson)
        {
            if (!importRequestPending)
            {
                return;
            }

            importRequestPending = false;
            if (string.IsNullOrWhiteSpace(envelopeJson)
                || envelopeJson.Length > SaveTransferCodec.MaximumEnvelopeBytes
                || Encoding.UTF8.GetByteCount(envelopeJson) > SaveTransferCodec.MaximumEnvelopeBytes)
            {
                ImportFailed?.Invoke("선택한 백업 파일의 용량이 올바르지 않습니다.");
                return;
            }

            ImportCompleted?.Invoke(envelopeJson);
        }

        [Preserve]
        public void OnBrowserImportFailed(string errorMessage)
        {
            if (!importRequestPending)
            {
                return;
            }

            importRequestPending = false;
            ImportFailed?.Invoke(string.IsNullOrWhiteSpace(errorMessage)
                ? "브라우저에서 백업 파일을 읽지 못했습니다."
                : errorMessage);
        }

        private void OnDisable()
        {
            importRequestPending = false;
        }

        private void ReadSelectedFile(string filePath)
        {
            try
            {
                var info = new FileInfo(filePath);
                if (!info.Exists)
                {
                    ImportFailed?.Invoke("선택한 백업 파일을 찾지 못했습니다.");
                    return;
                }

                if (info.Length <= 0L || info.Length > SaveTransferCodec.MaximumEnvelopeBytes)
                {
                    ImportFailed?.Invoke("선택한 백업 파일의 용량이 올바르지 않습니다.");
                    return;
                }

                ImportCompleted?.Invoke(File.ReadAllText(filePath, Encoding.UTF8));
            }
            catch (IOException)
            {
                ImportFailed?.Invoke("선택한 백업 파일을 읽지 못했습니다.");
            }
            catch (UnauthorizedAccessException)
            {
                ImportFailed?.Invoke("선택한 백업 파일에 접근할 수 없습니다.");
            }
            catch (ArgumentException)
            {
                ImportFailed?.Invoke("선택한 백업 파일 경로가 올바르지 않습니다.");
            }
            catch (NotSupportedException)
            {
                ImportFailed?.Invoke("이 환경에서는 선택한 파일을 읽을 수 없습니다.");
            }
        }

        private static bool TryWriteFile(string filePath, string contents, out string message)
        {
            try
            {
                var directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.WriteAllText(filePath, contents, new UTF8Encoding(false));
                message = $"{Path.GetFileName(filePath)} 백업 파일을 저장했습니다.";
                return true;
            }
            catch (IOException)
            {
                message = "백업 파일을 저장하지 못했습니다.";
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                message = "백업 파일을 저장할 권한이 없습니다.";
                return false;
            }
            catch (ArgumentException)
            {
                message = "백업 파일 경로가 올바르지 않습니다.";
                return false;
            }
            catch (NotSupportedException)
            {
                message = "이 환경에서는 백업 파일을 저장할 수 없습니다.";
                return false;
            }
        }

        private static string GetBackupDirectoryPath()
        {
            return Path.Combine(Application.persistentDataPath, BackupDirectoryName);
        }

        private static void TryOpenBackupDirectory(string directoryPath)
        {
            try
            {
                Directory.CreateDirectory(directoryPath);
                Application.OpenURL(new Uri(directoryPath).AbsoluteUri);
            }
            catch (Exception)
            {
                // Folder opening is only a convenience. The import remains untouched.
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int CheeseTamaDownloadSaveTransfer(string contents, string fileName);

        [DllImport("__Internal")]
        private static extern void CheeseTamaPickSaveTransfer(
            string gameObjectName,
            string successMethodName,
            string failureMethodName,
            int maximumBytes);
#endif
    }
}
