using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private float waterMaxVolume = 0.7f;
    [SerializeField] private float waterHearingDistance = 25f;
    [SerializeField] private float waterFadeSpeed = 3f;
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float musicVolume = 0.5f;
    [SerializeField] private float musicLoopBpm = 120f;

    private AudioSource playerAudioSource;
    private AudioSource waterAudioSource;
    private AudioSource ambientAudioSource;
    private AudioSource musicAudioSource;
    private Transform playerTransform;
    private Transform waterTransform;
    private AudioClip hitClip;
    private AudioClip waterClip;
    private AudioClip ambientClip;
    private AudioClip[] musicTracks = new AudioClip[0];
    private int selectedTrackIndex;
    private float targetWaterVolume;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public AudioClip[] MusicTracks => musicTracks;
    public int SelectedTrackIndex => selectedTrackIndex;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        selectedTrackIndex = PlayerPrefs.GetInt("SelectedTrack", 0);
        AudioListener.volume = masterVolume;
        LoadAudioClips();
        LoadMusicTracks();
        SetupPlayerAudio();
        SetupWaterAudio();
        SetupAmbientAudio();
        SetupMusicAudio();
    }

    void LoadAudioClips()
    {
        hitClip = Resources.Load<AudioClip>("Audio/GolfHit");
        waterClip = Resources.Load<AudioClip>("Audio/RiverAmbient");

        if (hitClip == null)
            hitClip = GenerateHitClip();
        if (waterClip == null)
            waterClip = GenerateWaterClip();
    }

    void LoadMusicTracks()
    {
        AudioClip[] loaded = Resources.LoadAll<AudioClip>("AudioLoop");
        List<AudioClip> tracks = new List<AudioClip>();
        if (loaded != null)
        {
            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null) tracks.Add(loaded[i]);
            }
        }

        AudioClip procedural = GenerateProceduralMusicLoop();
        if (procedural != null)
            tracks.Add(procedural);

        musicTracks = tracks.ToArray();

        if (musicTracks.Length == 0)
        {
            selectedTrackIndex = 0;
        }
        else if (selectedTrackIndex < 0 || selectedTrackIndex >= musicTracks.Length)
        {
            selectedTrackIndex = 0;
        }
    }

    AudioClip GenerateHitClip()
    {
        int sampleRate = 44100;
        int durationSamples = (int)(sampleRate * 0.2f);
        float[] samples = new float[durationSamples];

        for (int i = 0; i < durationSamples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 25f);
            float signal = Mathf.Sin(2f * Mathf.PI * 800f * t) * 0.5f;
            signal += Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.25f;
            signal += Mathf.Sin(2f * Mathf.PI * 400f * t) * 0.15f;
            signal += Mathf.Sin(2f * Mathf.PI * 2000f * t) * 0.1f * Mathf.Exp(-t * 40f);
            samples[i] = signal * envelope;
        }

        AudioClip clip = AudioClip.Create("GolfHit", durationSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    AudioClip GenerateWaterClip()
    {
        int sampleRate = 44100;
        int durationSamples = (int)(sampleRate * 4f);
        float[] samples = new float[durationSamples];

        System.Random rng = new System.Random(42);

        for (int i = 0; i < durationSamples; i++)
        {
            float t = (float)i / sampleRate;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.15f;
            float lowRumble = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.05f;
            float midFlow = Mathf.Sin(2f * Mathf.PI * 300f * t + Mathf.Sin(t * 2f) * 3f) * 0.08f;
            float crossfade = Mathf.Sin(t * 0.5f * Mathf.PI);
            samples[i] = (noise + lowRumble + midFlow) * (0.5f + crossfade * 0.5f);
        }

        AudioClip clip = AudioClip.Create("RiverAmbient", durationSamples, 1, sampleRate, true);
        clip.SetData(samples, 0);
        return clip;
    }

    AudioClip GenerateProceduralMusicLoop()
    {
        int sampleRate = 44100;
        float beatsPerSecond = musicLoopBpm / 60f;
        float secondsPerBeat = 1f / beatsPerSecond;
        float loopSeconds = secondsPerBeat * 16f;
        int durationSamples = (int)(sampleRate * loopSeconds);
        float[] samples = new float[durationSamples];

        float[] melody = new float[] { 261.63f, 329.63f, 392.0f, 523.25f, 392.0f, 329.63f, 392.0f, 440.0f,
                                       392.0f, 329.63f, 392.0f, 523.25f, 587.33f, 523.25f, 440.0f, 392.0f };
        float[] bass = new float[] { 65.41f, 65.41f, 73.42f, 65.41f, 87.31f, 87.31f, 98.0f, 87.31f,
                                     65.41f, 65.41f, 73.42f, 65.41f, 87.31f, 98.0f, 87.31f, 65.41f };

        System.Random hatRng = new System.Random(12345);

        for (int i = 0; i < durationSamples; i++)
        {
            float t = (float)i / sampleRate;
            float beatPos = t * beatsPerSecond;
            int beatIndex = (int)beatPos % 16;
            float beatFraction = beatPos - Mathf.Floor(beatPos);

            float kickEnv = (beatIndex % 4 == 0) ? Mathf.Exp(-beatFraction * 14f) : 0f;
            float kick = Mathf.Sin(2f * Mathf.PI * (60f + 30f * Mathf.Exp(-beatFraction * 20f)) * t) * kickEnv * 0.45f;

            float hatEnv = ((int)(beatPos * 2f) % 2 == 1) ? Mathf.Exp(-((beatPos * 2f) - Mathf.Floor(beatPos * 2f)) * 35f) : 0f;
            float hat = (float)(hatRng.NextDouble() * 2.0 - 1.0) * hatEnv * 0.12f;

            float bassFreq = bass[beatIndex];
            float bassEnv = Mathf.Exp(-beatFraction * 2.5f);
            float bassWave = Mathf.Sin(2f * Mathf.PI * bassFreq * t) * bassEnv * 0.18f;

            float melodyFreq = melody[beatIndex];
            float melodyEnv = Mathf.Exp(-beatFraction * 1.5f) * 0.18f;
            float melodyWave = (Mathf.Sin(2f * Mathf.PI * melodyFreq * t) * 0.7f
                               + Mathf.Sin(2f * Mathf.PI * melodyFreq * 2f * t) * 0.25f) * melodyEnv;

            float pad = Mathf.Sin(2f * Mathf.PI * 130.81f * t + Mathf.Sin(t * 0.7f) * 1.2f) * 0.04f;

            float sample = kick + hat + bassWave + melodyWave + pad;
            samples[i] = Mathf.Clamp(sample, -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create("ProceduralMusicLoop", durationSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void SetupPlayerAudio()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        playerTransform = playerObj.transform;
        playerAudioSource = playerObj.GetComponent<AudioSource>();
        if (playerAudioSource == null)
        {
            playerAudioSource = playerObj.AddComponent<AudioSource>();
        }

        playerAudioSource.playOnAwake = false;
        playerAudioSource.loop = false;
        playerAudioSource.spatialBlend = 0f;
        playerAudioSource.volume = 0.8f;

        if (hitClip != null)
        {
            playerAudioSource.clip = hitClip;
        }
    }

    void SetupWaterAudio()
    {
        GameObject waterObj = FindWaterObject();
        if (waterObj == null) return;

        waterTransform = waterObj.transform;
        waterAudioSource = waterObj.GetComponent<AudioSource>();
        if (waterAudioSource == null)
        {
            waterAudioSource = waterObj.AddComponent<AudioSource>();
        }

        waterAudioSource.playOnAwake = false;
        waterAudioSource.loop = true;
        waterAudioSource.spatialBlend = 1f;
        waterAudioSource.minDistance = 1f;
        waterAudioSource.maxDistance = waterHearingDistance;
        waterAudioSource.volume = 0f;

        if (waterClip != null)
        {
            waterAudioSource.clip = waterClip;
            waterAudioSource.Play();
        }
    }

    GameObject FindWaterObject()
    {
        GameObject water = GameObject.Find("StreamWater");
        if (water != null) return water;

        WaterTrigger trigger = FindObjectOfType<WaterTrigger>();
        if (trigger != null) return trigger.gameObject;

        return null;
    }

    void SetupAmbientAudio()
    {
        ambientAudioSource = gameObject.AddComponent<AudioSource>();
        ambientAudioSource.playOnAwake = false;
        ambientAudioSource.loop = true;
        ambientAudioSource.spatialBlend = 0f;
        ambientAudioSource.volume = 0.15f;

        ambientClip = GenerateAmbientClip();
        if (ambientClip != null)
        {
            ambientAudioSource.clip = ambientClip;
            ambientAudioSource.Play();
        }
    }

    void SetupMusicAudio()
    {
        musicAudioSource = gameObject.AddComponent<AudioSource>();
        musicAudioSource.playOnAwake = false;
        musicAudioSource.loop = true;
        musicAudioSource.spatialBlend = 0f;
        musicAudioSource.volume = musicVolume;
        musicAudioSource.ignoreListenerPause = true;

        if (musicTracks != null && musicTracks.Length > 0)
        {
            int safeIndex = Mathf.Clamp(selectedTrackIndex, 0, musicTracks.Length - 1);
            selectedTrackIndex = safeIndex;
            musicAudioSource.clip = musicTracks[safeIndex];
            musicAudioSource.Play();
        }
    }

    AudioClip GenerateAmbientClip()
    {
        int sampleRate = 44100;
        int durationSamples = (int)(sampleRate * 8f);
        float[] samples = new float[durationSamples];
        System.Random rng = new System.Random(99);

        for (int i = 0; i < durationSamples; i++)
        {
            float t = (float)i / sampleRate;
            float wind = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.03f;
            wind += Mathf.Sin(2f * Mathf.PI * 80f * t + Mathf.Sin(t * 0.7f) * 5f) * 0.02f;
            float birds = Mathf.Sin(2f * Mathf.PI * 2200f * t) * Mathf.Exp(-Mathf.Repeat(t, 4f) * 8f) * 0.02f;
            samples[i] = wind + birds;
        }

        AudioClip clip = AudioClip.Create("AmbientOutdoor", durationSamples, 1, sampleRate, true);
        clip.SetData(samples, 0);
        return clip;
    }

    void Update()
    {
        UpdateWaterVolume();
    }

    void UpdateWaterVolume()
    {
        if (waterAudioSource == null) return;
        if (playerTransform == null || waterTransform == null)
        {
            RelinkScenicReferences();
            if (playerTransform == null || waterTransform == null) return;
        }

        float distance = Vector3.Distance(playerTransform.position, waterTransform.position);
        float normalized = Mathf.Clamp01(1f - (distance / waterHearingDistance));
        targetWaterVolume = normalized * waterMaxVolume;

        waterAudioSource.volume = Mathf.Lerp(waterAudioSource.volume, targetWaterVolume, waterFadeSpeed * Time.unscaledDeltaTime);
    }

    void RelinkScenicReferences()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
        if (waterTransform == null)
        {
            GameObject w = FindWaterObject();
            if (w != null) waterTransform = w.transform;
        }
    }

    public void PlayHitSound()
    {
        if (playerAudioSource == null) return;

        if (hitClip != null)
        {
            playerAudioSource.PlayOneShot(hitClip, 0.8f);
        }
    }

    public void PlayHoleSound()
    {
        if (playerAudioSource == null) return;

        int sampleRate = 44100;
        int durationSamples = (int)(sampleRate * 0.6f);
        float[] samples = new float[durationSamples];

        for (int i = 0; i < durationSamples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 4f);
            float signal = Mathf.Sin(2f * Mathf.PI * 523f * t) * 0.3f;
            signal += Mathf.Sin(2f * Mathf.PI * 659f * t) * 0.25f;
            signal += Mathf.Sin(2f * Mathf.PI * 784f * t) * 0.2f;
            signal += Mathf.Sin(2f * Mathf.PI * 1047f * t) * 0.15f;
            samples[i] = signal * envelope;
        }

        AudioClip holeClip = AudioClip.Create("HoleComplete", durationSamples, 1, sampleRate, false);
        holeClip.SetData(samples, 0);
        playerAudioSource.PlayOneShot(holeClip, 0.7f);
        StartCoroutine(DestroyClipAfter(holeClip, 1f));
    }

    IEnumerator DestroyClipAfter(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (clip != null) Destroy(clip);
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = masterVolume;
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicAudioSource != null)
            musicAudioSource.volume = musicVolume;
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save();
    }

    public void SelectTrack(int index)
    {
        if (musicTracks == null || musicTracks.Length == 0) return;
        index = Mathf.Clamp(index, 0, musicTracks.Length - 1);
        selectedTrackIndex = index;
        PlayerPrefs.SetInt("SelectedTrack", selectedTrackIndex);
        PlayerPrefs.Save();

        if (musicAudioSource != null)
        {
            musicAudioSource.Stop();
            musicAudioSource.clip = musicTracks[selectedTrackIndex];
            musicAudioSource.Play();
        }
    }

    public void NextTrack()
    {
        if (musicTracks == null || musicTracks.Length == 0) return;
        SelectTrack((selectedTrackIndex + 1) % musicTracks.Length);
    }

    public void PreviousTrack()
    {
        if (musicTracks == null || musicTracks.Length == 0) return;
        SelectTrack((selectedTrackIndex - 1 + musicTracks.Length) % musicTracks.Length);
    }

    public string GetTrackName(int index)
    {
        if (musicTracks == null || musicTracks.Length == 0) return "None";
        index = Mathf.Clamp(index, 0, musicTracks.Length - 1);
        AudioClip c = musicTracks[index];
        if (c == null) return "(empty)";
        return c.name == "ProceduralMusicLoop" ? "Procedural" : c.name;
    }
}
