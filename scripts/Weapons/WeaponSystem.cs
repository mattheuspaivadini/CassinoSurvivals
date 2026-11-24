using System.Collections.Generic;
using Godot;

/// <summary>Armas do jogador. Só a selecionada (scroll do mouse) atira, com mira automática no inimigo mais próximo.</summary>
public partial class WeaponSystem : Node
{
    [Export] public Godot.Collections.Array<WeaponData> StartingWeapons { get; set; }
    [Export] public PackedScene ProjectileScene { get; set; }

    public readonly List<WeaponInstance> Weapons = new List<WeaponInstance>(8);

    [Signal] public delegate void WeaponsChangedEventHandler();

    public int Selected { get; private set; }

    public override void _Ready()
    {
        foreach (var w in StartingWeapons) AddOrUpgrade(w, 1);
    }

    public WeaponInstance Find(WeaponData data)
    {
        for (int i = 0; i < Weapons.Count; i++)
            if (Weapons[i].Data == data) return Weapons[i];
        return null;
    }

    /// <summary>Se já tem a arma: +levels. Se não tem: desbloqueia no nível = levels.</summary>
    public void AddOrUpgrade(WeaponData data, int levels)
    {
        var w = Find(data);
        if (w == null) Weapons.Add(new WeaponInstance(data, levels));
        else w.AddLevels(levels);

        EmitSignal(SignalName.WeaponsChanged);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (GameManager.Instance.State != GameState.Playing || Weapons.Count <= 1) return;

        // Scroll para cima = arma anterior, para baixo = próxima.
        int step = e.IsActionPressed("weapon_next") ? 1 : e.IsActionPressed("weapon_prev") ? -1 : 0;
        if (step == 0) return;
        Selected = (Selected + step + Weapons.Count) % Weapons.Count;
        EmitSignal(SignalName.WeaponsChanged);
    }

    public override void _Process(double delta)
    {
        if (GameManager.Instance.State != GameState.Playing || EnemySpawner.Instance == null) return;

        var w = Weapons[Selected];
        Vector2 pos = ((Node2D)GetParent()).GlobalPosition;
        Enemy target = EnemySpawner.Instance.FindNearest(pos, w.Data.Range * Units.Px, out _);
        if (w.Tick((float)delta, target != null)) Fire(w, pos, target.GlobalPosition);
    }

    void Fire(WeaponInstance w, Vector2 origin, Vector2 targetPos)
    {
        var d = w.Data;
        float baseAngle = (targetPos - origin).Angle();
        int n = Mathf.Max(1, d.ProjectilesPerShot);
        float damage = d.Damage(w.Level);

        for (int i = 0; i < n; i++)
        {
            float offset = n == 1 ? 0f : Mathf.DegToRad(d.SpreadAngle) * ((float)i / (n - 1) - 0.5f);
            var v = Vector2.FromAngle(baseAngle + offset) * d.ProjectileSpeed * Units.Px;
            var p = ProjectileScene.Instantiate<Projectile>();
            GetTree().CurrentScene.AddChild(p);
            p.Launch(origin, v, damage, d.ProjectileLifetime, d.ProjectileColor, d.ProjectileScale);
        }
        AudioManager.Instance?.PlayShoot();
    }
}
