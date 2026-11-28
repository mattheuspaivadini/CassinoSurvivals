using Godot;

/// <summary>Cérebro que controla o inimigo. Chase = perseguição padrão (Ficha, Fichona, Garrafa, Paus).</summary>
public enum EnemyBehavior { Chase, Espadas, Ouros, Copas, Rainha, Rei, Tigrinho }

/// <summary>Tipo de inimigo (comum, Dourado/elite ou boss). Uma única cena lê estes dados no spawn.</summary>
[GlobalClass]
public partial class EnemyData : Resource
{
    public const float GoldenHpMultiplier = 1.8f;
    public const float GoldenDamageMultiplier = 1.3f;
    public const float GoldenScaleMultiplier = 1.12f;
    public static readonly Color GoldenTint = new Color(1f, 0.84f, 0.22f, 1f);

    [Export] public string EnemyName { get; set; } = "Ficha";
    /// <summary>Fallback estático caso não haja spritesheet configurada.</summary>
    [Export] public Texture2D Sprite { get; set; }
    [Export] public Color Color { get; set; } = Colors.White;
    /// <summary>Diâmetro da colisão/hitbox em unidades de mundo (1u = 60px).</summary>
    [Export] public float Scale { get; set; } = 0.75f;
    [Export] public float Mass { get; set; } = 1f;

    [ExportGroup("Spritesheet (Animação)")]
    [Export] public Texture2D IdleSheet { get; set; }
    [Export] public int IdleFrames { get; set; }
    [Export] public Texture2D ChaseSheet { get; set; }
    [Export] public int ChaseFrames { get; set; }
    [Export] public Texture2D DeathSheet { get; set; }
    [Export] public int DeathFrames { get; set; }
    [Export] public int FrameSize { get; set; } = 32;
    /// <summary>Altura do frame quando não é quadrado (0 = igual a FrameSize).</summary>
    [Export] public int FrameHeight { get; set; }
    /// <summary>Folha "de costas", usada quando o inimigo anda para cima.</summary>
    [Export] public Texture2D ChaseBackSheet { get; set; }
    /// <summary>Animação curta de ataque (frames empilhados na vertical, quadrados), de frente e de costas.</summary>
    [Export] public Texture2D AttackSheet { get; set; }
    [Export] public Texture2D AttackBackSheet { get; set; }
    /// <summary>Sprite do projétil (aponta para cima na textura); vazio = losango desenhado.</summary>
    [Export] public Texture2D ProjectileSprite { get; set; }
    [Export] public Rect2 ProjectileRegion { get; set; }
    [Export] public float AnimFps { get; set; } = 11f;
    /// <summary>Escala pixel-art aplicada ao AnimatedSprite2D (se &lt;= 0, calcula a partir de Scale).</summary>
    [Export] public float SpriteScale { get; set; } = 2.6f;

    [ExportGroup("Combate")]
    [Export] public float MaxHp { get; set; } = 20f;
    [Export] public float ContactDamage { get; set; } = 8f;
    [Export] public float ContactCooldown { get; set; } = 0.6f;
    [Export] public float MoveSpeed { get; set; } = 4.2f;
    /// <summary>Se true, o HP não escala com o multiplicador de round/tempo.</summary>
    [Export] public bool FixedHp { get; set; }
    /// <summary>Enxame (Ficha): mira um ângulo ao redor quando longe para cercar o jogador.</summary>
    [Export] public bool Flanker { get; set; }
    [Export] public EnemyBehavior Behavior { get; set; } = EnemyBehavior.Chase;
    /// <summary>Paus: não sofre empurrão (usado pelo knockback da passiva All-In, #21).</summary>
    [Export] public bool KnockbackResistant { get; set; }
    /// <summary>Dano de cada projétil (Ouros e bosses).</summary>
    [Export] public float ProjectileDamage { get; set; } = 8f;
    /// <summary>Invocado pela Copas (coraçõezinhos).</summary>
    [Export] public EnemyData SummonEnemy { get; set; }

    [ExportGroup("Variante Dourada (Elite VIP)")]
    [Export] public bool IsGolden { get; set; }

    [ExportGroup("Troco (Divisão ao Morrer)")]
    [Export] public EnemyData SplitEnemy { get; set; }
    [Export] public int SplitMinCount { get; set; }
    [Export] public int SplitMaxCount { get; set; }

