using System.Linq;
using Godot;

/// <summary>
/// Controlador da cena Main. Com DebugWeaponSandbox ativo (ou CLI --sandbox), desliga o
/// spawner de inimigos e entrega as 5 armas ao WeaponSystem para teste visual.
/// Para jogar normalmente: desmarque a flag no inspetor ou use a CLI --nosandbox.
/// </summary>
public partial class MainController : Node2D
{
    [Export] public bool DebugWeaponSandbox { get; set; } = true;
    [Export] public EnemySpawner EnemySpawner { get; set; }
    [Export] public WeaponSystem WeaponSystem { get; set; }

    public bool SandboxActive { get; private set; }

    public override void _Ready()
    {
        string[] args = OS.GetCmdlineUserArgs();
        SandboxActive = DebugWeaponSandbox || args.Contains("--sandbox");
        if (args.Contains("--nosandbox")) SandboxActive = false;

        // SetProcess(false) para _Process inteiro: sem ondas, sem boss e sem timer pendente.
        // Instance continua válido para o HUD e para o WeaponSystem.
        if (EnemySpawner != null) EnemySpawner.SetProcess(!SandboxActive);
        if (WeaponSystem != null) WeaponSystem.SandboxMode = SandboxActive;
    }
}
