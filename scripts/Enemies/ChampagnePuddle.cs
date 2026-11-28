using Godot;

/// <summary>
/// Poça deixada pela explosão da Garrafa de Champanhe (PDF, seção 9 e Balanceamento Numérico):
/// dura 4s (6s na versão Dourada) e aplica dano contínuo em ticks de 0,5s ao pisar.
/// </summary>
public partial class ChampagnePuddle : Node2D
{
    const float TickInterval = 0.5f;
    const float BurstDuration = 0.22f;
    const float FadeOutWindow = 0.8f;

    float radiusPx;
    float damagePerTick;
    float duration;
    float elapsed;
    float tickTimer;
    float blastRadiusPx;
    bool golden;

    public void Setup(Vector2 worldPos, float puddleRadiusPx, float explosionRadiusPx, float dps, float totalDuration, bool isGolden)
    {
        GlobalPosition = worldPos;
        radiusPx = puddleRadiusPx;
        blastRadiusPx = Mathf.Max(puddleRadiusPx, explosionRadiusPx);
        damagePerTick = dps * TickInterval;
        duration = Mathf.Max(0.5f, totalDuration);
        golden = isGolden;
        ZIndex = -2; // acima do carpete (-10/-9) e paredes (-5), abaixo de drops (0), inimigos (1) e jogador (3)
    }

    public override void _Process(double delta)
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;
        float dt = (float)delta;

        elapsed += dt;
        if (elapsed >= duration)
        {
            QueueFree();
            return;
        }

        if (tickTimer > 0f) tickTimer -= dt;

        var player = PlayerController.Instance;
        if (player != null && tickTimer <= 0f)
        {
            float reach = radiusPx + player.RadiusPx * 0.45f;
            if ((player.GlobalPosition - GlobalPosition).LengthSquared() <= reach * reach)
            {
                player.TakeDamage(damagePerTick);
                tickTimer = TickInterval;
            }
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        float fade = Mathf.Clamp((duration - elapsed) / FadeOutWindow, 0f, 1f);
        Color baseColor = golden ? new Color(1f, 0.82f, 0.16f) : new Color(0.42f, 0.92f, 0.30f);
        Color highlight = golden ? new Color(1f, 0.96f, 0.55f) : new Color(0.75f, 1f, 0.62f);

        if (elapsed < BurstDuration)
        {
            float t = elapsed / BurstDuration;
            float ringR = Mathf.Lerp(radiusPx * 0.35f, blastRadiusPx, t);
            DrawArc(Vector2.Zero, ringR, 0f, Mathf.Tau, 36, new Color(highlight, (1f - t) * 0.9f), 5f);
        }

        DrawCircle(Vector2.Zero, radiusPx, new Color(baseColor, 0.40f * fade));
        DrawCircle(new Vector2(-radiusPx * 0.12f, -radiusPx * 0.10f), radiusPx * 0.68f, new Color(highlight, 0.24f * fade));
        DrawArc(Vector2.Zero, radiusPx, 0f, Mathf.Tau, 36, new Color(highlight, 0.72f * fade), 2.5f);

        // Bolhas animadas de champanhe na poça.
        for (int i = 0; i < 5; i++)
        {
            float phase = (elapsed * 1.6f + i * 0.37f) % 1f;
            float angle = i * 1.2566f + 0.4f;
            float dist = radiusPx * (0.22f + 0.48f * ((i * 37 % 10) / 10f));
            Vector2 bPos = Vector2.FromAngle(angle) * dist + new Vector2(0f, -phase * 8f);
            float bRadius = Mathf.Lerp(2.5f, 6f, phase);
            DrawArc(bPos, bRadius, 0f, Mathf.Tau, 12, new Color(highlight, (1f - phase) * 0.75f * fade), 1.5f);
        }
    }
}
