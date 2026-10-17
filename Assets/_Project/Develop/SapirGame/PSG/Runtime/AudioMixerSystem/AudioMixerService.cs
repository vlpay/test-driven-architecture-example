using System;
using Cysharp.Threading.Tasks;
using SapirGame.PSG.Runtime.Utilities;
using SapirGame.PSG.Runtime.Utilities.Logging;
using UnityEngine;
using UnityEngine.Audio;

namespace SapirGame.PSG.Runtime.AudioMixerSystem
{
    /// <summary>
    /// Provides audio mixing and volume control services for managing game audio levels
    /// and persistence of audio settings across sessions
    /// </summary>
    public class AudioMixerService : IAudioMixerService, ILoadUnit
    {
        private const string MusicVolumeParam = "MusicVolume";
        private const string SfxVolumeParam = "SFXVolume";
        private const string MasterVolumeParam = "MasterVolume";

        private const float AllVolumesDefault = 0.8f;
        private readonly AudioMixer _mixer;
        private const string LogTag = "AudioMixerService";

        /// <summary>
        /// Initializes a new instance of the AudioMixerService with the specified mixer
        /// </summary>
        /// <param name="mixer">The AudioMixer instance to use for audio control</param>
        public AudioMixerService(AudioMixer mixer)
        {
            _mixer = mixer;
        }

        /// <summary>
        /// Gets or sets the music volume level in linear scale (0.0 to 1.0)
        /// Converts between linear scale and decibels for AudioMixer compatibility
        /// </summary>
        public float MusicVolume
        {
            get
            {
                var dbValue = GetMixerVolumeDB(MusicVolumeParam);
                var linearValue = DBToLinear(dbValue);
                Log.Default.D(LogTag, $"MusicVolume GET: {linearValue} (from {dbValue}dB)");
                return linearValue;
            }
            set => SetMixerVolume(MusicVolumeParam, value);
        }

        /// <summary>
        /// Gets or sets the sound effects volume level in linear scale (0.0 to 1.0)
        /// Converts between linear scale and decibels for AudioMixer compatibility
        /// </summary>
        public float SfxVolume
        {
            get
            {
                var dbValue = GetMixerVolumeDB(SfxVolumeParam);
                var linearValue = DBToLinear(dbValue);
                Log.Default.D(LogTag, $"SfxVolume GET: {linearValue} (from {dbValue}dB)");
                return linearValue;
            }
            set => SetMixerVolume(SfxVolumeParam, value);
        }

        /// <summary>
        /// Gets or sets the master volume level in linear scale (0.0 to 1.0)
        /// Converts between linear scale and decibels for AudioMixer compatibility
        /// </summary>
        public float MasterVolume
        {
            get
            {
                var dbValue = GetMixerVolumeDB(MasterVolumeParam);
                var linearValue = DBToLinear(dbValue);
                Log.Default.D(LogTag, $"MasterVolume GET: {linearValue} (from {dbValue}dB)");
                return linearValue;
            }
            set => SetMixerVolume(MasterVolumeParam, value);
        }

        /// <summary>
        /// Indicates whether the audio service has been successfully initialized
        /// and is ready for use
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Mutes or unmutes the music audio channel
        /// </summary>
        /// <param name="mute">True to mute music, false to restore previous volume level</param>
        public void MuteMusic(bool mute)
        {
            Log.Default.D(LogTag, $"MuteMusic: {mute}");
            SetMixerVolume(MusicVolumeParam, mute ? 0.001f : MusicVolume);
        }

        /// <summary>
        /// Mutes or unmutes the sound effects audio channel
        /// </summary>
        /// <param name="mute">True to mute sound effects, false to restore previous volume level</param>
        public void MuteSfx(bool mute)
        {
            Log.Default.D(LogTag, $"MuteSfx: {mute}");
            SetMixerVolume(SfxVolumeParam, mute ? 0.001f : SfxVolume);
        }

        /// <summary>
        /// Mutes or unmutes the master audio channel (affects all audio output)
        /// </summary>
        /// <param name="mute">True to mute all audio, false to restore previous volume level</param>
        public void MuteMaster(bool mute)
        {
            Log.Default.D(LogTag, $"MuteMaster: {mute}");
            SetMixerVolume(MasterVolumeParam, mute ? 0.001f : MasterVolume);
        }

