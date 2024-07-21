using Components.UI;
using Events;
using UnityEngine.SceneManagement;


public class TryAgainBTN : UIBTN
{
    protected override void OnClick()
    {
        LoadMainScene();
        MainMenuUIEvents.TryAgainBTN?.Invoke();
    }

    private void LoadMainScene()
    {
        SceneManager.LoadScene("Login");
    }

}

