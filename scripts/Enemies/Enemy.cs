using Godot;

/// <summary>
/// Inimigo animado: persegue o jogador via RigidBody2D (usando FlowField na arena e flanqueamento na Ficha),
/// com suporte a variante Dourada, divisão "Troco" (Fichona) e explosão + poça (Garrafa de Champanhe).
/// </summary>
public partial class Enemy : RigidBody2D
{
    public EnemyData Data { get; private set; }
    /// <summary>Raio em pixels.</summary>
    public float Radius { get; private set; }

    /// <summary>Naipes e bosses: decide movimento e ataques. Null = perseguição padrão.</summary>
    public EnemyBrain Brain { get; private set; }
    public float Hp => hp;
    public float MaxHp { get; private set; }
    public float DamageMultiplier => damageMultiplierUsed;
    public float HpMultiplier => hpMultiplierUsed;
    /// <summary>Velocidade de movimento (u/s) já com a escala de dificuldade da fase.</summary>
    public float MoveSpeed => Data.MoveSpeed * speedMultiplierUsed;
    float speedMultiplierUsed = 1f;
    public bool IsDead => dead;
    public AnimatedSprite2D Sprite => sprite;

    /// <summary>Índice na lista ativa do EnemySpawner (remoção O(1) por swap).</summary>
    public int ActiveIndex = -1;

    /// <summary>Peso do afastamento entre vizinhos (evita a horda virar fila única nos corredores).</summary>
    const float SeparationWeight = 0.6f;
    /// <summary>Quão rápido a velocidade alcança a desejada (1/s): dá inércia sem perder resposta.</summary>
    const float Steering = 12f;
    /// <summary>Unidades: além desta distância o flanqueador mira o ponto ao redor do jogador.</summary>
    const float FlankRange = 5f;
    const float FlankRadius = 3f;

    AnimatedSprite2D sprite;
    CollisionShape2D shape;
    Vector2 baseSpriteScale = Vector2.One;
    string currentAnim;
    float hp, hpMultiplierUsed = 1f, damageMultiplierUsed = 1f, contactTimer, flashTimer, flankAngle, fuseTimer;
    Vector2 entry;
    bool entering, fusing, dead;

    public override void _Ready()
    {
        sprite = GetNode<AnimatedSprite2D>("Sprite");
        shape = GetNode<CollisionShape2D>("Shape");
    }

    /// <summary>Chamar depois de AddChild (precisa dos nós filhos).</summary>
    public void Init(EnemyData data, float hpMultiplier, float damageMultiplier = 1f)
    {
        Data = data;
        hpMultiplierUsed = hpMultiplier;
        damageMultiplierUsed = Mathf.Max(0.1f, damageMultiplier);
        hp = data.FixedHp ? data.MaxHp : data.MaxHp * hpMultiplier;
        speedMultiplierUsed = HordeDirector.Instance?.EnemySpeedMultiplier ?? 1f;
        MaxHp = hp;
        Mass = data.Mass;
        dead = false;
        fusing = false;
        fuseTimer = 0f;
        contactTimer = 0f;
        flashTimer = 0f;
        currentAnim = null;

        CollisionLayer = 4;
        CollisionMask = 7;
        SetPhysicsProcess(true);
        if (shape != null) shape.Disabled = false;

        var frames = data.GetOrBuildFrames();
        sprite.SpriteFrames = frames;
        float frameW = Mathf.Max(1, data.FrameSize);
        if (data.ChaseSheet == null && data.IdleSheet == null && data.Sprite != null)
            frameW = Mathf.Max(1, data.Sprite.GetWidth());

        float s = data.SpriteScale > 0f ? data.SpriteScale : (Units.Px * data.Scale / frameW);
        baseSpriteScale = Vector2.One * s;
        sprite.Scale = baseSpriteScale;
        sprite.Modulate = data.Color;
        sprite.FlipH = false;

        string initialAnim = frames.HasAnimation("chase") ? "chase" : (frames.HasAnimation("idle") ? "idle" : "");
        if (initialAnim.Length > 0)
        {
            PlayAnim(initialAnim);
            int count = frames.GetFrameCount(initialAnim);
            if (count > 1) sprite.Frame = GD.RandRange(0, count - 1);
        }

        ZIndex = data.IsBoss ? 2 : 1;
        Radius = data.Scale * 0.5f * Units.Px;
        shape.Shape = new CircleShape2D { Radius = Radius }; // um shape por inimigo: o raio varia com o tipo
        flankAngle = GD.Randf() * Mathf.Tau;
        entering = false;
        sprite.Offset = Vector2.Zero;
        Brain = EnemyBrain.Create(data.Behavior);
        Brain?.Attach(this);
    }

