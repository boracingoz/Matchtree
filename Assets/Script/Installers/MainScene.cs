using UnityEngine;
using Zenject;

namespace Installers
{
    public class MainScene : MonoInstaller
    {
        [SerializeField] private Camera _camera;
        public override void InstallBindings()
        {
            Container.BindInstance(_camera);
        }
    }
}