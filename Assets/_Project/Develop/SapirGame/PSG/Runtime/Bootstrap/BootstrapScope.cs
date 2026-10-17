using SapirGame.PSG.Runtime.AudioMixerSystem;
using SapirGame.PSG.Runtime.AudioPlayerSystem;
using SapirGame.PSG.Runtime.Utilities;
using UnityEngine;
using UnityEngine.Audio;
using VContainer;
using VContainer.Unity;

namespace SapirGame.PSG.Runtime.Bootstrap
{
    public sealed class BootstrapScope : LifetimeScope
    {
        [SerializeField] private AudioLibrary _audioLibrary;
        [SerializeField] private AudioMixer _audioMixer;

        protected override void Awake()
        {
            DontDestroyOnLoad(this);
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<LoadingService>(Lifetime.Scoped);
            builder.Register<SceneManager>(Lifetime.Singleton);

            builder.Register<AudioMixerService>(Lifetime.Singleton).As<IAudioMixerService>().As<ILoadUnit>();
            builder.RegisterInstance(_audioMixer);

            builder.Register<AudioPlayer>(Lifetime.Singleton)
                .As<IAudioPlayer>().As<ILoadUnit>();
            builder.RegisterInstance(_audioLibrary);

            builder.RegisterEntryPoint<BootstrapFlow>();
        }
    }
}
