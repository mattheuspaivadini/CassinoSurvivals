using Godot;

/// <summary>Movimento top-down + HP do jogador.</summary>
public partial class PlayerController : RigidBody2D
{
	const int FrameSize = 32;
	const float DisplayScale = 3f;

	public static PlayerController Instance { get; private set; }

	[Export] public float MaxHp { get; set; } = 100f;
	[Export] public float MoveSpeed { get; set; } = 6f;
	[Export] public float RollSpeed { get; set; } = 15f;
	[Export] public float RollDuration { get; set; } = 0.25f;
	[Export] public float RollCooldown { get; set; } = 0.6f;
	/// <summary>Invulnerabilidade pós-dano (PDF, seção 3: 0,5s). Evita múltiplos golpes no dash/poças.</summary>
	[Export] public float InvulnerabilityDuration { get; set; } = 0.5f;
	/// <summary>Raio (unidades) usado nas checagens de contato/coleta por distância.</summary>
	[Export] public float Radius { get; set; } = 0.4f;

	[ExportGroup("Sprites")]
	[Export] public Texture2D IdleSheet { get; set; }
	[Export] public Texture2D WalkSheet { get; set; }
	[Export] public Texture2D RollSheet { get; set; }

	/// <summary>(hpAtual, hpMax)</summary>
	[Signal] public delegate void HpChangedEventHandler(float hp, float max);

	public float CurrentHp { get; private set; }
	float baseMaxHp;
	public float RadiusPx => Radius * Units.Px;
	public bool IsInvulnerable => invulnTimer > 0f;
	public bool IsRolling => rollTimer > 0f;
	public bool IsBluffImmune => bluffImmuneThisRoll && rollTimer > 0f;

	const uint PlayerLayerBit = 2u;
	const uint EnemyLayerBit = 4u;

	AnimatedSprite2D sprite;
	Color baseColor;
	string currentAnim;
	float flashTimer;
	float invulnTimer;
	float rollTimer;
	float rollCooldownTimer;
	float bluffAuraPhase;
	uint defaultCollisionLayer;
	uint defaultCollisionMask;
	bool ghosting;
	bool bluffImmuneThisRoll;
	bool drewBluffAura;
	Vector2 rollDirection;

	static readonly string[] Directions = { "down", "up", "left", "right" };

	public override void _Ready()
	{
		Instance = this;
		defaultCollisionLayer = CollisionLayer;
		defaultCollisionMask = CollisionMask;
		sprite = GetNode<AnimatedSprite2D>("Sprite");
		sprite.Modulate = Colors.White;
		baseColor = Colors.White;
		sprite.Scale = Vector2.One * DisplayScale;
		// O editor pode remover os exports da cena ao re-salva-la enquanto o script
		// estiver temporariamente invalido; recupera pelos caminhos conhecidos.
		IdleSheet ??= GD.Load<Texture2D>("res://assets/Dicehead and Casino Asset Pack/Player/Player_Idle.png");
		WalkSheet ??= GD.Load<Texture2D>("res://assets/Dicehead and Casino Asset Pack/Player/Player_Movement.png");
		RollSheet ??= GD.Load<Texture2D>("res://assets/Dicehead and Casino Asset Pack/Player/Player_Dodge_Roll.png");
		BuildFrames();
		if (!sprite.SpriteFrames.HasAnimation("idle") || sprite.SpriteFrames.GetFrameCount("idle") <= 0)
			GD.PushWarning("[PlayerController] Nenhum frame de idle: confira IdleSheet/WalkSheet/RollSheet no Player.tscn.");
		PlayAnim("idle");
		baseMaxHp = MaxHp;
		MaxHp = ComputeMaxHp(); // Vigor (Loja Externa) + HP Máximo do Caça-níquel de Melhorias
		CurrentHp = RunState.SavedHp.HasValue ? Mathf.Clamp(RunState.SavedHp.Value, 1f, MaxHp) : MaxHp;
		RunState.Changed += RefreshMaxHp;
	}

	public override void _ExitTree() => RunState.Changed -= RefreshMaxHp;

	float ComputeMaxHp() => baseMaxHp * (MetaProgress.HpMultiplier + RunState.MaxHpBonus);

	/// <summary>Recalcula o HP máximo após uma melhoria, mantendo a fração de vida atual.</summary>
	void RefreshMaxHp()
	{
		float max = ComputeMaxHp();
		if (Mathf.IsEqualApprox(max, MaxHp)) return;
		float fraction = MaxHp > 0f ? CurrentHp / MaxHp : 1f;
		MaxHp = max;
		CurrentHp = Mathf.Min(MaxHp, MaxHp * fraction);
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
	}

