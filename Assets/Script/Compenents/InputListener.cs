using Components;
using Events;
using UnityEngine;
using Zenject;

namespace Compenents
{
    public class InputListener : MonoBehaviour
    {
        [Inject] private InputEvents InputEvents { get; set;}
        [Inject] private Camera Camera { get; set;}


        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseDown();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                HandleMouseUp();
            }
        }


        private void HandleMouseDown()
        {
            Ray inputRay = Camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(inputRay, 100f);

            Tile firstHitTile = null;

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.TryGetComponent(out firstHitTile))
                {
                    break;
                }
            }
            
            InputEvents.MouseDownGrid?.Invoke(firstHitTile, inputRay.origin + inputRay.direction);
            Debug.Log("Mouse down on Grid");    
        }
        
        private void HandleMouseUp()
        {
            Ray inputRay = Camera.ScreenPointToRay(Input.mousePosition);
            
            InputEvents.MouseUpGrid?.Invoke(inputRay.origin + inputRay.direction);
            Debug.Log("Mouse up on grid");
        }

    }
} 
