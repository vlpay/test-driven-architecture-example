using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Audio/Audio Library")]
public class AudioLibrary : ScriptableObject
{
    [System.Serializable]
    public class SoundEntry
    {
        [HorizontalGroup("Sound", Width = 180)]
        [HideLabel]
        [PreviewField(60, ObjectFieldAlignment.Left)]
        public AudioClip clip;

        [HorizontalGroup("Sound", Width = 100)]
        [LabelText("Vol")]
        [Range(0f, 2f)]
        public float volume = 1f;

        [HorizontalGroup("Sound", Width = 100)]
        [LabelText("Prio")]
        [Range(0, 256)]
        public int priority = 128;
    }

    [System.Serializable]
    public class UISoundEntry : SoundEntry
    {
        [HorizontalGroup("Sound", Width = 150)]
        [EnumToggleButtons]
        [LabelText("Type")]
        public UISoundType type;
    }

    [System.Serializable]
    public class MusicEntry : SoundEntry
    {
        [HorizontalGroup("Sound", Width = 150)]
        [EnumToggleButtons]
        [LabelText("Type")]
        public MusicType type;

        [HorizontalGroup("Sound", Width = 80)]
        [LabelText("Loop")]
        public bool loop = true;
    }

    [Title("UI Sounds")]
    [ListDrawerSettings(
        NumberOfItemsPerPage = 8,
        Expanded = true,
        ShowItemCount = true,
        DraggableItems = true
    )]
    [SerializeField] private List<UISoundEntry> _uiSounds = new();

    [Title("Music")]
    [ListDrawerSettings(NumberOfItemsPerPage = 5, Expanded = true)]
    [SerializeField] private List<MusicEntry> _music = new();

    [Title("SFX")]
    [DictionaryDrawerSettings(
        KeyLabel = "ID",
        ValueLabel = "Settings",
        DisplayMode = DictionaryDisplayOptions.Foldout
    )]
    [SerializeField] private Dictionary<string, SoundEntry> _sfx = new();

    // ����
    private Dictionary<UISoundType, UISoundEntry> _uiCache;
    private Dictionary<MusicType, MusicEntry> _musicCache;

    [Button("Build Cache", ButtonSizes.Medium)]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    private void BuildCache()
    {
        _uiCache = new Dictionary<UISoundType, UISoundEntry>();
        foreach (var sound in _uiSounds)
        {
            if (sound.clip != null) _uiCache[sound.type] = sound;
        }

        _musicCache = new Dictionary<MusicType, MusicEntry>();
        foreach (var track in _music)
        {
            if (track.clip != null) _musicCache[track.type] = track;
        }

        Debug.Log($"Cache: {_uiCache.Count} UI, {_musicCache.Count} Music");
    }

    public bool TryGetUISound(UISoundType type, out AudioClip clip, out float volume, out int priority)
    {
        clip = null;
        volume = 1f;
        priority = 128;

        if (_uiCache == null) BuildCache();

        if (_uiCache.TryGetValue(type, out var sound))
        {
            clip = sound.clip;
            volume = sound.volume;
            priority = sound.priority;
            return true;
        }

        return false;
    }

    public bool TryGetMusic(MusicType type, out AudioClip clip, out float volume, out bool loop)
    {
        clip = null;
        volume = 1f;
        loop = false;

        if (_musicCache == null) BuildCache();

        if (_musicCache.TryGetValue(type, out var track))
        {
            clip = track.clip;
            volume = track.volume;
            loop = track.loop;
            return true;
        }

        return false;
    }

    public bool TryGetSFX(string id, out AudioClip clip, out float volume)
    {
        clip = null;
        volume = 1f;

        if (_sfx.TryGetValue(id, out var entry) && entry.clip != null)
        {
            clip = entry.clip;
            volume = entry.volume;
            return true;
        }

        return false;
    }
}
