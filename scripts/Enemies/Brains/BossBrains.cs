using Godot;

/// <summary>
/// A Rainha (Fase 5): ágil, média distância. Leque de Cartas, Espiral Real e Guarda da Corte.
/// Fúria: mais rápida e leque duplo. Trava: escudo, espiral intensa e 2 Cartas de Espadas.
/// </summary>
public class RainhaBrain : BossBrain
{
    static readonly Color CardColor = new Color(0.95f, 0.3f, 0.75f);
    const float ShotSpeed = 6.5f;
    float strafe = 1f;

    protected override float IdleGap => 1.6f;

    protected override Vector2 IdleMove(float dt, Vector2 toPlayer) => KeepDistance(toPlayer, 5f, 8f, Speed, ref strafe);

    protected override void ChooseAttack()
    {
        strafe = -strafe;
        float r = GD.Randf();
        if (r < 0.45f) Fan(Fury ? 2 : 1);
        else if (r < 0.8f) Spiral(3f, 0.12f);
        else Guard();
    }

    void Fan(int times)
    {
        for (int i = 0; i < times; i++)
        {
            Vector2 aim = Vector2.Zero;
            Then(0.6f, () =>
            {
                Self.PlayAttack(0.95f);
                aim = PlayerPos - Self.GlobalPosition;
                Telegraph.Cone(Self, Self.GlobalPosition, aim, 6f * Units.Px, 50f, 0.6f, () => FireFan(aim, 5, 50f, ShotSpeed, CardColor), () => Self.IsDead);
            });
            Hold(0.35f);
        }
        Hold(0.4f);
    }

    void Spiral(float seconds, float interval)
    {
        Then(1.2f, null, (dt, _) => Self.SteerTo(ArenaCenter, Speed * 1.5f));
        float angle = 0f, shotTimer = 0f;
        Then(seconds, () => Self.PlayAttack(seconds), (dt, _) =>
        {
            shotTimer -= dt;
            if (shotTimer <= 0f)
            {
                shotTimer = interval;
                angle += Mathf.DegToRad(17f);
                for (int arm = 0; arm < 2; arm++)
                    EnemyProjectile.Fire(Self, Self.GlobalPosition, Vector2.FromAngle(angle + Mathf.Pi * arm), ShotSpeed * 0.8f,
                        Data.ProjectileDamage * Self.DamageMultiplier, CardColor);
            }
            return Vector2.Zero;
        });
        Hold(0.6f);
    }

    void Guard()
    {
        Then(0.8f, () =>
        {
            Self.PlayAttack(0.8f);
            Summon(EnemyKind.Ficha, 4, 8);
            Summon(EnemyKind.CartaEspadas, 1, 8);
        });
    }

    protected override void LockSequence()
    {
        Then(0.1f, () => Summon(EnemyKind.CartaEspadas, 2, 10));
        Spiral(7f, 0.08f);
    }
}

/// <summary>
/// O Rei de Paus (Fase 10): pesado, avança firme. Impacto do Cetro, Avanço Soberano e Decreto.
/// Fúria: onda de choque extra e investida dupla. Trava: Cetro, Avanço, Cetro + Paus e Ouros.
/// </summary>
public class ReiBrain : BossBrain
{
    const float CetroRadius = 3f;
    const float ShockRadius = 5f;
    const float ChargeSpeed = 13f;

    protected override float IdleGap => 1.4f;

    protected override Vector2 IdleMove(float dt, Vector2 toPlayer) => Self.SteerTo(PlayerPos, Speed);

    protected override void ChooseAttack()
    {
        float r = GD.Randf();
        if (r < 0.45f) Cetro();
        else if (r < 0.85f) Avanco(Fury ? 2 : 1);
        else Decreto();
    }

    void Cetro()
    {
        Then(1f, () =>
        {
            Vector2 center = GD.Randf() < 0.6f ? Self.GlobalPosition : PlayerPos;
            bool fury = Fury;
            Telegraph.Circle(Self, center, CetroRadius * Units.Px, 1f, () =>
            {
                HurtPlayerInCircle(center, CetroRadius * Units.Px, Hit(2f));
                AudioManager.Instance?.PlayEnemyDeath();
                if (fury)
                    Telegraph.Circle(Self, center, ShockRadius * Units.Px, 0.5f, () => HurtPlayerInCircle(center, ShockRadius * Units.Px, Hit(1.2f)), () => Self.IsDead);
            }, () => Self.IsDead);
        });
        Hold(Fury ? 0.8f : 0.5f);
    }

