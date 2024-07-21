using Events;

using UnityEngine.SceneManagement;


namespace Components.UI
{
    public class PlayBTN: UIBTN
    {
        protected override void OnClick()
        {
            LoadMainScene();
            MainMenuEvents.PlayGameBTN?.Invoke();
        }

        private void LoadMainScene()
        {
            SceneManager.LoadScene("Main");
        }
    }
}