    [ExportGroup("Explosão e Poça (Garrafa)")]
    [Export] public bool Explodes { get; set; }
    [Export] public float ExplosionDamage { get; set; } = 22f;
    /// <summary>Raio da explosão em unidades.</summary>
    [Export] public float ExplosionRadius { get; set; } = 1.9f;
    /// <summary>Distância extra (unidades) em que a garrafa inicia o aviso de explosão.</summary>
    [Export] public float ExplosionTriggerRange { get; set; } = 1.15f;
    /// <summary>Tempo de telegrafia antes de explodir (PDF: 0,5s).</summary>
    [Export] public float ExplosionFuse { get; set; } = 0.5f;
    [Export] public float PuddleDamagePerSecond { get; set; } = 6f;
    /// <summary>Raio da poça de champanhe em unidades.</summary>
    [Export] public float PuddleRadius { get; set; } = 1.6f;
    /// <summary>Duração da poça normal e da versão Dourada (PDF: 4s normal, 6s Dourada).</summary>
    [Export] public float PuddleDuration { get; set; } = 4f;
    [Export] public float GoldenPuddleDuration { get; set; } = 6f;

    [ExportGroup("Recompensa")]
    [Export] public int Points { get; set; } = 15;
    [Export] public bool GuaranteedChip { get; set; }
    /// <summary>Quantas fichas o drop garantido solta (Dourado = 1, bosses = 3 a 5).</summary>
    [Export] public int ChipDrops { get; set; } = 1;
    /// <summary>Dinheiro ($) da Loja Externa pago ao morrer (PDF: comum $1, Dourado $5, invocados $0).</summary>
    [Export] public int Dollars { get; set; } = 1;
    [Export(PropertyHint.Range, "0,1")] public float ChipDropChance { get; set; } = 0.005f;
    [Export(PropertyHint.Range, "0,1")] public float HealDropChance { get; set; } = 0.03f;
    [Export(PropertyHint.Range, "0,1")] public float PointsDropChance { get; set; } = 0.05f;

    [ExportGroup("Boss")]
    [Export] public bool IsBoss { get; set; }
    /// <summary>Armadura do boss: dano acima disto no mesmo segundo entra a 20% (vem da fase no HordeDirector).</summary>
    [Export] public float BossDpsCap { get; set; }

    SpriteFrames cachedFrames;

    /// <summary>Constrói uma única vez e reaproveita o SpriteFrames entre todos os spawns deste tipo.</summary>
    public SpriteFrames GetOrBuildFrames()
    {
        if (cachedFrames != null) return cachedFrames;

        var frames = new SpriteFrames();
        if (frames.HasAnimation("default")) frames.RemoveAnimation("default");

        int size = Mathf.Max(1, FrameSize);
        AddSheetAnimation(frames, "idle", IdleSheet, IdleFrames, size, Mathf.Max(1f, AnimFps * 0.8f), loop: true, FrameHeight > 0 ? FrameHeight : size);
        int height = FrameHeight > 0 ? FrameHeight : size;
        AddSheetAnimation(frames, "chase", ChaseSheet, ChaseFrames, size, AnimFps, loop: true, height);
        AddSheetAnimation(frames, "chase_back", ChaseBackSheet, ChaseFrames, size, AnimFps, loop: true, height);
        AddVerticalAnimation(frames, "attack", AttackSheet, AnimFps);
        AddVerticalAnimation(frames, "attack_back", AttackBackSheet, AnimFps);
        AddSheetAnimation(frames, "death", DeathSheet, DeathFrames, size, AnimFps * 1.15f, loop: false, FrameHeight > 0 ? FrameHeight : size);

        if (!frames.HasAnimation("chase") && Sprite != null)
        {
            frames.AddAnimation("chase");
            frames.SetAnimationSpeed("chase", 1f);
            frames.SetAnimationLoopMode("chase", SpriteFrames.LoopMode.None);
            frames.AddFrame("chase", Sprite);
        }

        cachedFrames = frames;
        return cachedFrames;
    }

    static void AddSheetAnimation(SpriteFrames frames, string name, Texture2D sheet, int explicitCount, int frameSize, float fps, bool loop, int frameHeightWanted = 0)
    {
        if (sheet == null) return;
        int count = explicitCount > 0 ? explicitCount : Mathf.Max(1, sheet.GetWidth() / frameSize);
        int frameHeight = Mathf.Min(frameHeightWanted > 0 ? frameHeightWanted : frameSize, sheet.GetHeight());
        StringName anim = name;
        frames.AddAnimation(anim);
        frames.SetAnimationSpeed(anim, fps);
        frames.SetAnimationLoopMode(anim, loop ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None);
        for (int i = 0; i < count; i++)
        {
            frames.AddFrame(anim, new AtlasTexture
            {
                Atlas = sheet,
                Region = new Rect2(i * frameSize, 0, frameSize, frameHeight)
            });
        }
    }