    void PlayAnim(string name)
    {
        if (currentAnim == name || sprite?.SpriteFrames == null || !sprite.SpriteFrames.HasAnimation(name)) return;
        currentAnim = name;
        sprite.Play(name);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (dead) return;
        var player = PlayerController.Instance;
        if (player == null || (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)) return;
        float dt = (float)delta;

        Vector2 pos = GlobalPosition;
        Vector2 toPlayer = player.GlobalPosition - pos;

        if (fusing)
        {
            LinearVelocity = LinearVelocity.Lerp(Vector2.Zero, Mathf.Min(1f, 18f * dt));
            fuseTimer -= dt;
            QueueRedraw();
            if (fuseTimer <= 0f) ResolveDeath(killedByPlayer: false);
            return;
        }

        if (!entering && Data.Explodes)
        {
            float triggerDist = Radius + player.RadiusPx + Data.ExplosionTriggerRange * Units.Px;
            if (toPlayer.LengthSquared() <= triggerDist * triggerDist)
            {
                fusing = true;
                fuseTimer = Mathf.Max(0.05f, Data.ExplosionFuse);
                PlayAnim(sprite.SpriteFrames.HasAnimation("idle") ? "idle" : "chase");
                QueueRedraw();
                return;
            }
        }

        Vector2 desired;
        if (entering) desired = WithSeparation(EnterDirection(pos), MoveSpeed);
        else if (Brain != null) desired = Brain.Think(dt, toPlayer);
        else desired = WithSeparation(ChaseDirection(pos, player.GlobalPosition, toPlayer), MoveSpeed);

        bool snap = !entering && Brain != null && Brain.SnapVelocity;
        LinearVelocity = snap ? desired : LinearVelocity.Lerp(desired, Mathf.Min(1f, Steering * dt));

        if (attackTimer > 0f) attackTimer -= dt;
        bool facingPlayer = Brain != null && Brain.FacePlayer;
        Vector2 look = facingPlayer ? toPlayer : LinearVelocity;
        if (LinearVelocity.LengthSquared() > 16f || facingPlayer)
        {
            bool back = sprite.SpriteFrames.HasAnimation("chase_back") && look.Y < 0f && Mathf.Abs(look.Y) > Mathf.Abs(look.X);
            if (attackTimer > 0f && sprite.SpriteFrames.HasAnimation("attack"))
                PlayAnim(back && sprite.SpriteFrames.HasAnimation("attack_back") ? "attack_back" : "attack");
            else PlayAnim(back ? "chase_back" : "chase");
        }
        else if (sprite.SpriteFrames.HasAnimation("idle")) PlayAnim("idle");

        if (Mathf.Abs(look.X) > 12f) sprite.FlipH = look.X < 0f;

        if (Brain != null && Brain.NeedsRedraw) QueueRedraw();

        if (!Data.Explodes && (Brain == null || Brain.ContactDamage))
        {
            contactTimer -= dt;
            float reach = Radius + player.RadiusPx + 0.1f * Units.Px;
            if (contactTimer <= 0f && toPlayer.LengthSquared() <= reach * reach)
            {
                player.TakeDamage(Data.ContactDamage * damageMultiplierUsed * (Brain?.ContactScale ?? 1f));
                contactTimer = Data.ContactCooldown;
            }
        }
    }

    /// <summary>Direção (normalizada ou zero) somada ao afastamento dos vizinhos, virada em velocidade px/s.</summary>
    public Vector2 WithSeparation(Vector2 dir, float speedUnits)
    {
        dir += EnemySpawner.Instance.Separation(this) * SeparationWeight;
        return dir.LengthSquared() > 0.0001f ? dir.Normalized() * speedUnits * Units.Px : Vector2.Zero;
    }

    /// <summary>Velocidade (px/s) indo até <paramref name="target"/> contornando obstáculos da arena.</summary>
    public Vector2 SteerTo(Vector2 target, float speedUnits)
    {
        var arena = Arena.Instance;
        Vector2 dir = arena == null ? (target - GlobalPosition).Normalized() : arena.Steer(GlobalPosition, target);
        return WithSeparation(dir, speedUnits);
    }

