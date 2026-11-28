using System.Collections.Generic;
using Godot;

/// <summary>Carta de Espadas (♠): para, mira por 0,6s e desliza rápido em linha reta (175% da velocidade).</summary>
public class EspadasBrain : EnemyBrain
{
    const float AimTime = 0.6f;
    const float ChargeMultiplier = 1.75f;
    const float TriggerRange = 6f;   // unidades
    const float ChargeDistance = 7f; // unidades
    const float Recover = 0.6f;

    enum State { Chase, Aim, Charge, Recover }
    State state;
    float timer, cooldown = 1f;
    Vector2 chargeDir;

    public override bool FacePlayer => state == State.Aim;

    public override Vector2 Think(float dt, Vector2 toPlayer)
    {
        timer -= dt;
        cooldown -= dt;
        switch (state)
        {
            case State.Aim:
                SnapVelocity = true;
                if (timer <= 0f) { state = State.Charge; timer = ChargeDistance / (Self.MoveSpeed * ChargeMultiplier); ContactScale = 1.5f; }
                return Vector2.Zero;

            case State.Charge:
                if (timer <= 0f || FreeDistance(Self.GlobalPosition, chargeDir, Self.Radius + 0.3f * Units.Px) < 1f)
                { state = State.Recover; timer = Recover; ContactScale = 1f; }
                return chargeDir * Self.MoveSpeed * ChargeMultiplier * Units.Px;

            case State.Recover:
                if (timer <= 0f) { state = State.Chase; SnapVelocity = false; cooldown = 1.2f; }
                return Vector2.Zero;
        }

        float range = TriggerRange * Units.Px;
        Vector2 playerPos = Self.GlobalPosition + toPlayer;
        if (cooldown <= 0f && toPlayer.LengthSquared() <= range * range && HasLineOfSight(playerPos))
        {
            state = State.Aim;
            timer = AimTime;
            chargeDir = toPlayer.Normalized();
            float len = Mathf.Min(ChargeDistance * Units.Px, FreeDistance(Self.GlobalPosition, chargeDir, ChargeDistance * Units.Px));
            Telegraph.Line(Self, Self.GlobalPosition, Self.GlobalPosition + chargeDir * len, Self.Radius, AimTime, null, () => Self.IsDead);
            return Vector2.Zero;
        }
        return Self.SteerTo(playerPos, Self.MoveSpeed);
    }
}

/// <summary>Carta de Ouros (♦): mantém distância média e dispara um losango a cada 2,2s.</summary>
public class OurosBrain : EnemyBrain
{
    const float FireInterval = 2.2f;
    const float ProjectileSpeed = 6.5f;
    static readonly Color DiamondColor = new Color(1f, 0.2f, 0.25f);

    float fireTimer = 1.2f, strafe = 1f, strafeTimer;

    public override bool FacePlayer => true;

    public override Vector2 Think(float dt, Vector2 toPlayer)
    {
        fireTimer -= dt;
        strafeTimer -= dt;
        if (strafeTimer <= 0f) { strafe = GD.Randf() < 0.5f ? -1f : 1f; strafeTimer = 1.5f + GD.Randf(); }

        Vector2 playerPos = Self.GlobalPosition + toPlayer;
        float range = 9f * Units.Px;
        if (fireTimer <= 0f && toPlayer.LengthSquared() <= range * range && HasLineOfSight(playerPos))
        {
            fireTimer = FireInterval;
            EnemyProjectile.Fire(Self, Self.GlobalPosition, toPlayer, ProjectileSpeed, Data.ProjectileDamage * Self.DamageMultiplier, DiamondColor,
                texture: Data.ProjectileSprite, region: Data.ProjectileRegion);
            Self.PlayAttack();
        }
        return KeepDistance(toPlayer, 4.5f, 7f, Self.MoveSpeed, ref strafe);
    }
}

/// <summary>
/// Carta de Copas (♥, "Bruxa"): não ataca; mantém distância e invoca 3 coraçõezinhos a cada 6s
/// (máx. 6 vivos). Ao morrer, todos os coraçõezinhos dela morrem juntos.
/// </summary>
public class CopasBrain : EnemyBrain
{
    public const float SummonInterval = 4.5f;
    public const int SummonBatch = 5;
    public const int MaxHearts = 10;

    readonly List<Enemy> hearts = new List<Enemy>(MaxHearts);
    float timer = 2f, strafe = 1f;

    public override bool FacePlayer => true;
    public int AliveHearts { get { Prune(); return hearts.Count; } }

    public override Vector2 Think(float dt, Vector2 toPlayer)
    {
        timer -= dt;
        if (timer <= 0f)
        {
            timer = SummonInterval;
            Summon();
        }
        return KeepDistance(toPlayer, 5.5f, 8f, Self.MoveSpeed, ref strafe);
    }

    void Summon()
    {
        if (Data.SummonEnemy == null || EnemySpawner.Instance == null) return;
        int n = EnemyRules.SummonRoom(AliveHearts, SummonBatch, MaxHearts);
        var heart = EnemyData.CreateSummon(Data.SummonEnemy);
        for (int i = 0; i < n; i++)
        {
            Vector2 pos = Self.GlobalPosition + Vector2.FromAngle(Mathf.Tau * i / SummonBatch + GD.Randf()) * 0.9f * Units.Px;
            if (Arena.Instance != null && Arena.Instance.IsSolid(pos)) pos = Self.GlobalPosition;
            hearts.Add(EnemySpawner.Instance.Spawn(heart, pos, Self.HpMultiplier, isExtra: true, damageMultiplier: Self.DamageMultiplier));
        }
    }

    public override void OnDeath()
    {
        Prune();
        var copy = hearts.ToArray();
        hearts.Clear();
        foreach (var h in copy) h.Kill();
    }

    void Prune() => hearts.RemoveAll(h => h == null || !GodotObject.IsInstanceValid(h) || h.IsDead);
}