	void BuildFrames()
	{
		var frames = new SpriteFrames();
		AddFrames(frames, "idle", IdleSheet, 0, 5, 6f, true);
		for (int i = 0; i < Directions.Length; i++)
		{
			AddFrames(frames, "walk_" + Directions[i], WalkSheet, i, 8, 10f, true);
			AddFrames(frames, "roll_" + Directions[i], RollSheet, i, 6, 24f, false);
		}
		sprite.SpriteFrames = frames;
	}

	static void AddFrames(SpriteFrames frames, string name, Texture2D texture, int row, int count, float fps, bool loop)
	{
		if (texture == null)
		{
			GD.PushWarning($"[PlayerController] Textura da animação '{name}' não atribuída; frames não criados.");
			return;
		}
		StringName anim = name;
		frames.AddAnimation(anim);
		frames.SetAnimationSpeed(anim, fps);
		frames.SetAnimationLoopMode(anim, loop ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None);
		for (int i = 0; i < count; i++)
		{
			var frame = new AtlasTexture();
			frame.Atlas = texture;
			frame.Region = new Rect2(i * FrameSize, row * FrameSize, FrameSize, FrameSize);
			frames.AddFrame(anim, frame);
		}
	}

	void PlayAnim(string name)
	{
		if (currentAnim == name || sprite.SpriteFrames == null || !sprite.SpriteFrames.HasAnimation(name)) return;
		currentAnim = name;
		sprite.Play(name);
	}

	static string DirName(Vector2 v)
	{
		if (Mathf.Abs(v.X) > Mathf.Abs(v.Y)) return v.X > 0f ? "right" : "left";
		return v.Y > 0f ? "down" : "up";
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");

		if (rollCooldownTimer > 0f) rollCooldownTimer -= dt;

		if (rollTimer > 0f)
		{
			rollTimer -= dt;
			if (rollTimer <= 0f)
			{
				rollTimer = 0f;
				bluffImmuneThisRoll = false;
				SetGhosting(false);
			}
			else
			{
				LinearVelocity = rollDirection * RollSpeed * Units.Px;
				PlayAnim("roll_" + DirName(rollDirection));
				return;
			}
		}

		bool canRoll = GameManager.Instance == null || GameManager.Instance.State == GameState.Playing;
		if (canRoll && rollCooldownTimer <= 0f && Input.IsActionJustPressed("roll"))
		{
			rollDirection = input.LengthSquared() > 0.01f ? input.Normalized() : AimDirection();
			rollTimer = RollDuration;
			rollCooldownTimer = RollCooldown * RunState.DashCooldownMultiplier;
			bluffImmuneThisRoll = RunState.TryConsumePerfectBluff();
			if (bluffImmuneThisRoll)
			{
				Flash(new Color(0.35f, 0.95f, 1f));
				GameFeel.WorldBurst(this, GlobalPosition, new Color(0.35f, 0.95f, 1f), 14, 220f);
			}
			SetGhosting(true);
			LinearVelocity = rollDirection * RollSpeed * Units.Px;
			PlayAnim("roll_" + DirName(rollDirection));
			AudioManager.Instance?.PlayDodgeRoll();
			return;
		}

		float speedMult = Mathf.Min(RunState.MoveSpeedMultiplier, RunState.MaxMoveSpeedMultiplier);
		LinearVelocity = input * MoveSpeed * speedMult * Units.Px;
		if (input.LengthSquared() > 0.01f)
		{
			sprite.FlipH = false;
			PlayAnim("walk_" + DirName(input));
		}
		else
		{
			PlayAnim("idle");
			sprite.FlipH = GetGlobalMousePosition().X < GlobalPosition.X;
		}
	}

	/// <summary>Ghosting no Dash (PDF, seção 3): atravessa inimigos sem parar por colisão, mantendo paredes.</summary>
	void SetGhosting(bool on)
	{
		if (ghosting == on) return;
		ghosting = on;
		CollisionLayer = on ? (defaultCollisionLayer & ~PlayerLayerBit) : defaultCollisionLayer;
		CollisionMask = on ? (defaultCollisionMask & ~EnemyLayerBit) : defaultCollisionMask;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		if (invulnTimer > 0f) invulnTimer = Mathf.Max(0f, invulnTimer - dt);
		if (flashTimer > 0f) flashTimer = Mathf.Max(0f, flashTimer - dt);

		bool showBluffAura = IsBluffImmune || RunState.IsPerfectBluffReady;
		if (showBluffAura)
		{
			bluffAuraPhase += dt;
			drewBluffAura = true;
			QueueRedraw();
		}
		else if (drewBluffAura)
		{
			drewBluffAura = false;
			QueueRedraw();
		}

		if (flashTimer > 0f) return;
		if (invulnTimer > 0f)
		{
			bool blink = ((int)(invulnTimer * 20f) & 1) == 0;
			sprite.Modulate = new Color(baseColor.R, baseColor.G, baseColor.B, blink ? 0.45f : 0.95f);
		}
		else if (sprite.Modulate != baseColor)
		{
			sprite.Modulate = baseColor;
		}
	}

