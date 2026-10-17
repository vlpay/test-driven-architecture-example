namespace SapirGame.PSG.Runtime.AudioPlayerSystem
{
    public interface IAudioPlayer
    {
        void PlayUI(UISoundType type);
        void PlayMusic(MusicType type);
        void StopMusic();
        void PauseMusic(bool pause);
        void StopAll();
    }
}
