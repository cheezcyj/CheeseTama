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
            EditorApplication.update -= SyncActiveScene;
            EditorApplication.update += SyncActiveScene;
        }

        private static void SyncActiveScene()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating
                || StarterSceneBuilder.DeferAutomaticEditorPreview)
            {
                return;
            }

            EditorApplication.update -= SyncActiveScene;
            syncScheduled = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (StarterSceneBuilder.SyncEditorScenePreview())
            {
                EditorApplication.QueuePlayerLoopUpdate();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }
    }
}
