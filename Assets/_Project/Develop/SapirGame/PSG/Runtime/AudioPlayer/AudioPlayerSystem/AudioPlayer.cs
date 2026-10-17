using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SapirGame.PSG.Runtime.Utilities;
using SapirGame.PSG.Runtime.Utilities.Logging;
using UnityEngine;
using UnityEngine.Audio;
using VContainer;
using Object = UnityEngine.Object;

namespace SapirGame.PSG.Runtime.AudioPlayerSystem
{
    public class AudioPlayer : IAudioPlayer, ILoadUnit, IDisposable
    {
        private class SourceInfo
        {
            public AudioSource source;
            public bool isInUse;
        }

        private readonly AudioLibrary _library;
        private readonly AudioMixerGroup[] _mixerGroups;

        private readonly List<SourceInfo> _sourcePool = new();
        private readonly Queue<SourceInfo> _available = new();

        private AudioSource _musicSource;
        private MusicType _currentMusic = MusicType.None;

        private GameObject _audioRoot;
        private int _missedSounds;

        private CancellationTokenSource _cts;
        private const string LogTag = "AudioPlayer";

        public bool IsInitialized { get; private set; }

        [Inject]
        public AudioPlayer(AudioLibrary library, AudioMixer mixer)
        {
            _library = library;
            _mixerGroups = mixer.FindMatchingGroups(string.Empty);
            _cts = new CancellationTokenSource();
        }

        /// <summary>
        /// Initializes the audio player system by creating audio sources, setting up mixer groups,
        /// and preparing the audio pool for playback. This method must be called before any audio operations.
        /// </summary>
        /// <returns>A UniTask that completes when initialization is finished.</returns>
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

                _audioRoot = new GameObject("Audio");
                Object.DontDestroyOnLoad(_audioRoot);

                var musicGroup = FindGroup("MusicVolume");
                var sfxGroup = FindGroup("SFXVolume");
                var masterGroup = FindGroup("Master");

                for (var i = 0; i < 15; i++)
                {
                    var source = _audioRoot.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.outputAudioMixerGroup = sfxGroup ?? masterGroup;

                    _sourcePool.Add(new SourceInfo { source = source });
                    _available.Enqueue(_sourcePool[i]);
                }

                _musicSource = _audioRoot.AddComponent<AudioSource>();
                _musicSource.playOnAwake = false;
                _musicSource.loop = true;

                if (musicGroup != null)
                    _musicSource.outputAudioMixerGroup = musicGroup;
                else if (masterGroup != null)
                    _musicSource.outputAudioMixerGroup = masterGroup;

                IsInitialized = true;

                Log.Default.D(LogTag, $"Initialization complete. Groups found: {_mixerGroups.Length}");