    /// <summary>Alinha no eixo horizontal ou vertical e investe até bater; fica 1s vulnerável parado.</summary>
    void Avanco(int times)
    {
        Then(0f, () =>
        {
            Vector2 d = PlayerPos - Self.GlobalPosition;
            Vector2 dir = Mathf.Abs(d.X) > Mathf.Abs(d.Y) ? new Vector2(Mathf.Sign(d.X), 0f) : new Vector2(0f, Mathf.Sign(d.Y));
            ChargeAttack(dir, 0.7f, ChargeSpeed, 20f, 2f, 1f);
            if (times > 1) Then(0f, () => Avanco(times - 1));
        });
    }

    void Decreto()
    {
        Then(0.8f, () =>
        {
            Summon(EnemyKind.CartaPaus, 1, 6);
            Summon(EnemyKind.CartaOuros, 1, 6);
        });
    }

    protected override void LockSequence()
    {
        Then(0.1f, () =>
        {
            Summon(EnemyKind.CartaPaus, 1, 8);
            Summon(EnemyKind.CartaOuros, 1, 8);
        });
        Cetro();
        Avanco(1);
        Cetro();
        Hold(1.5f);
    }
}

/// <summary>
/// O Tigrinho (Fase 15, chefe final). Pulo do Tigre (área no chão, pousa causando dano),
/// Avanço do Tigre (linha vermelha e investida) e Invocação de Lacaios.
/// Fúria: 2 pulos seguidos. Trava: 3 pulos e lacaios.
/// </summary>
public class TigrinhoBrain : BossBrain
{
    const float JumpRadius = 2.5f;
    const float JumpTime = 1f;
    const float ChargeSpeed = 15f;
    static readonly EnemyKind[] Minions = { EnemyKind.Ficha, EnemyKind.Champanhe, EnemyKind.CartaEspadas, EnemyKind.CartaOuros };

    protected override float IdleGap => 1.3f;

    protected override Vector2 IdleMove(float dt, Vector2 toPlayer) => Self.SteerTo(PlayerPos, Speed);

    protected override void ChooseAttack()
    {
        float r = GD.Randf();
        if (r < 0.45f) { Jump(); if (Fury) Jump(); }
        else if (r < 0.85f) Then(0f, () => ChargeAttack((PlayerPos - Self.GlobalPosition).Normalized(), 0.8f, ChargeSpeed, 16f, 2f, 0.6f));
        else Lacaios();
    }

    /// <summary>Marca a área sob o jogador, salta em arco por cima de tudo e pousa causando dano.</summary>
    void Jump()
    {
        Vector2 from = Vector2.Zero, target = Vector2.Zero;
        float t = 0f;
        Then(JumpTime, () =>
        {
            from = Self.GlobalPosition;
            target = PlayerPos;
            t = 0f;
            Self.SetSolid(false);
            ContactDamage = false;
            Telegraph.Circle(Self, target, JumpRadius * Units.Px, JumpTime, null, () => Self.IsDead);
        }, (dt, _) =>
        {
            t = Mathf.Min(1f, t + dt / JumpTime);
            Self.Teleport(from.Lerp(target, t));
            Self.Sprite.Offset = new Vector2(0f, -Mathf.Sin(Mathf.Pi * t) * 18f);
            return Vector2.Zero;
        });
        Then(0.6f, () =>
        {
            Self.Teleport(target);
            Self.Sprite.Offset = Vector2.Zero;
            Self.SetSolid(true);
            ContactDamage = true;
            HurtPlayerInCircle(target, JumpRadius * Units.Px, Hit(1.6f));
            AudioManager.Instance?.PlayEnemyDeath();
        });
    }

    void Lacaios()
    {
        Then(0.9f, () =>
        {
            int total = GD.RandRange(3, 5);
            for (int i = 0; i < total; i++) Summon(Minions[GD.RandRange(0, Minions.Length - 1)], 1, 10);
        });
    }

    public override void OnDeath()
    {
        Self.Sprite.Offset = Vector2.Zero;
        base.OnDeath();
    }

    protected override void LockSequence()
    {
        Lacaios();
        Jump();
        Jump();
        Jump();
        Hold(1f);
    }
}