    static void AddVerticalAnimation(SpriteFrames frames, string name, Texture2D sheet, float fps)
    {
        if (sheet == null) return;
        int w = sheet.GetWidth();
        int count = Mathf.Max(1, sheet.GetHeight() / w);
        frames.AddAnimation(name);
        frames.SetAnimationSpeed(name, fps);
        frames.SetAnimationLoopMode(name, SpriteFrames.LoopMode.None);
        for (int i = 0; i < count; i++)
            frames.AddFrame(name, new AtlasTexture { Atlas = sheet, Region = new Rect2(0, i * w, w, w) });
    }

    /// <summary>Duplica os dados compartilhando a instância já construída de SpriteFrames.</summary>
    public EnemyData CloneWithSharedFrames()
    {
        var shared = GetOrBuildFrames();
        var clone = (EnemyData)Duplicate();
        clone.cachedFrames = shared;
        return clone;
    }

    /// <summary>
    /// Aplica os modificadores globais de Criatura Dourada (Balanceamento Numérico, pág. 3):
    /// 1,80x HP, 1,30x Dano, +12% tamanho visual/hitbox, 1 Ficha garantida + $5.
    /// </summary>
    public static EnemyData CreateGoldenVariant(EnemyData source)
    {
        var g = source.CloneWithSharedFrames();
        if (source.IsGolden) return g;

        g.IsGolden = true;
        g.EnemyName = source.EnemyName + " (Dourado)";
        g.MaxHp = source.MaxHp * GoldenHpMultiplier;
        g.ContactDamage = source.ContactDamage * GoldenDamageMultiplier;
        g.ExplosionDamage = source.ExplosionDamage * GoldenDamageMultiplier;
        g.PuddleDamagePerSecond = source.PuddleDamagePerSecond * GoldenDamageMultiplier;
        g.PuddleDuration = source.GoldenPuddleDuration > 0f ? source.GoldenPuddleDuration : 6f;
        g.Scale = source.Scale * GoldenScaleMultiplier;
        if (source.SpriteScale > 0f) g.SpriteScale = source.SpriteScale * GoldenScaleMultiplier;
        g.Color = GoldenTint;
        g.GuaranteedChip = true;
        g.ChipDrops = 1;
        g.Dollars = 5;
        g.ChipDropChance = 0f;
        g.Points = Mathf.RoundToInt(source.Points * 2.5f);
        return g;
    }

    /// <summary>
    /// Trava contra farm infinito e trava da Fichona Dourada (PDF, seções 7 e 9):
    /// fichas geradas no "Troco" nunca nascem Douradas, rendem $0 e têm 0% de drop.
    /// </summary>
    /// <summary>
    /// Invocados (coraçõezinhos da Copas, Guarda da Corte, Decreto, lacaios): PDF seção 7 —
    /// nunca Dourados, $0 e sem drop.
    /// </summary>
    public static EnemyData CreateSummon(EnemyData source)
    {
        var m = source.CloneWithSharedFrames();
        m.IsGolden = false;
        m.Color = source.IsGolden ? Colors.White : source.Color;
        m.Dollars = 0;
        m.GuaranteedChip = false;
        m.ChipDrops = 0;
        m.ChipDropChance = 0f;
        m.HealDropChance = 0f;
        m.PointsDropChance = 0f;
        m.SplitEnemy = null;
        m.SplitMinCount = 0;
        m.SplitMaxCount = 0;
        return m;
    }

    public static EnemyData CreateTrocoMinion(EnemyData fichaSource)
    {
        var m = fichaSource.CloneWithSharedFrames();
        m.IsGolden = false;
        m.Color = Colors.White;
        m.Dollars = 0;
        m.Points = 5;
        m.GuaranteedChip = false;
        m.ChipDrops = 0;
        m.ChipDropChance = 0f;
        m.HealDropChance = 0f;
        m.PointsDropChance = 0f;
        m.SplitEnemy = null;
        m.SplitMinCount = 0;
        m.SplitMaxCount = 0;
        return m;
    }
}
