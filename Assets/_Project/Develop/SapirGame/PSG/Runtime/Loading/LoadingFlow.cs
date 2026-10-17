using SapirGame.PSG.Runtime.Bootstrap.Units;
using SapirGame.PSG.Runtime.Utilities;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace SapirGame.PSG.Runtime.Loading
{
    public class LoadingFlow : IStartable
    {
        private readonly LoadingService _loadingService;
        private readonly SceneManager _sceneManager;

        public LoadingFlow(LoadingService loadingService, SceneManager sceneManager)
        {
            _loadingService = loadingService;
            _sceneManager = sceneManager;
        }

        public async void Start()
        {
            await _loadingService.BeginLoading(new FooLoadingUnit(3));
            _sceneManager.LoadScene(RuntimeConstants.Scenes.Meta).Forget();
        }
    }
}
