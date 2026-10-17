namespace SapirGame.PSG.Runtime.AudioMixerSystem
{
    /// <summary>
    /// Defines the contract for audio service implementations that manage volume levels
    /// and audio channel muting for the game
    /// </summary>
    public interface IAudioMixerService
    {
        /// <summary>
        /// Gets or sets the music volume level in linear scale (0.0 to 1.0)
        /// </summary>
        float MusicVolume { get; set; }

        /// <summary>
        /// Gets or sets the sound effects volume level in linear scale (0.0 to 1.0)
        /// </summary>
        float SfxVolume { get; set; }

        /// <summary>
        /// Gets or sets the master volume level in linear scale (0.0 to 1.0)
        /// </summary>
        float MasterVolume { get; set; }

        /// <summary>
        /// Indicates whether the audio service has been successfully initialized
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Mutes or unmutes the music audio channel
        /// </summary>
        /// <param name="mute">True to mute music, false to restore previous volume level</param>
        void MuteMusic(bool mute);

        /// <summary>
        /// Mutes or unmutes the sound effects audio channel
        /// </summary>
        /// <param name="mute">True to mute sound effects, false to restore previous volume level</param>
        void MuteSfx(bool mute);

        /// <summary>
        /// Mutes or unmutes the master audio channel (affects all audio output)
        /// </summary>
        /// <param name="mute">True to mute all audio, false to restore previous volume level</param>
        void MuteMaster(bool mute);

        /// <summary>
        /// Persists current audio settings to permanent storage
        /// </summary>
        void SaveAudioSettings();
    }
}
