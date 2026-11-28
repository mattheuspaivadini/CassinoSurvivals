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

	AnimatedSprite2D sprite;
	Color baseColor;
	string currentAnim;
	float flashTimer;
	float rollTimer;
	float rollCooldownTimer;
	Vector2 rollDirection;

	static readonly string[] Directions = { "down", "up", "left", "right" };

	public override void _Ready()
	{
		Instance = this;
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
		CurrentHp = MaxHp;
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
			LinearVelocity = rollDirection * RollSpeed * Units.Px;
			PlayAnim("roll_" + DirName(rollDirection));
			return;
		}

		bool canRoll = GameManager.Instance == null || GameManager.Instance.State == GameState.Playing;
		if (canRoll && rollCooldownTimer <= 0f && Input.IsActionJustPressed("roll"))
		{
			rollDirection = input.LengthSquared() > 0.01f ? input.Normalized() : AimDirection();
			rollTimer = RollDuration;
			rollCooldownTimer = RollCooldown * RunState.DashCooldownMultiplier;
			LinearVelocity = rollDirection * RollSpeed * Units.Px;
			PlayAnim("roll_" + DirName(rollDirection));
			return;
		}

		LinearVelocity = input * MoveSpeed * RunState.MoveSpeedMultiplier * Units.Px;
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

	public override void _Process(double delta)
	{
		if (flashTimer <= 0f) return;
		flashTimer -= (float)delta;
		if (flashTimer <= 0f) sprite.Modulate = baseColor;
	}

	/// <summary>Direção usada no rolamento quando o jogador está parado.</summary>
	Vector2 AimDirection()
	{
		Vector2 toMouse = GetGlobalMousePosition() - GlobalPosition;
		return toMouse.LengthSquared() < 1f ? Vector2.Right : toMouse.Normalized();
	}

	public void TakeDamage(float amount)
	{
		if (CurrentHp <= 0f || GameManager.Instance.State != GameState.Playing) return;
		CurrentHp = Mathf.Max(0f, CurrentHp - amount);
		Flash(Colors.Red);
		AudioManager.Instance?.PlayPlayerHurt();
		if (CurrentHp <= 0f && TryRevive()) return;
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
		if (CurrentHp <= 0f) GameManager.Instance.GameOver();
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
