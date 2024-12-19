using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Eflatun.SceneReference;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utility.Singletons;

namespace Utility.SceneManagement
{
    public class Bootstrapper : PersistentSingleton<Bootstrapper>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static async UniTaskVoid Init()
        {
            Debug.Log("Bootstrapper Initializing...");
            await SceneManager.LoadSceneAsync("Bootstrapper", LoadSceneMode.Single);
        }
    }

    public class LoadingProgress : IProgress<float>
    {
        public event Action<float> OnProgressChanged;

        private const float Ratio = 1.0f;
        private const float InverseRatio = 1.0f / Ratio;

        public void Report(float value)
        {
            OnProgressChanged?.Invoke(value * InverseRatio);
        }
    }

    public readonly struct AsyncOperationGroup
    {
        public readonly List<AsyncOperation> Operations;

        public float Progress => Operations.Count > 0 ? Operations.Average(static x => x.progress) : 0.0f;
        public bool IsDone => Operations.All(static x => x.isDone);

        public AsyncOperationGroup(int initialCapacity = 16)
        {
            Operations = new List<AsyncOperation>(initialCapacity);
        }
    }

    [Serializable]
    public class SceneData
    {
        public SceneReference reference;

        public string SceneName => reference.Name;
        public SceneType sceneType;
    }

    public enum SceneType
    {
        ActiveScene,
        MainMenu,
        UserInterface,
        HUD,
        Cinematic,
        Environment,
        Tooling,
    };

    [Serializable]
    public class SceneGroup
    {
        public string groupName = string.Empty;
        public List<SceneData> scenes = new();

        public async UniTask<string> FindSceneNameByTypeAsync(SceneType sceneType)
        {
            var scene = await scenes.ToUniTaskAsyncEnumerable().FirstOrDefaultAsync(x => x.sceneType == sceneType);
            return scene.SceneName;
        }

        public string FindSceneNameByType(SceneType sceneType)
        {
            return scenes.FirstOrDefault(s => s.sceneType == sceneType)?.SceneName;
        }
    }
}