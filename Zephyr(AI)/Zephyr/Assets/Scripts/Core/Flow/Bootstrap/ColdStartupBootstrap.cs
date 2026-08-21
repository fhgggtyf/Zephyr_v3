using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zephyr.Core.Flow
{
    /// <summary>
    /// Optional scene-local bootstrap for opening a scene directly from the editor.
    /// It only fills missing additive dependencies and destroys itself afterwards.
    /// When the normal Initializer/Persistent flow is already running this component
    /// has no effect.
    /// </summary>
    [DefaultExecutionOrder(-950)]
    public sealed class ColdStartupBootstrap : MonoBehaviour
    {
        private static ColdStartupBootstrap s_owner;

        [SerializeField] private SceneSO _persistentScene;
        [SerializeField] private SceneSO _gameManagerScene;
        [SerializeField] private bool _loadGameManager = true;

        private IEnumerator Start()
        {
            if (s_owner != null)
            {
                Destroy(gameObject);
                yield break;
            }

            s_owner = this;
            yield return EnsureSceneLoaded(_persistentScene, "Persistent");
            if (PersistentBootstrap.IsReady && _loadGameManager)
                yield return EnsureSceneLoaded(_gameManagerScene, "GameManager");

            s_owner = null;
            Destroy(gameObject);
        }

        private static IEnumerator EnsureSceneLoaded(SceneSO scene, string role)
        {
            if (scene == null || !scene.IsValid)
            {
                Debug.LogError($"ColdStartupBootstrap: {role} SceneSO is missing or invalid.");
                yield break;
            }

            Scene loaded = SceneManager.GetSceneByName(scene.SceneName);
            if (loaded.IsValid() && loaded.isLoaded)
            {
                if (scene.Id == SceneId.Persistent)
                    yield return new WaitUntil(() => PersistentBootstrap.IsReady);
                else if (scene.Id == SceneId.GameManager)
                    yield return new WaitUntil(() => GameManagerBootstrap.IsReady);
                yield break;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(scene.LoadKey, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError($"ColdStartupBootstrap: Unity could not load {role} scene.");
                yield break;
            }

            yield return operation;
            if (scene.Id == SceneId.Persistent)
                yield return new WaitUntil(() => PersistentBootstrap.IsReady);
            else if (scene.Id == SceneId.GameManager)
                yield return new WaitUntil(() => GameManagerBootstrap.IsReady);
        }

        private void OnDestroy()
        {
            if (s_owner == this) s_owner = null;
        }
    }
}