                return UniTask.CompletedTask;
            }
            catch (System.Exception e)
            {
                Log.Default.E(LogTag, $"Initialization failed: {e}");
                throw;
            }
        }

        private AudioMixerGroup FindGroup(string groupName)
        {
            foreach (var group in _mixerGroups)
                if (group.name == groupName)
                    return group;

            Log.Default.W(LogTag, $"AudioMixerGroup '{groupName}' not found");
            return null;
        }

        public void PlayUI(UISoundType type)
        {
            if (!_library.TryGetUISound(type, out var clip, out var clipVol, out var priority))
            {
                Log.Default.W(LogTag, $"UI sound not found: {type}");
                return;
            }

            var sourceInfo = GetAvailableSource();
            if (sourceInfo == null) return;

            sourceInfo.source.volume = clipVol;
            sourceInfo.source.priority = priority;
            sourceInfo.source.PlayOneShot(clip);

            ReturnToPoolAfterDelay(sourceInfo, clip.length).Forget();
        }

        public void PlaySFX(string id, Vector3 position = default)
        {
            if (!_library.TryGetSFX(id, out var clip, out var clipVol))
            {
                Log.Default.W(LogTag, $"SFX not found: {id}");
                return;
            }

            var sourceInfo = GetAvailableSource();
            if (sourceInfo == null) return;

            sourceInfo.source.volume = clipVol;

            if (position != default)
            {
                sourceInfo.source.spatialBlend = 1f;
                sourceInfo.source.transform.position = position;
            }

            sourceInfo.source.PlayOneShot(clip);
            ReturnToPoolAfterDelay(sourceInfo, clip.length).Forget();
        }

        public void PlayMusic(MusicType type)
        {
            if (_cts.IsCancellationRequested)
            {
                Log.Default.W(LogTag, "Cannot play music - operation cancelled");
                return;
            }

            if (_currentMusic == type && _musicSource != null && _musicSource.isPlaying)
                return;

            if (!_library.TryGetMusic(type, out var clip, out var clipVol, out var loop))
            {
                Log.Default.W(LogTag, $"Music not found: {type}");
                return;
            }

            _currentMusic = type;
            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = clipVol;
            _musicSource.Play();

            Log.Default.D(LogTag, $"Playing music: {type}");
        }

        private SourceInfo GetAvailableSource()
        {
            if (_cts.IsCancellationRequested)
                return null;

            if (_available.Count > 0)
            {
                var info = _available.Dequeue();

                if (info?.source == null)
                {
                    Log.Default.W(LogTag, "Found null source in pool");
                    return null;
                }

                info.isInUse = true;
                info.source.spatialBlend = 0f;
                return info;
            }

            foreach (var info in _sourcePool)
            {
                if (info?.source == null) continue;

                if (info.isInUse && !info.source.isPlaying)
                {
                    ReturnToPool(info);

                    if (_available.Count > 0)
                    {
                        var newInfo = _available.Dequeue();

                        if (newInfo?.source == null)
                            return null;

                        newInfo.isInUse = true;
                        newInfo.source.spatialBlend = 0f;
                        return newInfo;
                    }
                }
            }

            _missedSounds++;
            if (_missedSounds % 20 == 0)
                Log.Default.W(LogTag, $"Missed {_missedSounds} sounds");

            return null;
        }

        private async UniTaskVoid ReturnToPoolAfterDelay(SourceInfo info, float duration)
        {
            if (info?.source == null || _cts.IsCancellationRequested)
                return;

            try
            {
                await UniTask.Delay((int)(duration * 1000) + 100, cancellationToken: _cts.Token);

                if (_cts.IsCancellationRequested || info.source == null)
                    return;

                if (info.isInUse && !info.source.isPlaying)
                    ReturnToPool(info);
            }
            catch (System.OperationCanceledException)
            {
                Log.Default.D(LogTag, "Audio operation cancelled");
            }
            catch (System.Exception e)
            {
                Log.Default.W(LogTag, $"Error in ReturnToPoolAfterDelay: {e.Message}");
            }
        }

        private void ReturnToPool(SourceInfo info)
        {
            if (_cts.IsCancellationRequested || info?.source == null)
                return;

            info.isInUse = false;
            info.source.Stop();
            info.source.spatialBlend = 0f;

            if (info.source != null)
                _available.Enqueue(info);
        }

        public void StopMusic()
        {
            if (_musicSource != null)
            {
                _musicSource.Stop();
                _currentMusic = MusicType.None;
                Log.Default.D(LogTag, "Music stopped");
            }
        }

        public void PauseMusic(bool pause)
        {
            if (_musicSource == null) return;

            if (pause)
            {
                _musicSource.Pause();
                Log.Default.D(LogTag, "Music paused");
            }
            else
            {
                _musicSource.UnPause();
                Log.Default.D(LogTag, "Music unpaused");
            }
        }

        public void StopAll()
        {
            _cts?.Cancel();

            foreach (var info in _sourcePool)
            {
                if (info?.source == null) continue;

                if (info.isInUse)
                {
                    info.source.Stop();
                    info.isInUse = false;

                    if (info.source != null)
                        _available.Enqueue(info);
                }
            }

            StopMusic();
            Log.Default.D(LogTag, "All audio stopped");
        }

        public void Dispose()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            StopAll();

            if (_audioRoot != null)
            {
                Object.Destroy(_audioRoot);
                _audioRoot = null;
            }

            _sourcePool.Clear();
            _available.Clear();

            IsInitialized = false;
            Log.Default.D(LogTag, "AudioPlayer disposed");
        }

        public bool IsActive => !_cts.IsCancellationRequested && _audioRoot != null;
    }
}
