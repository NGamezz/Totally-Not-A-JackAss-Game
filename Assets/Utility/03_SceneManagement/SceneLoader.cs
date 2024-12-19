using Cysharp.Threading.Tasks;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.UI;

namespace Utility.SceneManagement
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField, Child] private Image loadingBar;
        [SerializeField, Child] private Canvas loadingCanvas;
        [SerializeField, Child] private Camera loadingCamera;

        [SerializeField] private float fillSpeed = 0.5f;
        [SerializeField] private SceneGroup[] sceneGroups;

        private float _targetProgress;
        private bool _isLoading;

        private readonly SceneGroupManager _sceneGroupManager = new SceneGroupManager();

        private void Awake()
        {
            _sceneGroupManager.OnSceneLoaded += static x => Debug.Log(x);
            _sceneGroupManager.OnSceneUnloaded += static x => Debug.Log(x);
            _sceneGroupManager.OnSceneGroupLoaded += static () => Debug.Log("Loaded Scene Group.");
        }

        private void Update()
        {
            if (!_isLoading) return;

            var currentFillAmount = loadingBar.fillAmount;
            var progressDifference = Mathf.Abs(currentFillAmount - _targetProgress);

            var dynamicFillSpeed = progressDifference * fillSpeed;

            loadingBar.fillAmount = Mathf.Lerp(currentFillAmount, _targetProgress, Time.deltaTime * dynamicFillSpeed);
        }

        // ReSharper disable once Unity.IncorrectMethodSignature
        private async UniTaskVoid Start()
        {
            await LoadSceneGroup(0);
        }

        private async UniTask LoadSceneGroup(int index)
        {
            loadingBar.fillAmount = 0.0f;
            _targetProgress = 1.0f;

            if (index < 0 || index >= sceneGroups.Length)
            {
                Debug.LogError("Invalid Scene Group Index: " + index);
                return;
            }

            var progress = new LoadingProgress();
            progress.OnProgressChanged += target => _targetProgress = Mathf.Max(target, _targetProgress);

            EnableLoadingCanvas();

            await _sceneGroupManager.LoadScene(sceneGroups[index], progress);

            EnableLoadingCanvas(false);
        }

        private void EnableLoadingCanvas(bool enable = true)
        {
            _isLoading = enable;
            loadingCanvas.gameObject.SetActive(enable);
            loadingCamera.gameObject.SetActive(enable);
        }
    }
}