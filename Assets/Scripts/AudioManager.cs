using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private float waterMaxVolume = 0.7f;
    [SerializeField] private float waterHearingDistance = 25f;
    [SerializeField] private float waterFadeSpeed = 3f;
    [SerializeField] private float masterVolume = 1f;

    private AudioSource playerAudioSource;
    private AudioSource waterAudioSource;
    private AudioSource ambientAudioSource;
    private Transform playerTransform;
    private Transform waterTransform;
    private AudioClip hitClip;
    private AudioClip waterClip;
    private AudioClip ambientClip;
    private float targetWaterVolume;

    public float MasterVolume => masterVolume;

    void Start()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = masterVolume;
        LoadAudioClips();
        SetupPlayerAudio();
        SetupWaterAudio();
        SetupAmbientAudio();
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

    void SetupPlayerAudio()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("[AudioManager] No Player found!");
            return;
        }

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
        if (waterObj == null)
        {
            Debug.LogWarning("[AudioManager] No Water object found!");
            return;
        }

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
        if (waterAudioSource == null || playerTransform == null || waterTransform == null) return;

        float distance = Vector3.Distance(playerTransform.position, waterTransform.position);
        float normalized = Mathf.Clamp01(1f - (distance / waterHearingDistance));
        targetWaterVolume = normalized * waterMaxVolume;

        waterAudioSource.volume = Mathf.Lerp(waterAudioSource.volume, targetWaterVolume, waterFadeSpeed * Time.deltaTime);
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
        masterVolume = volume;
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
    }
}