    /// <summary>Move o corpo físico na hora (pulo do Tigrinho).</summary>
    public void Teleport(Vector2 pos)
    {
        PhysicsServer2D.BodySetState(GetRid(), PhysicsServer2D.BodyState.Transform, new Transform2D(0f, pos));
        GlobalPosition = pos;
    }

    /// <summary>Liga/desliga a colisão (o tigre passa por cima dos obstáculos no pulo).</summary>
    public void SetSolid(bool solid)
    {
        CollisionLayer = solid ? 4u : 0u;
        CollisionMask = solid ? 7u : 0u;
    }

    float attackTimer;

    /// <summary>Toca a animação de ataque (se houver) por alguns instantes.</summary>
    public void PlayAttack(float seconds = 0.35f) => attackTimer = seconds;

    /// <summary>Morte sem recompensa e sem explosão (invocados quando o dono cai).</summary>
    public void Kill() => ResolveDeath(killedByPlayer: false, explode: false);

    /// <summary>Ao sair de uma porta, anda até este ponto antes de perseguir (não fica preso no batente).</summary>
    public void SetEntry(Vector2 worldPos)
    {
        entry = worldPos;
        entering = true;
    }

    Vector2 EnterDirection(Vector2 pos)
    {
        Vector2 d = entry - pos;
        float arrive = 0.3f * Units.Px;
        if (d.LengthSquared() <= arrive * arrive) entering = false;
        return d.Normalized();
    }

    /// <summary>
    /// Flanqueadores longe do jogador miram um ponto ao redor dele (cada um no seu ângulo) para cercar;
    /// perto, vão direto. Com arena, contorna obstáculos pelo flow field.
    /// </summary>
    Vector2 ChaseDirection(Vector2 pos, Vector2 playerPos, Vector2 toPlayer)
    {
        Vector2 target = playerPos;
        float flankRange = FlankRange * Units.Px;
        if (Data.Flanker && toPlayer.LengthSquared() > flankRange * flankRange)
            target += Vector2.FromAngle(flankAngle) * FlankRadius * Units.Px;

        var arena = Arena.Instance;
        return arena == null ? (target - pos).Normalized() : arena.Steer(pos, target);
    }

    public override void _Process(double delta)
    {
        if (dead) return;
        float dt = (float)delta;

        if (fusing)
        {
            float total = Mathf.Max(0.05f, Data.ExplosionFuse);
            float progress = Mathf.Clamp(1f - fuseTimer / total, 0f, 1f);
            sprite.Scale = baseSpriteScale * (1f + 0.20f * progress);
            if (flashTimer > 0f)
            {
                flashTimer -= dt;
                return;
            }
            bool blink = ((int)(progress * 10f) & 1) == 0;
            sprite.Modulate = blink ? new Color(1f, 0.35f, 0.25f) : Data.Color;
            return;
        }

        if (flashTimer <= 0f) return;
        flashTimer -= dt;
        if (flashTimer <= 0f) sprite.Modulate = Data.Color;
    }

    public override void _Draw()
    {
        if (!dead) Brain?.Draw(this);
        if (!fusing || dead) return;
        float total = Mathf.Max(0.05f, Data.ExplosionFuse);
        float progress = Mathf.Clamp(1f - fuseTimer / total, 0f, 1f);
        float blastR = Data.ExplosionRadius * Units.Px;
        Color warnColor = Data.IsGolden ? new Color(1f, 0.78f, 0.15f) : new Color(1f, 0.28f, 0.22f);

        DrawCircle(Vector2.Zero, blastR * progress, new Color(warnColor, 0.22f));
        DrawArc(Vector2.Zero, blastR, 0f, Mathf.Tau, 36, new Color(warnColor, 0.85f), 2.5f);
    }

    public void TakeDamage(float amount)
    {
        if (dead || hp <= 0f) return;
        if (Brain != null) amount = Brain.FilterDamage(amount);
        if (amount <= 0f)
        {
            sprite.Modulate = new Color(1f, 0.85f, 0.3f); // invulnerável: brilho dourado em vez do flash
            flashTimer = 0.06f;
            return;
        }
        hp -= amount;
        sprite.Modulate = Colors.White;
        flashTimer = 0.06f;
        AudioManager.Instance?.PlayEnemyHit();
        if (hp <= 0f) ResolveDeath(killedByPlayer: true);
    }

