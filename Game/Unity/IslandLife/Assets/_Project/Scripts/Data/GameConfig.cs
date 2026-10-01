using UnityEngine;

namespace IslandLife.Data
{
    /// <summary>
    /// Global configuration values for the game.
    /// Create a single instance as an asset and assign it to
    /// <see cref="Config"/> during bootstrap, so every system reads the same
    /// values without hard coded constants.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameConfig",
        menuName = "IslandLife/Data/Game Config")]
    public class GameConfig : ScriptableObject
    {
        /// <summary>
        /// Active configuration. Assigned by the bootstrap step; may be null
        /// when no config asset has been provided yet.
        /// </summary>
        public static GameConfig Config { get; set; }

        /// <summary>
        /// Target frame rate the game aims for on mobile devices.
        /// </summary>
        [SerializeField]
        private int targetFrameRate = 60;

        /// <summary>
        /// Active configuration is used as the runtime settings source.
        /// </summary>
        private void OnEnable()
        {
            ApplySettings();
        }

        /// <summary>
        /// Applies the configuration to engine level settings.
        /// </summary>
        public void ApplySettings()
        {
            Application.targetFrameRate = targetFrameRate;
        }

        /// <summary>
        /// Returns the configured target frame rate.
        /// </summary>
        public int GetTargetFrameRate()
        {
            return targetFrameRate;
        }
    }
}