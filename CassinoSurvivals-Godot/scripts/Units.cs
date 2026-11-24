using Godot;

/// <summary>
/// O jogo foi desenhado em unidades de mundo da Unity (1 unidade = 1 sprite, câmera ortográfica 9 = 18 unidades de altura).
/// No Godot 1 unidade = Px pixels: mantém o enquadramento em 1080p e a física (calibrada para pixels) estável.
/// </summary>
public static class Units
{
    public const float Px = 60f;

    /// <summary>Faz o sprite (textura quadrada) ocupar <paramref name="unitSize"/> unidades de lado.</summary>
    public static void Fit(Sprite2D sprite, float unitSize)
    {
        if (sprite.Texture != null) sprite.Scale = Vector2.One * (Px * unitSize / sprite.Texture.GetWidth());
    }
}