	public override void _Draw()
	{
		if (IsBluffImmune)
		{
			float r = RadiusPx * 1.45f;
			DrawCircle(Vector2.Zero, r, new Color(0.30f, 0.95f, 1f, 0.22f));
			DrawArc(Vector2.Zero, r, 0f, Mathf.Tau, 32, new Color(1f, 0.92f, 0.35f, 0.95f), 3.2f);
		}
		else if (RunState.IsPerfectBluffReady)
		{
			// Indicador visual do Blefe Perfeito pronto (passiva #8).
			float pulse = 1.24f + 0.08f * Mathf.Sin(bluffAuraPhase * 5f);
			float r = RadiusPx * pulse;
			DrawArc(Vector2.Zero, r, 0f, Mathf.Tau, 28, new Color(0.35f, 0.96f, 1f, 0.78f), 2.4f);
		}
	}

	/// <summary>Direção usada no rolamento quando o jogador está parado.</summary>
	Vector2 AimDirection()
	{
		Vector2 toMouse = GetGlobalMousePosition() - GlobalPosition;
		return toMouse.LengthSquared() < 1f ? Vector2.Right : toMouse.Normalized();
	}

	public void TakeDamage(float amount, bool isContact = false)
	{
		if (CurrentHp <= 0f || invulnTimer > 0f || GameManager.Instance.State != GameState.Playing) return;
		// Blefe Perfeito (passiva #8): dash imune atravessa inimigos sem sofrer dano de contato.
		if (isContact && IsBluffImmune) return;

		invulnTimer = InvulnerabilityDuration;
		CurrentHp = Mathf.Max(0f, CurrentHp - amount);
		Flash(Colors.Red);
		AudioManager.Instance?.PlayPlayerHurt();
		GameFeel.Shake(Mathf.Clamp(amount * 0.4f, 4f, 14f), 0.18f);

		// Virar a Mesa (passiva #7): ao sofrer dano (inclusive no dash), empurra inimigos colados.
		if (RunState.Has(Passive.VirarAMesa)) TriggerFlipTablePulse();

		if (CurrentHp <= 0f && TryRevive()) return;
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
		if (CurrentHp <= 0f) GameManager.Instance.GameOver();
	}

	void TriggerFlipTablePulse()
	{
		float radiusPx = PassiveRules.FlipTableRadiusUnits * Units.Px;
		GameFeel.PulseRing(this, GlobalPosition, radiusPx, GameFeel.Neon, 0.28f);
		GameFeel.WorldBurst(this, GlobalPosition, GameFeel.Neon, 24, 340f);

		var spawner = EnemySpawner.Instance;
		if (spawner == null) return;
		foreach (var enemy in spawner.Active)
		{
			if (enemy.IsDead || !IsInstanceValid(enemy)) continue;
			Vector2 delta = enemy.GlobalPosition - GlobalPosition;
			float reach = radiusPx + enemy.Radius;
			if (delta.LengthSquared() > reach * reach) continue;
			Vector2 dir = delta.LengthSquared() > 1f ? delta.Normalized() : Vector2.Right;
			enemy.ApplyKnockback(dir, PassiveRules.FlipTableKnockbackSpeedPx, ignoreMass: true);
		}
	}

	bool reviveUsed;

	/// <summary>Imortalidade (Loja Externa): 1x por partida, chance de voltar com parte do HP.</summary>
	bool TryRevive()
	{
		if (reviveUsed || GD.Randf() >= MetaProgress.ReviveChance) return false;
		reviveUsed = true;
		CurrentHp = MaxHp * MetaProgress.ReviveHpFraction;
		Flash(new Color(1f, 0.85f, 0.15f));
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
		return true;
	}

	public void Heal(float amount)
	{
		if (CurrentHp <= 0f) return;
		CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
		Flash(Colors.Green);
		AudioManager.Instance?.PlayHeal();
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
	}

	void Flash(Color c)
	{
		sprite.Modulate = c;
		flashTimer = 0.1f;
	}
}
