using Godot;

/// <summary>
/// Ponto único de áudio. Os clipes ficam vazios por enquanto: atribua os AudioStream em AudioManager.tscn
/// e tudo passa a tocar sem mudar código (Play ignora clipes nulos).
/// </summary>
public partial class AudioManager : Node
{
    public static AudioManager Instance { get; private set; }

    [Export] public AudioStream Shoot { get; set; }
    [Export] public AudioStream EnemyHit { get; set; }
    [Export] public AudioStream EnemyDeath { get; set; }
    [Export] public AudioStream PlayerHurt { get; set; }
    [Export] public AudioStream Pickup { get; set; }
    [Export] public AudioStream Heal { get; set; }
    [Export] public AudioStream SlotSpin { get; set; }
    [Export] public AudioStream SlotWin { get; set; }
    [Export] public AudioStream Jackpot { get; set; }
    [Export] public AudioStream BossSpawn { get; set; }
    [Export] public AudioStream GameOver { get; set; }
    [Export] public AudioStream Victory { get; set; }

    [ExportGroup("Música")]
    /// <summary>Trilha de fundo em loop. Toca desde o início e não para no reload da cena.</summary>
    [Export] public AudioStream Music { get; set; }
    /// <summary>Trilha dos rounds de boss.</summary>
    [Export] public AudioStream BossMusic { get; set; }
    /// <summary>Volume da trilha de boss.</summary>
    [Export] public float MusicVolumeDb { get; set; } = -4f;
    /// <summary>Volume da trilha principal (a gravação é mais baixa que a do boss).</summary>
    [Export] public float MainMusicVolumeDb { get; set; } = 3f;
    /// <summary>Trecho da trilha principal que fica em loop (s). Fim &lt;= 0 = faixa inteira.</summary>
    [Export] public float MainLoopStart { get; set; } = 10f;
    [Export] public float MainLoopEnd { get; set; } = 33f;
    /// <summary>Tempo de troca entre as trilhas (s).</summary>
    [Export] public float MusicFade { get; set; } = 0.8f;

    AudioStreamPlayer source, music;

    public const string MusicBus = "Music";
    public const string SfxBus = "SFX";
    const string SettingsPath = "user://settings.cfg";
    static readonly string[] VolumeBuses = { "Master", MusicBus, SfxBus };

    public override void _Ready()
    {
        Instance = this;
        EnsureBuses();
        LoadVolumes();
        ProcessMode = ProcessModeEnum.Always; // o caça-níquel toca com o jogo pausado
        source = GetNode<AudioStreamPlayer>("Source");
        source.Bus = SfxBus;
        StartMusic();
    }

    Tween musicFade;

    void StartMusic()
    {
        music = new AudioStreamPlayer { Bus = MusicBus };
        AddChild(music);
        // Loop garantido mesmo se a flag do MP3 não pegar: ao terminar, recomeça a mesma faixa.
        music.Finished += () => music.Play(StartOf(music.Stream));
        SwitchMusic(Music, fade: false);
    }

    /// <summary>Trilha normal da fase.</summary>
    public void PlayMainMusic() => SwitchMusic(Music, true);

    /// <summary>Trilha de boss (cai na normal se não houver).</summary>
    public void PlayBossMusic() => SwitchMusic(BossMusic ?? Music, true);

    void SwitchMusic(AudioStream stream, bool fade)
    {
        if (music == null || stream == null) return;
        if (music.Stream == stream && music.Playing) return;
        if (stream is AudioStreamMP3 mp3) mp3.Loop = true;

        musicFade?.Kill();
        if (!fade || !music.Playing)
        {
            music.Stream = stream;
            music.VolumeDb = VolumeOf(stream);
            music.Play(StartOf(stream));
            return;
        }
        musicFade = CreateTween();
        musicFade.TweenProperty(music, "volume_db", -40f, MusicFade * 0.5f);
        musicFade.TweenCallback(Callable.From(() => { music.Stream = stream; music.Play(StartOf(stream)); }));
        musicFade.TweenProperty(music, "volume_db", VolumeOf(stream), MusicFade * 0.5f);
    }

    /// <summary>Canais Music e SFX (enviam para o Master) para o menu de pausa controlar separado.</summary>
    static void EnsureBuses()
    {
        foreach (string name in new[] { MusicBus, SfxBus })
        {
            if (AudioServer.GetBusIndex(name) >= 0) continue;
            int i = AudioServer.BusCount;
            AudioServer.AddBus(i);
            AudioServer.SetBusName(i, name);
            AudioServer.SetBusSend(i, "Master");
        }
    }

    /// <summary>Volume linear (0 a 1) de "Master", "Music" ou "SFX".</summary>
    public static float GetVolume(string bus)
    {
        int i = AudioServer.GetBusIndex(bus);
        if (i < 0 || AudioServer.IsBusMute(i)) return 0f;
        return Mathf.DbToLinear(AudioServer.GetBusVolumeDb(i));
    }

    public static void SetVolume(string bus, float linear, bool save = true)
    {
        int i = AudioServer.GetBusIndex(bus);
        if (i < 0) return;
        linear = Mathf.Clamp(linear, 0f, 1f);
        AudioServer.SetBusMute(i, linear <= 0.001f);
        AudioServer.SetBusVolumeDb(i, Mathf.LinearToDb(Mathf.Max(linear, 0.001f)));
        if (save) SaveVolumes();
    }

    static void LoadVolumes()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(SettingsPath) != Error.Ok) return;
        foreach (string bus in VolumeBuses)
            SetVolume(bus, (float)cfg.GetValue("audio", bus, 1f), save: false);
    }

    static void SaveVolumes()
    {
        var cfg = new ConfigFile();
        cfg.Load(SettingsPath); // preserva outras seções
        foreach (string bus in VolumeBuses) cfg.SetValue("audio", bus, GetVolume(bus));
        cfg.Save(SettingsPath);
    }

    float VolumeOf(AudioStream stream) => stream == Music ? MainMusicVolumeDb : MusicVolumeDb;
    float StartOf(AudioStream stream) => stream == Music ? MainLoopStart : 0f;

    /// <summary>Trilha principal: ao passar do fim do trecho, volta para o início dele.</summary>
    public override void _Process(double delta)
    {
        if (music == null || !music.Playing || music.Stream != Music || MainLoopEnd <= 0f) return;
        if (music.GetPlaybackPosition() >= MainLoopEnd) music.Seek(MainLoopStart);
    }

    void Play(AudioStream clip)
    {
        if (clip == null) return;
        source.Stream = clip;
        source.Play();
    }

    public void PlayShoot() => Play(Shoot);
    public void PlayEnemyHit() => Play(EnemyHit);
    public void PlayEnemyDeath() => Play(EnemyDeath);
    public void PlayPlayerHurt() => Play(PlayerHurt);
    public void PlayPickup() => Play(Pickup);
    public void PlayHeal() => Play(Heal);
    public void PlaySlotSpin() => Play(SlotSpin);
    public void PlaySlotWin() => Play(SlotWin);
    public void PlayJackpot() => Play(Jackpot);
    public void PlayBossSpawn() => Play(BossSpawn);
    public void PlayGameOver() => Play(GameOver);
    public void PlayVictory() => Play(Victory);
}
