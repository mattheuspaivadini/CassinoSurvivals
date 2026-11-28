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
            Summon(EnemyKind.Ficha, 6, 14);
            Summon(EnemyKind.CartaEspadas, 2, 14);
            Summon(EnemyKind.CartaOuros, 1, 14);
        });
    }

    protected override void LockSequence()
    {
        Then(0.1f, () =>
        {
            Summon(EnemyKind.CartaEspadas, 3, 16);
            Summon(EnemyKind.Ficha, 4, 16);
        });
        Spiral(7f, 0.08f);
    }
}

/// <summary>
/// O Rei de Paus (Fase 10): pesado, avança firme. Impacto do Cetro, Avanço Soberano e Decreto.
/// Fúria: onda de choque extra e investida dupla. Trava: Cetro, Avanço, Cetro + Paus e Ouros.
/// </summary>
public class ReiBrain : BossBrain
{
    const float CetroRadius = 4.5f;
    const float ShockRadius = 7.2f;
    const float ChargeSpeed = 13f;

    protected override float IdleGap => 1.4f;

    protected override Vector2 IdleMove(float dt, Vector2 toPlayer) => Self.SteerTo(PlayerPos, Speed);

    protected override void ChooseAttack()
    {
        float r = GD.Randf();
        if (r < 0.60f) Cetro();
        else if (r < 0.82f) Avanco(Fury ? 2 : 1);
        else Decreto();
    }

