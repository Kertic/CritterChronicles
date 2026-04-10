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
        [MenuItem("Tools/CritterChronicles Sample/Generate Content & Scene")]
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

            var contentDatabase = ContentAssetCreator.EnsureDefaultContentDatabase();

            // Game bootstrap
            var systems = new GameObject("Systems");
            var bootstrap = systems.AddComponent<GameBootstrap>();
            bootstrap.Floors = 15;
            bootstrap.Width = 5;
            bootstrap.Seed = 0;

            var serializedBootstrap = new SerializedObject(bootstrap);
            serializedBootstrap.FindProperty("contentDatabase").objectReferenceValue = contentDatabase;
            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();

            // Event system
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();

            const string scenePath = "Assets/Scenes/AutobattlerSampleScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log("Autobattler sample scene created with a default content database. Press Play to start.");
        }
    }
}
