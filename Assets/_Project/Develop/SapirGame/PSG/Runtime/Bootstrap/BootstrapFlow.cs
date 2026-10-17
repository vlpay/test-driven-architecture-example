using SapirGame.PSG.Runtime.Bootstrap.Units;
using SapirGame.PSG.Runtime.Utilities;
using SapirGame.PSG.Runtime.AudioMixerSystem;
using SapirGame.PSG.Runtime.AudioPlayerSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace SapirGame.PSG.Runtime.Bootstrap
{
    public class BootstrapFlow : IStartable
    {
        private readonly LoadingService _loadingService;
        private readonly SceneManager _sceneManager;
        private readonly IAudioMixerService _audioMixerService;
        private readonly IAudioPlayer _audioPlayer;

        public BootstrapFlow(
            LoadingService loadingService,
            SceneManager sceneManager,
            IAudioMixerService audioMixerService,
            IAudioPlayer audioPlayer)
        {
            _loadingService = loadingService;
            _sceneManager = sceneManager;
            _audioMixerService = audioMixerService;
            _audioPlayer = audioPlayer;
        }

        public async void Start()
        {
            Application.targetFrameRate = RuntimeConstants.FrameRate.Default;

            await _loadingService.BeginLoading(_audioMixerService as ILoadUnit);
            await _loadingService.BeginLoading(_audioPlayer as ILoadUnit);

            _sceneManager.LoadScene(RuntimeConstants.Scenes.Loading).Forget();
        }
    }
}
