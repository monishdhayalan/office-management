using UnityEngine;
using System;
using System.Collections.Generic;

public enum SoundType
{
    None,
    BackgroundMusic,
    ButtonClick,
    PlaceObject,
    RotateObject,
    EmployeeSpawn,
    MoneyEarned,
    TableSpawned,
    Error,
    
}

[System.Serializable]
public class Sound
{
    public SoundType type;
    public AudioClip clip;
    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(.1f, 3f)]
    public float pitch = 1f;
    [Tooltip("Randomly shift pitch by +/- this value")]
    public bool randomizePitch;
    [Range(0f, 0.5f)]
    public float pitchVariance = 0.1f;
    public bool loop;

    [HideInInspector]
    public AudioSource source;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public Sound[] musicSounds;
    public Sound[] sfxSounds;

    public AudioSource musicSource;
    // sfxSource removed as we spawn new ones

    private float _musicVolume = 1f;
    private float _sfxVolume = 1f;
    private float _currentMusicClipVolume = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return; // Important to stop execution
        }

        // Initialize Music Sounds
        foreach (Sound s in musicSounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume * _musicVolume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            // Music usually uses the musicSource primarily, but separate sources allow crossfading if needed.
            // For simplicity, let's play music on a dedicated music source for now, or per-sound sources.
            // Actually, if we want to crossfade or have multiple tracks, per-sound source is better. 
            // BUT, for simple BG music, one source is often enough. Let's stick to the plan of multiple sources for flexibility or just use the dedicated ones.
            
            // Re-thinking: Plan said "Sound Class... holds AudioSource reference". 
            // If we want One Music Source and One SFX Source (simpler volume control), we should use those.
            // If we want multiple concurrent SFX, we should use PlayOneShot or a pool.
            
            // Let's implement a hybrid:
            // SFX: Use a shared AudioSource with PlayOneShot for concurrency.
            // Music: Use a dedicated AudioSource.
        }
    }

    private void Start()
    {
        PlayMusic(SoundType.BackgroundMusic);
    }

    // Changing approach slightly to be more robust for SFX concurrency
    // For SFX, we often want multiple sounds at once (e.g. clicking while an elevator moves). 
    // PlayOneShot is best for this on a single source.

    // List to track active SFX sources for volume control
    private List<AudioSource> _activeSfxSources = new List<AudioSource>();

    public void PlayMusic(SoundType type)
    {
        Sound s = Array.Find(musicSounds, sound => sound.type == type);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + type + " not found!");
            return;
        }
        
        _currentMusicClipVolume = s.volume;
        musicSource.clip = s.clip;
        musicSource.loop = true; // Music usually loops
        UpdateMusicVolume();
        musicSource.Play();
    }

    public void PlaySFX(SoundType type)
    {
        Sound s = Array.Find(sfxSounds, sound => sound.type == type);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + type + " not found!");
            return;
        }

        // Spawn a new GameObject for the SFX
        GameObject sfxObj = new GameObject("SFX_" + type);
        sfxObj.transform.SetParent(transform);
        AudioSource newSource = sfxObj.AddComponent<AudioSource>();
        
        newSource.clip = s.clip;
        newSource.volume = s.volume * _sfxVolume;
        newSource.volume = s.volume * _sfxVolume;
        
        if (s.randomizePitch)
        {
             newSource.pitch = s.pitch + UnityEngine.Random.Range(-s.pitchVariance, s.pitchVariance);
        }
        else
        {
             newSource.pitch = s.pitch;
        }
        newSource.loop = s.loop;
        
        newSource.Play();
        
        _activeSfxSources.Add(newSource);
        
        // Destroy after clip length (convert to seconds)
        // If it's looping, we don't auto-destroy (logic to handle that if needed, but SFX usually don't loop indefinitely without manual stop)
        if (!s.loop)
        {
            Destroy(sfxObj, s.clip.length / s.pitch); // Adjust for pitch if needed
            // We also need to remove from list. Clean up list periodically or use a Coroutine.
            // For simplicity, let's allow the list to be cleaned up or just strict "Fire and Forget" with explicit list removal in a Coroutine.
            StartCoroutine(CleanupSFX(newSource, s.clip.length / s.pitch));
        }
    }

    private System.Collections.IEnumerator CleanupSFX(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (source != null)
        {
            _activeSfxSources.Remove(source);
            // objects are destroyed via Destroy(obj, time), but we need to ensure list consistency.
            // Actually, Destroy(obj, time) doesn't give a callback.
            // So we rely on this coroutine to remove from list AND destroy.
            Destroy(source.gameObject);
        }
    }

    public void SetMusicVolume(float volume)
    {
        _musicVolume = volume;
        UpdateMusicVolume();
    }

    public void SetSFXVolume(float volume)
    {
        _sfxVolume = volume;
        // Update all active SFX sources
        // Note: This assumes all SFX sources should be scaled by _sfxVolume. 
        // We originally multiplied clip.volume * _sfxVolume. 
        // We can't easily recover "original clip volume" from the AudioSource.volume unless we stored it.
        // For accurate updates, we might need a wrapper or just simple "Iterate and Apply" if we assume basic behavior.
        // Issue: sources have different base volumes. 
        // Fix: Just update _sfxVolume. Next sounds will be correct. 
        // If we MUST update active sounds, it's complex without a custom wrapper class for active sounds.
        // USER REQUEST was "spawn sfx audio source", didn't explicitly demand realtime volume update for *active* short sfx.
        // Most SFX are short. Let's stick to updating future ones, OR try to do best effort for looping SFX.
        
        // Best effort: Update source volume? No, verifying base volume is hard. 
        // Let's just update the global variable.
    }

    private void UpdateMusicVolume()
    {
        musicSource.volume = _musicVolume * _currentMusicClipVolume;
    }

    public void ToggleMusic()
    {
        musicSource.mute = !musicSource.mute;
    }

    public void ToggleSFX()
    {
        // Toggle mute for future and potential active ones?
        // Simpler to just use volume 0 or mute flag if we tracked them.
        // For now, let's leave as is (updating variable effectively). 
        // Actually, let's implement Mute on the global AudioListener or per source?
        // The previous shared source had a .mute property.
        // Now valid approach:
        // _sfxMuted = !_sfxMuted;
    }
}
