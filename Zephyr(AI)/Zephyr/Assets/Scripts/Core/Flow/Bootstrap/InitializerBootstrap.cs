using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zephyr.Core.Flow
{
    [DefaultExecutionOrder(-1000)]
    public sealed class InitializerBootstrap : MonoBehaviour
    {
        [SerializeField] private SceneSO _persistentScene;

        private IEnumerator Start()
        {
            if (_persistentScene == null || !_persistentScene.IsValid)
            {
                Debug.LogError("InitializerBootstrap: Persistent SceneSO is missing or invalid.", this);
                yield break;
            }

            Scene persistent = SceneManager.GetSceneByName(_persistentScene.SceneName);
            if (!persistent.IsValid() || !persistent.isLoaded)
            {
                AsyncOperation load = SceneManager.LoadSceneAsync(_persistentScene.LoadKey, LoadSceneMode.Additive);
                if (load == null)
                {
                    Debug.LogError("InitializerBootstrap: Unity could not start loading Persistent.", this);
                    yield break;
                }

                yield return load;
            }

            yield return new WaitUntil(() => PersistentBootstrap.IsReady);

            if (GameFlow.Instance == null)
            {
                Debug.LogError("InitializerBootstrap: Persistent is ready but GameFlow is missing.", this);
                yield break;
            }

            GameFlow.Instance.RequestTransition(GameState.MainMenu);
        }
    }
}
