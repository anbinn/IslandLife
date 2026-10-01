using UnityEngine;

namespace IslandLife.Core
{
    /// <summary>
    /// Entry point of the IslandLife runtime.
    /// Placed on a bootstrap object in the first scene, it initializes the
    /// core layer before handing control to <see cref="GameManager"/>.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        /// <summary>
        /// When true this object removes itself after initialization so the
        /// bootstrap step does not keep a permanent scene object alive.
        /// </summary>
        [SerializeField]
        private bool destroyAfterInit = true;

        private void Awake()
        {
            // Persist across scene loads: the game state layer owns the
            // lifetime of the application, individual scenes do not.
            DontDestroyOnLoad(gameObject);

            GameManager.Instance.Initialize();

            if (destroyAfterInit)
            {
                Destroy(gameObject);
            }
        }
    }
}