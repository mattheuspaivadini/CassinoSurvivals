using Godot;

/// <summary>Inimigo: persegue o jogador via RigidBody2D (empurrões entre a horda) e causa dano de contato por distância.</summary>
public partial class Enemy : RigidBody2D
{
    public EnemyData Data { get; private set; }
    /// <summary>Raio em pixels.</summary>
    public float Radius { get; private set; }

    /// <summary>Índice na lista ativa do EnemySpawner (remoção O(1) por swap).</summary>
    public int ActiveIndex = -1;

    Sprite2D sprite;
    CollisionShape2D shape;
    float hp, contactTimer, flashTimer;

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
    }

    public override void _PhysicsProcess(double delta)
    {
        var player = PlayerController.Instance;
        if (player == null) return;

        Vector2 toPlayer = player.GlobalPosition - GlobalPosition;
        LinearVelocity = toPlayer.Normalized() * Data.MoveSpeed * Units.Px;

        contactTimer -= (float)delta;
        float reach = Radius + player.RadiusPx + 0.1f * Units.Px;
        if (contactTimer <= 0f && toPlayer.LengthSquared() <= reach * reach)
        {
            player.TakeDamage(Data.ContactDamage);
            contactTimer = Data.ContactCooldown;
        }
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
        if (boss) GameManager.Instance.Victory();
    }
}
