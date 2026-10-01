using UnityEngine;

namespace IslandLife.Core
{
    /// <summary>
    /// Singleton owner of the global game state layer.
    /// Holds references to long lived systems and provides the application
    /// level lifecycle callbacks. It contains no gameplay behaviour itself;
    /// feature systems register with it during initialization.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        /// <summary>
        /// Current manager instance. Created lazily if no bootstrap object
        /// exists, so tool and test entry points never hit a null reference.
        /// </summary>
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameManager>();
                }

                if (_instance == null)
                {
                    var created = new GameObject(nameof(GameManager));
                    _instance = created.AddComponent<GameManager>();
                }

                return _instance;
            }
        }

        /// <summary>
        /// True once <see cref="Initialize"/> has completed.
        /// </summary>
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            // Enforce a single state layer for the whole application.
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Initializes the core layer. Safe to call more than once; only the
        /// first call performs work.
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            IsInitialized = true;
        }
    }
}