    void ResolveDeath(bool killedByPlayer, bool explode = true)
    {
        if (dead) return;
        dead = true;
        fusing = false;
        QueueRedraw();
        Brain?.OnDeath();
        sprite.Offset = Vector2.Zero;

        AudioManager.Instance?.PlayEnemyDeath();

        if (killedByPlayer)
        {
            GameManager.Instance?.AddScore(Data.Points);
            GameManager.Instance?.AddDollars(Data.Dollars);
            DropManager.Instance?.RollDrops(Data, GlobalPosition);
        }
        else if (Data.GuaranteedChip)
        {
            // PDF (Garrafa Dourada): caso exploda por conta própria, a ficha cai em cima da poça.
            GameManager.Instance?.AddDollars(Data.Dollars);
            DropManager.Instance?.RollDrops(Data, GlobalPosition);
        }

        if (Data.Explodes && explode) TriggerExplosion(dealBlastDamage: !killedByPlayer);

        // Importante: spawna o "Troco" ANTES do Despawn para o HordeDirector somar os novos alvos antes de checar fim do round.
        if (Data.SplitEnemy != null && Data.SplitMaxCount > 0) SpawnTrocoMinions();

        bool boss = Data.IsBoss;
        bool hasDeathAnim = sprite?.SpriteFrames != null && sprite.SpriteFrames.HasAnimation("death");
        EnemySpawner.Instance?.Despawn(this, freeNode: !hasDeathAnim);

        if (hasDeathAnim)
        {
            SetPhysicsProcess(false);
            LinearVelocity = Vector2.Zero;
            CollisionLayer = 0;
            CollisionMask = 0;
            if (shape != null) shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
            sprite.Scale = baseSpriteScale;
            sprite.Modulate = Data.Color;
            ZIndex = 0;
            currentAnim = "death";
            sprite.Play("death");
            sprite.AnimationFinished += QueueFree;
        }

        // Na fase com rounds quem encerra é a saída; no modo antigo o boss vence a partida.
        if (boss && HordeDirector.Instance == null) GameManager.Instance?.Victory();
    }

    void TriggerExplosion(bool dealBlastDamage)
    {
        float blastPx = Data.ExplosionRadius * Units.Px;
        if (dealBlastDamage && PlayerController.Instance != null)
        {
            var player = PlayerController.Instance;
            float reach = blastPx + player.RadiusPx;
            if ((player.GlobalPosition - GlobalPosition).LengthSquared() <= reach * reach)
                player.TakeDamage(Data.ExplosionDamage * damageMultiplierUsed);
        }

        var root = GetTree()?.CurrentScene;
        if (root == null) return;
        var puddle = new ChampagnePuddle();
        root.AddChild(puddle);
        float duration = Data.IsGolden && Data.GoldenPuddleDuration > 0f ? Data.GoldenPuddleDuration : Data.PuddleDuration;
        puddle.Setup(GlobalPosition, Data.PuddleRadius * Units.Px, blastPx, Data.PuddleDamagePerSecond * damageMultiplierUsed, duration, Data.IsGolden);
    }

    void SpawnTrocoMinions()
    {
        var spawner = EnemySpawner.Instance;
        if (spawner == null) return;

        int min = Mathf.Max(1, Data.SplitMinCount);
        int max = Mathf.Max(min, Data.SplitMaxCount);
        int count = GD.RandRange(min, max);
        var minionData = EnemyData.CreateTrocoMinion(Data.SplitEnemy);
        float baseAngle = GD.Randf() * Mathf.Tau;
        float offset = 0.55f * Units.Px;

        for (int i = 0; i < count; i++)
        {
            Vector2 spawnPos = GlobalPosition + Vector2.FromAngle(baseAngle + Mathf.Tau * i / count) * offset;
            if (Arena.Instance != null)
            {
                Rect2 inner = Arena.Instance.Bounds.Grow(-1.1f * Units.Px);
                spawnPos = spawnPos.Clamp(inner.Position, inner.End);
            }
            spawner.Spawn(minionData, spawnPos, hpMultiplierUsed, isExtra: true, damageMultiplier: damageMultiplierUsed);
        }
    }
}
