using UnityEngine;

namespace Utils
{
    public abstract class EventListenerMono : MonoBehaviour
    {
        
            private void Start()
            {
                RegisterEvents();
            }

            protected void OnEnable()
            {
                RegisterEvents();
            }

            protected void OnDisable()
            {
                UnRegisterEvents();
            }

            protected abstract void RegisterEvents();
            protected abstract void UnRegisterEvents();
        }
    
}