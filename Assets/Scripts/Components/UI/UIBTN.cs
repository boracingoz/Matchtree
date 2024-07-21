using Extensions.Unity.MonoHelper;
using UnityEngine;

using UnityEngine.UI;

namespace Components.UI
{
        public abstract class UIBTN : EventListenerMono
        {
            [SerializeField] private Button _button;

            // buton oluşturduk
            //protected Tanımlandığı sınıfın altında kullanılır ve
            // Kullandığı sınıftan üretilen Alt sınıflarda kullanır
            // .
            //Override deme sebebim Üst sınıf olan EventLis.de Abstracleri geçersiz sayar.
            protected override void RegisterEvents()
            {
                _button.onClick.AddListener(OnClick);
            }
            // AddListener dinlemeye ekleniyor ve evente Kayıt oluyor.

            protected abstract void OnClick();

            protected override void UnRegisterEvents()
            {
                _button.onClick.RemoveListener(OnClick);
                // buton Onclick olduğunda dinlemden siliyor siliyor
            }

        }

       
    
}