    void Cetro()
    {
        if (GD.Randf() < 0.35f)
        {
            Then(1f, () =>
            {
                Self.PlaySpecialAnim("cajado", 1.0f);
                Vector2 center = Self.GlobalPosition;
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
            return;
        }

        const float LeapTime = 0.85f;
        Vector2 from = Vector2.Zero, target = Vector2.Zero;
        float t = 0f;
        Then(LeapTime, () =>
        {
            from = Self.GlobalPosition;
            target = PlayerPos;
            t = 0f;
            Self.SetSolid(false);
            ContactDamage = false;
            Self.PlaySpecialAnim("cajado", LeapTime + 0.35f);
            Telegraph.Circle(Self, target, CetroRadius * Units.Px, LeapTime, null, () => Self.IsDead);
        }, (dt, _) =>
        {
            t = Mathf.Min(1f, t + dt / LeapTime);
            Self.Teleport(from.Lerp(target, t));
            Self.Sprite.Offset = new Vector2(0f, -Mathf.Sin(Mathf.Pi * t) * 22f);
            return Vector2.Zero;
        });
        Then(Fury ? 0.8f : 0.5f, () =>
        {
            Self.Teleport(target);
            Self.Sprite.Offset = Vector2.Zero;
            Self.SetSolid(true);
            ContactDamage = true;
            HurtPlayerInCircle(target, CetroRadius * Units.Px, Hit(2f));
            AudioManager.Instance?.PlayEnemyDeath();
            if (Fury)
                Telegraph.Circle(Self, target, ShockRadius * Units.Px, 0.5f, () => HurtPlayerInCircle(target, ShockRadius * Units.Px, Hit(1.2f)), () => Self.IsDead);
        });
    }

    public override void OnDeath()
    {
        Self.Sprite.Offset = Vector2.Zero;
        Self.SetSolid(true);
        base.OnDeath();
    }

    /// <summary>Alinha no eixo horizontal ou vertical e investe até bater; fica 1s vulnerável parado.</summary>
    void Avanco(int times)
    {
        Then(0f, () =>
        {
            Vector2 d = PlayerPos - Self.GlobalPosition;
            Vector2 dir = Mathf.Abs(d.X) > Mathf.Abs(d.Y) ? new Vector2(Mathf.Sign(d.X), 0f) : new Vector2(0f, Mathf.Sign(d.Y));
            ChargeAttack(dir, 0.7f, ChargeSpeed, 20f, 2f, 1f, chargeAnim: "ombrada", stunAnim: "colisao");
            if (times > 1) Then(0f, () => Avanco(times - 1));
        });
    }

    void Decreto()
    {
        Then(0.8f, () =>
        {
            Summon(EnemyKind.CartaPaus, 2, 14);
            Summon(EnemyKind.CartaOuros, 2, 14);
            Summon(EnemyKind.CartaEspadas, 2, 14);
        });
    }

    protected override void LockSequence()
    {
        Then(0.1f, () =>
        {
            Summon(EnemyKind.CartaPaus, 2, 16);
            Summon(EnemyKind.CartaOuros, 2, 16);
            Summon(EnemyKind.CartaEspadas, 2, 16);
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

/// <summary>
/// A Máquina de Caça-Níquel (Fase 15, Boss Final — PDF seção 8):
/// Giro da Casa (3 Espadas / 3 Bombas / 3 Fichas), Fúria JACKPOT 777 (&lt; 35% HP) e
/// Travas de Fase em 65% e 35% com grade metálica bloqueando o visor durante o Bônus da Casa.
/// </summary>
public class MaquinaCacaNiquelBrain : BossBrain
{
    static readonly float[] SlotLocks = { 0.65f, 0.35f };
    static readonly Color SwordColor = new Color(1f, 0.35f, 0.55f);
    static readonly Color JackpotColor = new Color(1f, 0.85f, 0.15f);
    const float ShotSpeed = 9.2f;
    const float BombRadiusUnits = 2.1f;
    const float PuddleRadiusUnits = 2.0f;
    const float PuddleDps = 12f;
    const float PuddleDuration = 8f;
    float strafe = 1f;

    public override bool AllowFlipH => false;
    protected override float FuryThreshold => 0.35f;
    protected override float[] LockThresholds => SlotLocks;
    protected override float IdleGap => 0.75f;

    protected override Vector2 IdleMove(float dt, Vector2 toPlayer) =>
        KeepDistance(toPlayer, 4.5f, 8.5f, Speed, ref strafe);

    protected override void ChooseAttack()
    {
        strafe = -strafe;
        float spinTime = Fury ? 0.75f : 1.1f;
        Then(spinTime, () => Self.PlaySpecialAnim("spin", spinTime));

        if (Fury)
        {
            Then(1.1f, () =>
            {
                Self.PlaySpecialAnim("trans_777", 1.1f);
                Summon(EnemyKind.CartaEspadas, 1, 18);
                Summon(EnemyKind.CartaOuros, 1, 18);
            });
            ChuvaExplosiva(10, withVisualAnim: false);
            RajadaDaCasa(withVisualAnim: false, color: JackpotColor);
            Hold(0.25f);
            return;
        }

        float r = GD.Randf();
        if (r < 0.38f) RajadaDaCasa(withVisualAnim: true, color: SwordColor);
        else if (r < 0.76f) ChuvaExplosiva(GD.RandRange(8, 11), withVisualAnim: true);
        else TrocoDaCasa();
    }

    void RajadaDaCasa(bool withVisualAnim, Color color)
    {
        if (withVisualAnim)
        {
            Then(1.1f, () => Self.PlaySpecialAnim("trans_espadas", 1.1f));
            Then(0.1f, () => Self.PlaySpecialAnim("final_espadas", 1.8f));
        }
        float interval = Fury ? 0.20f : 0.28f;
        for (int wave = 0; wave < 5; wave++)
        {
            float angleOffset = wave * 15f;
            bool aimedFan = (wave & 1) == 0;
            FireEightWayWave(angleOffset, aimedFan, color);
            if (wave < 4) Hold(interval);
        }
        Hold(0.35f);
    }

    void FireEightWayWave(float offsetDegrees, bool aimedFan, Color color)
    {
        Then(0.04f, () =>
        {
            float baseRad = Mathf.DegToRad(offsetDegrees);
            for (int i = 0; i < 8; i++)
            {
                Vector2 dir = Vector2.FromAngle(baseRad + i * Mathf.Pi * 0.25f);
                EnemyProjectile.Fire(Self, Self.GlobalPosition, dir, ShotSpeed, Data.ProjectileDamage * Self.DamageMultiplier, color);
            }
            if (aimedFan)
                FireFan(PlayerPos - Self.GlobalPosition, 3, 28f, ShotSpeed * 1.05f, color);
        });
    }

    void ChuvaExplosiva(int count, bool withVisualAnim)
    {
        if (withVisualAnim)
        {
            Then(1.1f, () => Self.PlaySpecialAnim("trans_bombas", 1.1f));
            Then(0.1f, () => Self.PlaySpecialAnim("final_bombas", 1.5f));
        }
        Then(1.05f, () =>
        {
            float blastR = BombRadiusUnits * Units.Px;
            float puddleR = PuddleRadiusUnits * Units.Px;
            bool golden = Fury;
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = i == 0 ? PlayerPos : (i == 1 ? PickNearPlayerBombPos() : PickBombPos());
                Telegraph.Circle(Self, pos, blastR, 1.0f, () =>
                {
                    HurtPlayerInCircle(pos, blastR, Hit(1.4f));
                    AudioManager.Instance?.PlayEnemyDeath();
                    var puddle = new ChampagnePuddle();
                    Self.GetParent()?.AddChild(puddle);
                    puddle.Setup(pos, puddleR, blastR, PuddleDps * Self.DamageMultiplier, PuddleDuration, golden);
                }, () => Self.IsDead);
            }
        });
        Hold(0.45f);
    }

    Vector2 PickNearPlayerBombPos()
    {
        Vector2 candidate = PlayerPos + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 2.2f * Units.Px;
        var arena = Arena.Instance;
        if (arena == null) return candidate;
        Rect2 b = arena.Bounds.Grow(-2f * Units.Px);
        candidate = new Vector2(
            Mathf.Clamp(candidate.X, b.Position.X, b.End.X),
            Mathf.Clamp(candidate.Y, b.Position.Y, b.End.Y));
        return arena.IsSolid(candidate) ? PlayerPos : candidate;
    }

    Vector2 PickBombPos()
    {
        var arena = Arena.Instance;
        if (arena == null) return PlayerPos + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * GD.RandRange(2, 5) * Units.Px;
        Rect2 b = arena.Bounds.Grow(-2f * Units.Px);
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 p = new Vector2(
                (float)GD.RandRange(b.Position.X, b.End.X),
                (float)GD.RandRange(b.Position.Y, b.End.Y));
            if (!arena.IsSolid(p)) return p;
        }
        return PlayerPos;
    }

    void TrocoDaCasa()
    {
        Then(1.1f, () => Self.PlaySpecialAnim("trans_fichas", 1.1f));
        Then(1.0f, () =>
        {
            Self.PlaySpecialAnim("final_fichas", 1.0f);
            Summon(EnemyKind.CartaEspadas, 2, 18);
            Summon(EnemyKind.CartaOuros, 2, 18);
            Summon(EnemyKind.CartaPaus, 2, 18);
            Summon(EnemyKind.CartaCopas, 1, 18);
            Summon(EnemyKind.Champanhe, 2, 18);
        });
        Hold(0.35f);
    }

    protected override void LockSequence()
    {
        int lockIdx = LockIndex;
        Then(0.8f, () =>
        {
            Self.PlaySpecialAnim("spin", 0.8f);
            Summon(EnemyKind.CartaEspadas, 2, 20);
            Summon(EnemyKind.CartaOuros, 2, 20);
            Summon(EnemyKind.Champanhe, 2, 20);
        }, (dt, _) => Self.SteerTo(ArenaCenter, Speed * 1.4f));
        for (int cycle = 0; cycle < 2; cycle++)
        {
            ChuvaExplosiva(7, withVisualAnim: false);
            RajadaDaCasa(withVisualAnim: false, color: JackpotColor);
        }
        if (lockIdx == 1)
            Then(1.1f, () => Self.PlaySpecialAnim("trans_777", 1.1f));
        Hold(0.4f);
    }

    public override void Draw(Enemy canvas)
    {
        base.Draw(canvas);
        if (!Invulnerable) return;

        // Grade metálica fechada sobre o visor enquanto a trava de fase bloqueia tiros.
        float w = Self.Radius * 0.75f;
        float h = Self.Radius * 0.9f;
        var rect = new Rect2(-w * 0.5f, -h * 0.65f, w, h);
        canvas.DrawRect(rect, new Color(0.18f, 0.20f, 0.25f, 0.45f));
        canvas.DrawRect(rect, new Color(0.75f, 0.80f, 0.88f, 0.92f), filled: false, width: 3f);
        for (int i = 1; i <= 4; i++)
        {
            float x = rect.Position.X + rect.Size.X * (i / 5f);
            canvas.DrawLine(new Vector2(x, rect.Position.Y), new Vector2(x, rect.End.Y), new Color(0.82f, 0.86f, 0.92f, 0.95f), 3f);
        }
        for (int j = 1; j <= 2; j++)
        {
            float y = rect.Position.Y + rect.Size.Y * (j / 3f);
            canvas.DrawLine(new Vector2(rect.Position.X, y), new Vector2(rect.End.X, y), new Color(0.82f, 0.86f, 0.92f, 0.95f), 2.5f);
        }
    }
}
