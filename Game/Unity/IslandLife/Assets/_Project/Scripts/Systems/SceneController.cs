using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IslandLife.Systems
{
    /// <summary>
    /// Central place for scene transition requests.
    /// Keeps scene loading asynchronous and non blocking, and exposes a small
    /// API so later gameplay systems do not call the SceneManager directly.
    /// </summary>
    public class SceneController : MonoBehaviour
    {
        /// <summary>
        /// Seconds to wait for the fade-in once a scene finished loading.
        /// </summary>
        private const float TransitionDelay = 0.1f;

        /// <summary>
        /// Loads a scene by name and replaces the active one.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("SceneController: scene name is empty.");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        /// <summary>
        /// Reloads the currently active scene.
        /// </summary>
        public void ReloadCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            // Reserve a single async handle so overlapping requests cannot
            // start two loads at the same time.
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

            if (operation == null)
            {
                Debug.LogError($"SceneController: scene '{sceneName}' was not found.");
                yield break;
            }

            operation.allowSceneActivation = true;

            while (!operation.isDone)
            {
                yield return null;
            }

            // Reserved hook for the transition visual used later on.
            yield return new WaitForSeconds(TransitionDelay);
        }
    }
}