using System.IO;
using AutobattlerSample.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AutobattlerSample.Editor
{
    /// <summary>
    /// Automatically ensures content assets and a valid scene exist before entering Play Mode.
    /// If the current scene has no GameBootstrap the user is prompted to set one up; play is
    /// cancelled while the setup runs, then the user can press Play again.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeAutoSetup
    {
        private const string ScenePath = "Assets/Scenes/AutobattlerSampleScene.unity";

        static PlayModeAutoSetup()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;

            // 1. Ensure all ScriptableObject content assets exist.
            ContentAssetCreator.EnsureDefaultContentDatabase();

            // 2. Check whether the active scene already has a GameBootstrap.
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null)
            {
                // Scene is ready — nothing else to do, play continues normally.
                return;
            }

            // 3. No GameBootstrap found — we need to set up a scene first.
            //    Cancel the current play attempt so we can do so safely.
            EditorApplication.isPlaying = false;

            bool sceneAlreadyExists = File.Exists(ScenePath);

            if (sceneAlreadyExists)
            {
                // Open the pre-existing sample scene.
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                    Debug.Log("[AutoSetup] Opened the Autobattler sample scene. Press Play to start!");
                }
            }
            else
            {
                // Create a brand-new sample scene (also ensures content assets).
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    SampleContentCreator.CreateSampleScene();
                    Debug.Log("[AutoSetup] Created the Autobattler sample scene. Press Play to start!");
                }
            }
        }
    }
}

