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
    [Export] public float MusicVolumeDb { get; set; } = -4f;
    /// <summary>Tempo de troca entre as trilhas (s).</summary>
    [Export] public float MusicFade { get; set; } = 0.8f;

    AudioStreamPlayer source, music;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always; // o caça-níquel toca com o jogo pausado
        source = GetNode<AudioStreamPlayer>("Source");
        StartMusic();
    }

    Tween musicFade;

    void StartMusic()
    {
        music = new AudioStreamPlayer { VolumeDb = MusicVolumeDb, Bus = "Master" };
        AddChild(music);
        // Loop garantido mesmo se a flag do MP3 não pegar: ao terminar, recomeça a mesma faixa.
        music.Finished += () => music.Play();
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
            music.VolumeDb = MusicVolumeDb;
            music.Play();
            return;
        }
        musicFade = CreateTween();
        musicFade.TweenProperty(music, "volume_db", -40f, MusicFade * 0.5f);
        musicFade.TweenCallback(Callable.From(() => { music.Stream = stream; music.Play(); }));
        musicFade.TweenProperty(music, "volume_db", MusicVolumeDb, MusicFade * 0.5f);
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
