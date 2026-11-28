/// <summary>
/// Modo de mira escolhido pelo jogador. Estático para sobreviver ao reload da cena (reiniciar
/// mantém a escolha). Auto: mira e atira sozinho no inimigo visível mais próximo. Manual: mira no
/// mouse e atira com o botão esquerdo.
/// </summary>
public static class AimSettings
{
    public static bool Auto { get; set; } = true;
}
