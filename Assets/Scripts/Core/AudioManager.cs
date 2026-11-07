using UnityEngine;
using UnityEngine.SceneManagement;

// Independent of GameManager: created before the first scene and retained across levels.
[DisallowMultipleComponent]
public sealed class AudioManager : MonoBehaviour
{
    public const int MaxInstancesPerEffect = 5;
    public static AudioManager Instance { get; private set; }
    public SoundEffectLibrary Library { get; private set; }

    private enum Effect { MachineGun, KnightSlash, MineralClick, SelectCard, ArcherShot, ButtonSound }

    private sealed class Voice
    {
        public AudioSource source;
        public GunnerTowerController gunner;
        public float activeUntil;
    }

    private Voice[][] voices;
    private readonly int[] lastClip = { -1, -1, -1, -1, -1, -1 };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        AudioManager existing = FindFirstObjectByType<AudioManager>();
        if (existing != null) existing.Initialize();
        else new GameObject("AudioManager").AddComponent<AudioManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Initialize();
    }

    private void Initialize()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (voices != null) { StopAllSfx(); return; }
        Library = Resources.Load<SoundEffectLibrary>("Sound/SoundEffectLibrary");
        if (Library == null || Library.outputGroup == null)
        {
            Debug.LogError("SoundEffectLibrary and its SFX mixer output are required.", this);
            enabled = false;
            return;
        }

        // Separate fixed pools enforce a category limit even when clips alternate.
        voices = new Voice[6][];
        for (int effect = 0; effect < voices.Length; effect++)
        {
            voices[effect] = new Voice[MaxInstancesPerEffect];
            for (int i = 0; i < MaxInstancesPerEffect; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.outputAudioMixerGroup = Library.outputGroup;
                voices[effect][i] = new Voice { source = source };
            }
        }
    }

    private void OnEnable() => SceneManager.activeSceneChanged += OnActiveSceneChanged;
    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        StopAllSfx();
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    private void OnActiveSceneChanged(Scene previous, Scene next)
    {
        if (voices == null) return;
        // A button that loads a scene should still finish its click sound.
        for (int effect = 0; effect < voices.Length; effect++)
        {
            if (effect == (int)Effect.ButtonSound) continue;
            foreach (Voice voice in voices[effect]) StopVoice(voice);
        }
    }

    private void Update() => ReleaseIdleGunners();

    private void ReleaseIdleGunners()
    {
        if (voices == null) return;
        foreach (Voice voice in voices[(int)Effect.MachineGun])
        {
            if (voice.gunner != null && voice.gunner.isActiveAndEnabled && !voice.gunner.isBuildMode
                && voice.gunner.isAggro && Time.time <= voice.activeUntil) continue;
            StopVoice(voice);
        }
    }

    public bool PlayMachineGun(GunnerTowerController gunner)
    {
        if (!isActiveAndEnabled || voices == null || gunner == null || !gunner.isActiveAndEnabled
            || gunner.isBuildMode || !gunner.isAggro) return false;
        ReleaseIdleGunners();
        Voice available = null;
        foreach (Voice voice in voices[(int)Effect.MachineGun])
        {
            if (voice.gunner == gunner)
            {
                voice.activeUntil = Time.time + Mathf.Max(0.05f, gunner.attackCooldown * 1.5f);
                // Refreshing the same Gunner never consumes another instance.
                return voice.source.isPlaying || StartVoice(voice, Effect.MachineGun);
            }
            if (available == null && voice.gunner == null) available = voice;
        }
        // Ignore excess requests: no queue and no interruption of the five audible guns.
        if (available == null || !StartVoice(available, Effect.MachineGun)) return false;
        available.gunner = gunner;
        available.activeUntil = Time.time + Mathf.Max(0.05f, gunner.attackCooldown * 1.5f);
        return true;
    }

    public void StopMachineGun(GunnerTowerController gunner)
    {
        if (voices == null) return;
        foreach (Voice voice in voices[(int)Effect.MachineGun])
            if (voice.gunner == gunner) StopVoice(voice);
    }

    public bool PlayKnightSlash() => PlayOneShot(Effect.KnightSlash);
    public bool PlayMineralClick() => PlayOneShot(Effect.MineralClick);
    public bool PlaySelectCard() => PlayOneShot(Effect.SelectCard);
    public bool PlayArcherShot() => PlayOneShot(Effect.ArcherShot);

    // Buttons choose their own clips, so this takes the clip rather than picking from the library.
    public bool PlayButtonSound(AudioClip clip)
    {
        if (!isActiveAndEnabled || voices == null || clip == null) return false;
        foreach (Voice voice in voices[(int)Effect.ButtonSound])
        {
            if (voice.source.isPlaying) continue;
            voice.source.clip = clip;
            voice.source.volume = Library.buttonVolume;
            voice.source.Play();
            return true;
        }
        return false;
    }

    private bool PlayOneShot(Effect effect)
    {
        if (!isActiveAndEnabled || voices == null) return false;
        foreach (Voice voice in voices[(int)effect])
            if (!voice.source.isPlaying) return StartVoice(voice, effect);
        return false;
    }

    private bool StartVoice(Voice voice, Effect effect)
    {
        AudioClip[] clips;
        float volume;
        switch (effect)
        {
            case Effect.MachineGun: clips = Library.machineGunClips; volume = Library.machineGunVolume; break;
            case Effect.KnightSlash: clips = Library.knightSlashClips; volume = Library.knightSlashVolume; break;
            case Effect.SelectCard: clips = Library.selectCardClips; volume = Library.selectCardVolume; break;
            case Effect.ArcherShot: clips = Library.archerShotClips; volume = Library.archerShotVolume; break;
            default: clips = Library.mineralClickClips; volume = Library.mineralClickVolume; break;
        }
        if (clips == null || clips.Length == 0) return false;
        int index = Random.Range(0, clips.Length);
        if (clips.Length > 1 && index == lastClip[(int)effect]) index = (index + 1) % clips.Length;
        if (clips[index] == null) return false;
        lastClip[(int)effect] = index;
        voice.source.clip = clips[index];
        voice.source.volume = volume;
        voice.source.Play();
        return true;
    }

    private static void StopVoice(Voice voice)
    {
        if (voice.source != null) voice.source.Stop();
        voice.gunner = null;
        voice.activeUntil = 0f;
    }

    public void StopAllSfx()
    {
        if (voices == null) return;
        foreach (Voice[] pool in voices)
            foreach (Voice voice in pool) StopVoice(voice);
    }
}
