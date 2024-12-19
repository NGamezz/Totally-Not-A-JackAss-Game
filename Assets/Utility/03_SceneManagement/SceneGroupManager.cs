using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utility.SceneManagement
{
    public class SceneGroupManager
    {
        public event Action<string> OnSceneLoaded = delegate { };
        public event Action<string> OnSceneUnloaded = delegate { };
        public event Action OnSceneGroupLoaded = delegate { };

        public SceneGroup ActiveSceneGroup { get; private set; }

        public async UniTask LoadScene(SceneGroup group, IProgress<float> progress,
            bool reloadDuplicateScenes = false)
        {
            ActiveSceneGroup = group;

            await UnloadScenes();

            var sceneCount = SceneManager.sceneCount;
            var loadedScenes = new List<string>();

            for (var i = 0; i < sceneCount; ++i)
            {
                loadedScenes.Add(SceneManager.GetSceneAt(i).name);
            }

            var totalScenesToAdd = ActiveSceneGroup.scenes.Count;
            var operationGroup = new AsyncOperationGroup(totalScenesToAdd);

            for (var i = 0; i < totalScenesToAdd; ++i)
            {
                var sceneData = group.scenes[i];
                if (!reloadDuplicateScenes && loadedScenes.Contains(sceneData.SceneName))
                    continue;

                var operation = SceneManager.LoadSceneAsync(sceneData.reference.Path, LoadSceneMode.Additive);
                operationGroup.Operations.Add(operation);

                OnSceneLoaded?.Invoke(sceneData.SceneName);
            }

            while (!operationGroup.IsDone)
            {
                progress?.Report(operationGroup.Progress);
                await UniTask.Delay(100);
            }

            var activeScene = SceneManager.GetSceneByName(ActiveSceneGroup.FindSceneNameByType(SceneType.ActiveScene));

            if (activeScene.IsValid())
            {
                SceneManager.SetActiveScene(activeScene);
            }

            OnSceneGroupLoaded?.Invoke();
        }

        public async UniTask UnloadScenes()
        {
            var scenes = new List<string>();
            var activeScene = SceneManager.GetActiveScene().name;

            var sceneCount = SceneManager.sceneCount;

            for (var i = sceneCount - 1; i >= 0; --i)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (!scene.isLoaded)
                    continue;

                var sceneName = scene.name;
                if (sceneName.Equals(activeScene) || sceneName.Equals("Bootstrapper"))
                    continue;
                
                scenes.Add(sceneName);
            }
            
            var operationGroup = new AsyncOperationGroup(scenes.Count);

            foreach (var scene in scenes)
            {
                var operation = SceneManager.UnloadSceneAsync(scene);
                if (operation == null)
                    continue;
                
                operationGroup.Operations.Add(operation);
                OnSceneUnloaded.Invoke(scene);
            }

            while (!operationGroup.IsDone)
            {
                await UniTask.Delay(100);
            }
            
            await Resources.UnloadUnusedAssets();
        }
    }
}