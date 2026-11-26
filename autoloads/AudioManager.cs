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
    [Export] public float MusicVolumeDb { get; set; } = -10f;

    AudioStreamPlayer source, music;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always; // o caça-níquel toca com o jogo pausado
        source = GetNode<AudioStreamPlayer>("Source");
        StartMusic();
    }

    void StartMusic()
    {
        if (Music == null) return;
        if (Music is AudioStreamMP3 mp3) mp3.Loop = true;
        music = new AudioStreamPlayer { Stream = Music, VolumeDb = MusicVolumeDb, Bus = "Master" };
        AddChild(music);
        music.Play();
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
