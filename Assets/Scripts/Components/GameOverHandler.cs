using UnityEngine;
using Zenject;

namespace Components
{
    public class GameOverHandler : MonoBehaviour
    {
        public void TriggerGameOver()
        {
            Debug.Log("Game Over");
        }
    }
}