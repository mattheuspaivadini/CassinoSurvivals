using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Base dos 3 chefes (PDF, seção 8): Armadura de Boss (teto de DPS), Trava de Fase em 50% com
/// invulnerabilidade, Fúria abaixo de 40% e uma fila de passos para encadear avisos e ataques.
/// </summary>
public abstract class BossBrain : EnemyBrain
{
    protected delegate Vector2 StepTick(float dt, Vector2 toPlayer);

    struct Step
    {
        public float Time;
        public Action Start;
        public StepTick Tick;
    }

    readonly Queue<Step> steps = new Queue<Step>();
    readonly List<Enemy> summons = new List<Enemy>();
    Step current;
    float stepTimer, idleTimer = 1.5f, windowTimer, takenThisSecond, shieldAngle;
    bool running, lockDone, lockPending;

    public bool Invulnerable { get; private set; }
    public bool Fury { get; private set; }
    protected bool InLock { get; private set; }

    public override bool FacePlayer => true;
    public override bool NeedsRedraw => true;

    /// <summary>Segundos parado/andando entre ataques (encurta na Fúria).</summary>
    protected abstract float IdleGap { get; }
    protected abstract Vector2 IdleMove(float dt, Vector2 toPlayer);
    /// <summary>Enfileira os passos do próximo ataque.</summary>
    protected abstract void ChooseAttack();
    /// <summary>Enfileira a sequência da Trava de Fase (o boss fica invulnerável até ela acabar).</summary>
    protected abstract void LockSequence();
    protected virtual void OnFury() { }

    public override Vector2 Think(float dt, Vector2 toPlayer)
    {
        windowTimer += dt;
        if (windowTimer >= 1f) { windowTimer = 0f; takenThisSecond = 0f; }
        shieldAngle += dt * 2.5f;

        if (!Fury && Self.Hp <= Self.MaxHp * EnemyRules.FuryHpFraction)
        {
            Fury = true;
            OnFury();
        }
        if (lockPending) StartLock();

        if (running || steps.Count > 0) return RunSteps(dt, toPlayer);

        SnapVelocity = false;
        ContactDamage = true;
        ContactScale = 1f;
        idleTimer -= dt;
        if (idleTimer <= 0f)
        {
            idleTimer = Fury ? IdleGap * 0.6f : IdleGap;
            ChooseAttack();
            return RunSteps(dt, toPlayer);
        }
        return IdleMove(dt, toPlayer);
    }

    Vector2 RunSteps(float dt, Vector2 toPlayer)
    {
        if (!running)
        {
            if (steps.Count == 0) return Vector2.Zero;
            current = steps.Dequeue();
            stepTimer = current.Time;
            running = true;
            SnapVelocity = true;
            current.Start?.Invoke();
        }
        Vector2 v = current.Tick?.Invoke(dt, toPlayer) ?? Vector2.Zero;
        stepTimer -= dt;
        if (stepTimer <= 0f) running = false;
        return v;
    }

    /// <summary>Encerra o passo atual no próximo frame (investida bateu na parede).</summary>
    protected void EndStep() => stepTimer = 0f;

    protected void Then(float seconds, Action start = null, StepTick tick = null) =>
        steps.Enqueue(new Step { Time = Mathf.Max(0f, seconds), Start = start, Tick = tick });

    protected void Hold(float seconds) => Then(seconds);

    void StartLock()
    {
        lockPending = false;
        lockDone = true;
        steps.Clear();
        running = false;
        Invulnerable = true;
        InLock = true;
        LockSequence();
        Then(0f, () => { Invulnerable = false; InLock = false; idleTimer = IdleGap; });
    }

    public override float FilterDamage(float amount)
    {
        if (Invulnerable) return 0f;
        float effective = EnemyRules.ApplyDpsCap(takenThisSecond, amount, Data.BossDpsCap);
        takenThisSecond += amount;
        if (!lockDone)
        {
            effective = EnemyRules.ClampToLock(Self.Hp, Self.MaxHp, EnemyRules.LockHpFraction, effective);
            if (Self.Hp - effective <= Self.MaxHp * EnemyRules.LockHpFraction + 0.01f)
            {
                lockPending = true;
                Invulnerable = true; // a barra trava no marco já neste golpe
            }
        }
        return effective;
    }

    public override void OnDeath()
    {
        // Os reforços somem com o chefe para a sala fechar sem caça aos restos.
        var copy = summons.ToArray();
        summons.Clear();
        foreach (var e in copy)
            if (e != null && GodotObject.IsInstanceValid(e) && !e.IsDead) e.Kill();
    }

