using CheeseTama.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CheeseTama.Editor
{
    [InitializeOnLoad]
    internal static class CheeseTamaScenePreviewSynchronizer
    {
        private static bool syncScheduled;

        static CheeseTamaScenePreviewSynchronizer()
        {
            EditorSceneManager.sceneOpened -= HandleSceneOpened;
            EditorSceneManager.sceneOpened += HandleSceneOpened;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            ScheduleSync();
        }

        private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ScheduleSync();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                ScheduleSync();
            }
        }

        private static void ScheduleSync()
        {
            if (syncScheduled || Application.isPlaying)
            {
                return;
            }

            syncScheduled = true;
            EditorApplication.delayCall += SyncActiveScene;
        }

        private static void SyncActiveScene()
        {
            syncScheduled = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || Application.isPlaying)
            {
                ScheduleSync();
                return;
            }

            StarterSceneBuilder.SyncEditorScenePreview();
        }
    }
}
