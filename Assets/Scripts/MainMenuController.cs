using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fearfront.Scripts
{
    /// <summary>
    /// Handles XR-friendly main menu actions such as launching gameplay, toggling sub panels, and exiting.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField]
        private string playSceneName = "LevelScene";

        [SerializeField]
        private GameObject levelsPanel;

        [SerializeField]
        private GameObject settingsPanel;

        /// <summary>
        /// Loads the primary play scene.
        /// </summary>
        public void HandlePlay()
        {
            if (!string.IsNullOrWhiteSpace(playSceneName))
            {
                SceneManager.LoadScene(playSceneName);
            }
        }

        /// <summary>
        /// Shows or hides the levels panel, collapsing the settings panel if needed.
        /// </summary>
        public void HandleLevels()
        {
            TogglePanel(levelsPanel, settingsPanel);
        }

        /// <summary>
        /// Shows or hides the settings panel, collapsing the levels panel if needed.
        /// </summary>
        public void HandleSettings()
        {
            TogglePanel(settingsPanel, levelsPanel);
        }

        /// <summary>
        /// Quits the application (or stops play mode in the editor).
        /// </summary>
        public void HandleQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void TogglePanel(GameObject panelToToggle, GameObject panelToCollapse)
        {
            if (panelToToggle == null)
            {
                return;
            }

            var nextState = !panelToToggle.activeSelf;
            panelToToggle.SetActive(nextState);

            if (nextState && panelToCollapse != null)
            {
                panelToCollapse.SetActive(false);
            }
        }
    }
}

