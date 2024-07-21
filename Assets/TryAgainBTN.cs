using Components.UI;
using Events;
using UnityEngine.SceneManagement;


public class TryAgainBTN : UIBTN
{
    protected override void OnClick()
    {
        
        MainMenuUIEvents.TryAgainBTN?.Invoke();
    }

  

}

