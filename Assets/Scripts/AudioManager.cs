using UnityEngine;
using System.Collections;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(0.1f, 3f)]
    public float pitch = 1f;
    public bool loop = false;
    
    [HideInInspector]
    public AudioSource source;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [Header("Audio Sources")]
    public Sound[] sounds;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    [Range(0f, 1f)]
    public float musicVolume = 1f;
    
    [Header("Music Settings")]
    public bool playMusicOnStart = true;
    public string backgroundMusicName = "Gameplay";
    
    private string currentBackgroundMusic;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Debug.Log("[AUDIO] AudioManager: Initializing AudioManager singleton");
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudio();
        }
        else
        {
            Debug.Log("[AUDIO] AudioManager: Duplicate AudioManager found, destroying");
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        Debug.Log("AudioManager: Start() called, playMusicOnStart = " + playMusicOnStart);
        if (playMusicOnStart)
        {
            Debug.Log("AudioManager: Starting coroutine to play Main Menu music");
            StartCoroutine(PlayMainMenuMusicDelayed());
        }
    }
    
    private System.Collections.IEnumerator PlayMainMenuMusicDelayed()
    {
        // Wait a frame to ensure everything is initialized
        yield return null;
        Debug.Log("AudioManager: Now playing Main Menu music after delay");
        PlayBackgroundMusic("Main Menu");
    }
    
    void SetupAudio()
    {
        Debug.Log($"[AUDIO] AudioManager: Setting up {sounds.Length} sounds");
        
        foreach (Sound sound in sounds)
        {
            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.clip = sound.clip;
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.loop = sound.loop;
            
            Debug.Log($"[AUDIO] AudioManager: Setup sound '{sound.name}' - Clip: {(sound.clip != null ? sound.clip.name : "NULL")}, Volume: {sound.volume}");
        }
        
        Debug.Log("[AUDIO] AudioManager: Audio setup complete");
    }
    
    public void PlaySound(string name)
    {
        Debug.Log($"[AUDIO] AudioManager: Trying to play sound '{name}'");
        
        Sound sound = System.Array.Find(sounds, s => s.name == name);
        if (sound == null)
        {
            Debug.LogWarning($"[AUDIO] AudioManager: Sound '{name}' not found! Available sounds: {string.Join(", ", System.Array.ConvertAll(sounds, s => s.name))}");
            return;
        }
        
        if (sound.source == null)
        {
            Debug.LogError($"[AUDIO] AudioManager: Sound '{name}' has no AudioSource!");
            return;
        }
        
        if (sound.clip == null)
        {
            Debug.LogError($"[AUDIO] AudioManager: Sound '{name}' has no AudioClip!");
            return;
        }
        
        float volume = sound.volume * sfxVolume * masterVolume;
        Debug.Log($"[AUDIO] AudioManager: Playing '{name}' at volume {volume}");
        
        // Use PlayOneShot for SFX to prevent interruption
        if (name == "Damage" || name == "Jump" || name == "Multiply Door" || name == "Game Over" || name == "Game Complete")
        {
            sound.source.PlayOneShot(sound.clip, volume);
        }
        else
        {
            // Use Play for music/looping sounds
            sound.source.volume = volume;
            sound.source.Play();
        }
    }
    
    public void PlaySoundOneShot(string name)
    {
        Sound sound = System.Array.Find(sounds, s => s.name == name);
        if (sound == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }
        
        sound.source.PlayOneShot(sound.clip, sound.volume * sfxVolume * masterVolume);
    }
    
    public void PlayDamageSound()
    {
        Debug.Log("[AUDIO] AudioManager: Playing damage sound with PlayOneShot");
        Sound sound = System.Array.Find(sounds, s => s.name == "Damage");
        if (sound != null && sound.source != null && sound.clip != null)
        {
            float volume = sound.volume * sfxVolume * masterVolume;
            sound.source.PlayOneShot(sound.clip, volume);
            Debug.Log($"[AUDIO] AudioManager: Damage sound played at volume {volume}");
        }
        else
        {
            Debug.LogError("[AUDIO] AudioManager: Could not play damage sound - missing components");
        }
    }
    
    public void PlayBackgroundMusic(string name)
    {
        // Stop current background music if playing
        if (!string.IsNullOrEmpty(currentBackgroundMusic))
        {
            StopSound(currentBackgroundMusic);
        }
        
        Sound music = System.Array.Find(sounds, s => s.name == name);
        if (music == null)
        {
            Debug.LogWarning("Music: " + name + " not found!");
            return;
        }
        
        music.source.volume = music.volume * musicVolume * masterVolume;
        music.source.loop = true;
        music.source.Play();
        currentBackgroundMusic = name;
    }
    
    public void StopSound(string name)
    {
        Sound sound = System.Array.Find(sounds, s => s.name == name);
        if (sound == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }
        
        sound.source.Stop();
    }
    
    public void PauseSound(string name)
    {
        Sound sound = System.Array.Find(sounds, s => s.name == name);
        if (sound == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }
        
        sound.source.Pause();
    }
    
    public void UnpauseSound(string name)
    {
        Sound sound = System.Array.Find(sounds, s => s.name == name);
        if (sound == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }
        
        sound.source.UnPause();
    }
    
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }
    
    private void UpdateAllVolumes()
    {
        foreach (Sound sound in sounds)
        {
            if (sound.source != null)
            {
                if (sound.name == currentBackgroundMusic)
                {
                    sound.source.volume = sound.volume * musicVolume * masterVolume;
                }
                else
                {
                    sound.source.volume = sound.volume * sfxVolume * masterVolume;
                }
            }
        }
    }
    
    public void PauseAllAudio()
    {
        foreach (Sound sound in sounds)
        {
            if (sound.source != null && sound.source.isPlaying)
            {
                sound.source.Pause();
            }
        }
    }
    
    public void UnpauseAllAudio()
    {
        foreach (Sound sound in sounds)
        {
            if (sound.source != null)
            {
                sound.source.UnPause();
            }
        }
    }
    
    public bool IsPlaying(string name)
    {
        Sound sound = System.Array.Find(sounds, s => s.name == name);
        if (sound == null)
        {
            return false;
        }
        
        return sound.source.isPlaying;
    }
    
    public void FadeInMusic(string name, float duration = 2f)
    {
        PlayBackgroundMusic(name);
        Sound music = System.Array.Find(sounds, s => s.name == name);
        if (music != null)
        {
            StartCoroutine(FadeIn(music.source, duration, music.volume * musicVolume * masterVolume));
        }
    }
    
    public void FadeOutMusic(string name, float duration = 2f)
    {
        Sound music = System.Array.Find(sounds, s => s.name == name);
        if (music != null)
        {
            StartCoroutine(FadeOut(music.source, duration));
        }
    }
    
    private IEnumerator FadeIn(AudioSource audioSource, float duration, float targetVolume)
    {
        audioSource.volume = 0f;
        float timer = 0f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, timer / duration);
            yield return null;
        }
        
        audioSource.volume = targetVolume;
    }
    
    private IEnumerator FadeOut(AudioSource audioSource, float duration)
    {
        float startVolume = audioSource.volume;
        float timer = 0f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }
        
        audioSource.volume = 0f;
        audioSource.Stop();
    }
}
