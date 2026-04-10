using System.IO;
using AutobattlerSample.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AutobattlerSample.Editor
{
    public static class SampleContentCreator
    {
        [MenuItem("Tools/Autobattler Sample/Create Sample Scene")]
        public static void CreateSampleScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f);

            // Game bootstrap — all content is generated procedurally at runtime
            var systems = new GameObject("Systems");
            var bootstrap = systems.AddComponent<GameBootstrap>();
            bootstrap.Floors = 6;
            bootstrap.Width = 3;
            bootstrap.Seed = 0; // 0 = random seed each run

            // Event system
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();

            const string scenePath = "Assets/Scenes/AutobattlerSampleScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log("Autobattler sample scene created! Press Play to start. All encounters generated procedurally.");
        }
    }
}
