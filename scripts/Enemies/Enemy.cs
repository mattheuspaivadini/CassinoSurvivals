using Godot;

/// <summary>Inimigo: persegue o jogador via RigidBody2D (empurrões entre a horda) e causa dano de contato por distância.</summary>
public partial class Enemy : RigidBody2D
{
    public EnemyData Data { get; private set; }
    /// <summary>Raio em pixels.</summary>
    public float Radius { get; private set; }

    /// <summary>Índice na lista ativa do EnemySpawner (remoção O(1) por swap).</summary>
    public int ActiveIndex = -1;

    /// <summary>Peso do afastamento entre vizinhos (evita a horda virar fila única nos corredores).</summary>
    const float SeparationWeight = 0.6f;
    /// <summary>Quão rápido a velocidade alcança a desejada (1/s): dá inércia sem perder resposta.</summary>
    const float Steering = 12f;
    /// <summary>Unidades: além desta distância o flanqueador mira o ponto ao redor do jogador.</summary>
    const float FlankRange = 5f;
    const float FlankRadius = 3f;

    Sprite2D sprite;
    CollisionShape2D shape;
    float hp, contactTimer, flashTimer, flankAngle;
    Vector2 entry;
    bool entering;

    public override void _Ready()
    {
        sprite = GetNode<Sprite2D>("Sprite");
        shape = GetNode<CollisionShape2D>("Shape");
    }

    /// <summary>Chamar depois de AddChild (precisa dos nós filhos).</summary>
    public void Init(EnemyData data, float hpMultiplier)
    {
        Data = data;
        hp = data.FixedHp ? data.MaxHp : data.MaxHp * hpMultiplier;
        Mass = data.Mass;
        if (data.Sprite != null) sprite.Texture = data.Sprite;
        Units.Fit(sprite, data.Scale);
        sprite.Modulate = data.Color;
        ZIndex = data.IsBoss ? 2 : 1;
        Radius = data.Scale * 0.5f * Units.Px;
        shape.Shape = new CircleShape2D { Radius = Radius }; // um shape por inimigo: o raio varia com o tipo
        flankAngle = GD.Randf() * Mathf.Tau;
        entering = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        var player = PlayerController.Instance;
        if (player == null) return;
        float dt = (float)delta;

        Vector2 pos = GlobalPosition;
        Vector2 toPlayer = player.GlobalPosition - pos;
        Vector2 dir = entering ? EnterDirection(pos) : ChaseDirection(pos, player.GlobalPosition, toPlayer);
        dir += EnemySpawner.Instance.Separation(this) * SeparationWeight;
        Vector2 desired = dir.LengthSquared() > 0.0001f ? dir.Normalized() * Data.MoveSpeed * Units.Px : Vector2.Zero;
        LinearVelocity = LinearVelocity.Lerp(desired, Mathf.Min(1f, Steering * dt));

        contactTimer -= dt;
        float reach = Radius + player.RadiusPx + 0.1f * Units.Px;
        if (contactTimer <= 0f && toPlayer.LengthSquared() <= reach * reach)
        {
            player.TakeDamage(Data.ContactDamage);
            contactTimer = Data.ContactCooldown;
        }
    }

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
        if (flashTimer <= 0f) return;
        flashTimer -= (float)delta;
        if (flashTimer <= 0f) sprite.Modulate = Data.Color;
    }

    public void TakeDamage(float amount)
    {
        if (hp <= 0f) return;
        hp -= amount;
        sprite.Modulate = Colors.White;
        flashTimer = 0.06f;
        AudioManager.Instance?.PlayEnemyHit();
        if (hp <= 0f) Die();
    }

    void Die()
    {
        AudioManager.Instance?.PlayEnemyDeath();
        GameManager.Instance.AddScore(Data.Points);
        DropManager.Instance.RollDrops(Data, GlobalPosition);
        bool boss = Data.IsBoss;
        EnemySpawner.Instance.Despawn(this);
        // Na fase com rounds quem encerra é a saída; no modo antigo o boss vence a partida.
        if (boss && HordeDirector.Instance == null) GameManager.Instance.Victory();
    }
}
