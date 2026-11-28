using Godot;

/// <summary>Caminhos padrão dos .tres por tipo, usados quando a cena não configura o inimigo.</summary>
public static class EnemyCatalog
{
    public const string RainhaPath = "res://resources/enemies/Rainha.tres";
    public const string ReiPath = "res://resources/enemies/ReiDePaus.tres";
    public const string TigrinhoPath = "res://resources/enemies/Tigrinho.tres";

    public static string PathFor(EnemyKind kind) => kind switch
    {
        EnemyKind.Ficha => "res://resources/enemies/CrupieVeloz.tres",
        EnemyKind.Fichona => "res://resources/enemies/Capanga.tres",
        EnemyKind.Champanhe => "res://resources/enemies/Garrafa.tres",
        EnemyKind.CartaEspadas => "res://resources/enemies/CartaEspadas.tres",
        EnemyKind.CartaOuros => "res://resources/enemies/CartaOuros.tres",
        EnemyKind.CartaPaus => "res://resources/enemies/CartaPaus.tres",
        EnemyKind.CartaCopas => "res://resources/enemies/CartaCopas.tres",
        _ => null,
    };

    public static string BossPath(BossKind boss) => boss switch
    {
        BossKind.Rainha => RainhaPath,
        BossKind.Rei => ReiPath,
        _ => TigrinhoPath, // decisão do grupo: o chefe final (F15) é o Tigrinho
    };

    public static EnemyData Load(EnemyKind kind)
    {
        string path = PathFor(kind);
        return path != null && ResourceLoader.Exists(path) ? GD.Load<EnemyData>(path) : null;
    }

    public static EnemyData LoadBoss(BossKind boss)
    {
        string path = BossPath(boss);
        return ResourceLoader.Exists(path) ? GD.Load<EnemyData>(path) : null;
    }
}