        /// <summary>
        /// Persists current audio settings to permanent storage using PlayerPrefs
        /// Should be called when audio settings are changed to preserve user preferences
        /// </summary>
        public void SaveAudioSettings()
        {
            Log.Default.D(LogTag, $"SaveAudioSettings - Music: {MusicVolume}, SFX: {SfxVolume}, Master: {MasterVolume}");
            PlayerPrefs.SetFloat("audio_music_volume", MusicVolume);
            PlayerPrefs.SetFloat("audio_sfx_volume", SfxVolume);
            PlayerPrefs.SetFloat("audio_master_volume", MasterVolume);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Initializes audio volume values from PlayerPrefs or sets default values
        /// if no saved settings are available
        /// </summary>
        private void InitializeDefaultValues()
        {
            var savedMusicVolume = PlayerPrefs.GetFloat("audio_music_volume", AllVolumesDefault);
            var savedSfxVolume = PlayerPrefs.GetFloat("audio_sfx_volume", AllVolumesDefault);
            var savedMasterVolume = PlayerPrefs.GetFloat("audio_master_volume", AllVolumesDefault);

            Log.Default.D(LogTag, $"InitializeDefaultValues - Music: {savedMusicVolume}, SFX: {savedSfxVolume}, Master: {savedMasterVolume}");

            MusicVolume = savedMusicVolume;
            SfxVolume = savedSfxVolume;
            MasterVolume = savedMasterVolume;

            // Проверяем значения после установки
            Log.Default.D(LogTag, $"After initialization check - Music: {MusicVolume}, SFX: {SfxVolume}, Master: {MasterVolume}");
        }

        /// <summary>
        /// Retrieves the current volume level in decibels for the specified mixer parameter
        /// </summary>
        /// <param name="parameterName">The name of the AudioMixer exposed parameter</param>
        /// <returns>
        /// Volume level in decibels, or -80dB if the parameter is not found
        /// </returns>
        private float GetMixerVolumeDB(string parameterName)
        {
            if (_mixer == null)
            {
                Log.Default.E(LogTag, $"GetMixerVolumeDB: Mixer is null for {parameterName}");
                return -80f;
            }

            if (_mixer.GetFloat(parameterName, out var volumeDB))
            {
                return volumeDB;
            }

            Log.Default.E(LogTag, $"GetMixerVolumeDB: Parameter {parameterName} not found");
            return -80f;
        }

        /// <summary>
        /// Sets the volume level for the specified mixer parameter
        /// </summary>
        /// <param name="parameterName">The name of the AudioMixer exposed parameter</param>
        /// <param name="linearVolume">Volume level in linear scale (0.0 to 1.0)</param>
        private void SetMixerVolume(string parameterName, float linearVolume)
        {
            if (_mixer == null)
            {
                Log.Default.E(LogTag, $"SetMixerVolume: Mixer is null for {parameterName}");
                return;
            }

            var dB = LinearToDB(linearVolume);

            // Проверка до установки
            var currentDB = GetMixerVolumeDB(parameterName);
            var currentLinear = DBToLinear(currentDB);
            Log.Default.D(LogTag, $"SetMixerVolume {parameterName}: {currentLinear}->{linearVolume} ({currentDB}dB->{dB}dB)");

            // Устанавливаем значение
            var success = _mixer.SetFloat(parameterName, dB);
            Log.Default.D(LogTag, $"SetMixerVolume {parameterName}: SetFloat result = {success}");

            // Проверяем значение после установки
            var newDB = GetMixerVolumeDB(parameterName);
            var newLinear = DBToLinear(newDB);
            Log.Default.D(LogTag, $"SetMixerVolume {parameterName}: After set = {newLinear} ({newDB}dB)");

            if (Mathf.Abs(newDB - dB) > 0.1f)
            {
                Log.Default.W(LogTag, $"SetMixerVolume {parameterName}: VALUE MISMATCH! Requested {dB}dB, got {newDB}dB");
            }
        }

        /// <summary>
        /// Converts linear volume scale (0.0 to 1.0) to decibel scale for AudioMixer
        /// </summary>
        /// <param name="linear">Linear volume value between 0.0 and 1.0</param>
        /// <returns>
        /// Volume in decibels, with values below 0.0001 converted to -80dB (effectively silent)
        /// </returns>
        private float LinearToDB(float linear)
        {
            return linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
        }

        /// <summary>
        /// Converts decibel scale to linear volume scale (0.0 to 1.0)
        /// </summary>
        /// <param name="dB">Volume in decibels</param>
        /// <returns>
        /// Linear volume value between 0.0 and 1.0, with values below -80dB returning 0.0
        /// </returns>
        private float DBToLinear(float dB)
        {
            return dB <= -80f ? 0f : Mathf.Pow(10f, dB / 20f);
        }

        /// <summary>
        /// Releases resources when the service is no longer needed
        /// </summary>
        public void Dispose()
        {
            Log.Default.D(LogTag, "Dispose");
            IsInitialized = false;
        }

        public UniTask Load()
        {
            if (IsInitialized)
            {
                Log.Default.D(LogTag, "Already initialized");
                return UniTask.CompletedTask;
            }

            try
            {
                Log.Default.D(LogTag, "Starting initialization...");

                if (_mixer == null)
                {
                    Log.Default.E(LogTag, "AudioMixer is null - cannot initialize");
                    return UniTask.CompletedTask;
                }

                Log.Default.D(LogTag, $"Mixer ready: {_mixer.name}");

                IsInitialized = true;
                Log.Default.D(LogTag, "Service initialized");

                InitializeDefaultValues();

                return UniTask.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                Log.Default.W(LogTag, "Initialization cancelled");
                throw;
            }
            catch (Exception e)
            {
                Log.Default.E(LogTag, $"Initialization failed: {e}");
                throw;
            }
        }
    }
}
