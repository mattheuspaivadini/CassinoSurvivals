using System.Linq;
using Godot;

/// <summary>
/// Controlador da cena Main. O modo sandbox fica desativado por padrão na build final e só
/// liga explicitamente com --sandbox, --enemy-sandbox ou marcando DebugWeaponSandbox no inspetor.
/// </summary>
public partial class MainController : Node2D
{
    [Export] public bool DebugWeaponSandbox { get; set; } = false;
    [Export] public EnemySpawner EnemySpawner { get; set; }
    [Export] public WeaponSystem WeaponSystem { get; set; }

    public bool SandboxActive { get; private set; }

    public override void _Ready()
    {
        string[] args = OS.GetCmdlineUserArgs();
        bool enemySandbox = GameManager.HasEnemySandboxFlag(args) || (GameManager.Instance?.EnemySandboxActive ?? false);
        SandboxActive = DebugWeaponSandbox || args.Contains("--sandbox") || enemySandbox;
        if (args.Contains("--nosandbox")) SandboxActive = false;

        // SetProcess(false) para _Process inteiro: sem ondas, sem boss e sem timer pendente.
        // Instance continua válido para o HUD e para o WeaponSystem.
        if (EnemySpawner != null) EnemySpawner.SetProcess(!SandboxActive);
        if (WeaponSystem != null) WeaponSystem.SandboxMode = SandboxActive;
        if (enemySandbox && GetNodeOrNull("EnemySandboxUI") == null)
        {
            var ui = new EnemySandboxUI { Name = "EnemySandboxUI" };
            AddChild(ui);
        }
    }
}