    public override void Draw(Enemy canvas)
    {
        if (!Invulnerable) return;
        // Escudo giratório de cartas: indica que tiros não causam dano.
        float r = Self.Radius * 1.25f;
        for (int i = 0; i < 8; i++)
        {
            Vector2 c = Vector2.FromAngle(shieldAngle + Mathf.Tau * i / 8) * r;
            canvas.DrawRect(new Rect2(c - new Vector2(7f, 10f), new Vector2(14f, 20f)), new Color(1f, 0.85f, 0.3f, 0.85f));
        }
        canvas.DrawArc(Vector2.Zero, r, 0f, Mathf.Tau, 48, new Color(1f, 0.85f, 0.3f, 0.5f), 3f);
    }

    // ---------- Utilitários dos ataques ----------

    protected Vector2 PlayerPos => Player != null ? Player.GlobalPosition : Self.GlobalPosition;
    protected float Speed => Data.MoveSpeed * (Fury ? 1.3f : 1f);
    protected float Hit(float scale) => Data.ContactDamage * Self.DamageMultiplier * scale;

    protected Vector2 ArenaCenter
    {
        get
        {
            var a = Arena.Instance;
            return a != null ? a.Bounds.GetCenter() : Self.GlobalPosition;
        }
    }

    protected void HurtPlayerInCircle(Vector2 center, float radiusPx, float damage)
    {
        var p = Player;
        if (p != null && EnemyRules.InCircle(p.GlobalPosition, center, radiusPx + p.RadiusPx)) p.TakeDamage(damage);
    }

    protected void FireFan(Vector2 aim, int count, float spread, float speed, Color color)
    {
        foreach (var d in EnemyRules.FanDirections(aim, count, spread))
            EnemyProjectile.Fire(Self, Self.GlobalPosition, d, speed, Data.ProjectileDamage * Self.DamageMultiplier, color);
    }

    /// <summary>Investida em linha: aviso e depois corrida até a parede; acerta o jogador 1x na passagem.</summary>
    protected void ChargeAttack(Vector2 dir, float warnSeconds, float speedUnits, float maxUnits, float damageScale, float stunSeconds)
    {
        Vector2 from = Self.GlobalPosition;
        float len = FreeDistance(from, dir, maxUnits * Units.Px);
        bool hitDone = false;
        Then(warnSeconds, () => Telegraph.Line(Self, from, from + dir * len, Self.Radius, warnSeconds, null, () => Self.IsDead));
        Then(len / (speedUnits * Units.Px) + 0.15f, () => { ContactDamage = false; }, (dt, toPlayer) =>
        {
            var p = Player;
            if (!hitDone && p != null && toPlayer.Length() <= Self.Radius + p.RadiusPx)
            {
                hitDone = true;
                p.TakeDamage(Hit(damageScale));
            }
            if (FreeDistance(Self.GlobalPosition, dir, 0.4f * Units.Px) < 1f) EndStep();
            return dir * speedUnits * Units.Px;
        });
        Then(stunSeconds, () => { ContactDamage = true; AudioManager.Instance?.PlayEnemyHit(); });
    }

    /// <summary>Reforços pelas portas (sem $ e sem drop), respeitando o teto simultâneo.</summary>
    protected void Summon(EnemyKind kind, int count, int maxAlive)
    {
        summons.RemoveAll(e => e == null || !GodotObject.IsInstanceValid(e) || e.IsDead);
        int n = EnemyRules.SummonRoom(summons.Count, count, maxAlive);
        if (n <= 0) return;

        var director = HordeDirector.Instance;
        if (director != null && director.SummonAtDoors(kind, n, e => summons.Add(e))) return;

        var data = EnemyCatalog.Load(kind);
        if (data == null || EnemySpawner.Instance == null) return;
        var summon = EnemyData.CreateSummon(data);
        for (int i = 0; i < n; i++)
        {
            Vector2 pos = Self.GlobalPosition + Vector2.FromAngle(Mathf.Tau * i / n) * (Self.Radius + 0.8f * Units.Px);
            if (Arena.Instance != null && Arena.Instance.IsSolid(pos)) pos = Self.GlobalPosition;
            summons.Add(EnemySpawner.Instance.Spawn(summon, pos, 1f, isExtra: true, damageMultiplier: Self.DamageMultiplier));
        }
    }
}
