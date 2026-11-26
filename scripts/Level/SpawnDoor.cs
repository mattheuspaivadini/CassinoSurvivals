using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Porta lateral de spawn (PDF, seção 10): pisca um alerta ~1s, abre, solta os inimigos da fila um a um
/// e fecha. A frente da porta fica sempre desobstruída no layout.
/// Visual (filhos): "PanelA"/"PanelB" são as folhas que encolhem para dentro da parede ao abrir,
/// "Lamp" é a lâmpada sobre o vão e "Alert" o brilho no chão durante o alerta.
/// </summary>
public partial class SpawnDoor : Node2D
{
    enum DoorState { Idle, Alert, Open }

    /// <summary>Direção para dentro da arena.</summary>
    [Export] public Vector2 Inward { get; set; } = Vector2.Right;
    [Export] public float AlertTime { get; set; } = 1f;
    [Export] public float ReleaseInterval { get; set; } = 0.35f;
    [Export] public float CloseDelay { get; set; } = 0.5f;
    /// <summary>Unidades a partir da porta: onde o inimigo nasce e até onde anda antes de perseguir.</summary>
    [Export] public float SpawnDistance { get; set; } = 0.9f;
    [Export] public float EntryDistance { get; set; } = 3f;
    [Export] public float SlideTime { get; set; } = 0.25f;
    [Export] public Color LampIdle { get; set; } = new Color(0.35f, 0.08f, 0.1f);
    [Export] public Color LampAlert { get; set; } = new Color(1f, 0.15f, 0.2f);

    public Vector2 SpawnPosition => GlobalPosition + Inward.Normalized() * SpawnDistance * Units.Px;
    public Vector2 InwardDir => Inward.Normalized();
    public Vector2 EntryPosition => GlobalPosition + Inward.Normalized() * EntryDistance * Units.Px;

    readonly Queue<Action<SpawnDoor>> pending = new Queue<Action<SpawnDoor>>();
    Node2D panelA, panelB;
    CanvasItem alert, lamp;
    Tween slide;
    DoorState state;
    float timer;

    public override void _Ready()
    {
        panelA = GetNode<Node2D>("PanelA");
        panelB = GetNode<Node2D>("PanelB");
        lamp = GetNode<CanvasItem>("Lamp");
        alert = GetNode<CanvasItem>("Alert");
        alert.Visible = false;
        lamp.Modulate = LampIdle;
    }

    /// <summary>Agenda um spawn; a ação recebe a porta quando ela estiver aberta.</summary>
    public void Enqueue(Action<SpawnDoor> spawn) => pending.Enqueue(spawn);

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        switch (state)
        {
            case DoorState.Idle:
                if (pending.Count == 0) return;
                state = DoorState.Alert;
                timer = AlertTime;
                alert.Visible = true;
                break;

            case DoorState.Alert:
                timer -= dt;
                float pulse = Mathf.Abs(Mathf.Sin(timer * 12f));
                alert.Modulate = new Color(alert.Modulate, 0.35f + 0.65f * pulse);
                lamp.Modulate = LampIdle.Lerp(LampAlert, pulse);
                if (timer > 0f) return;
                alert.Visible = false;
                lamp.Modulate = LampAlert;
                Slide(true);
                state = DoorState.Open;
                timer = 0f;
                break;

            case DoorState.Open:
                timer -= dt;
                if (timer > 0f) return;
                if (pending.Count > 0)
                {
                    pending.Dequeue()(this);
                    timer = ReleaseInterval;
                }
                else if (timer <= -CloseDelay)
                {
                    Slide(false);
                    lamp.Modulate = LampIdle;
                    state = DoorState.Idle;
                }
                break;
        }
    }

    /// <summary>As folhas têm o pivô na ponta presa à parede: encolher em Y = deslizar para dentro dela.</summary>
    void Slide(bool open)
    {
        slide?.Kill();
        float y = open ? 0.06f : 1f;
        slide = CreateTween().SetParallel();
        slide.TweenProperty(panelA, "scale:y", y, SlideTime);
        slide.TweenProperty(panelB, "scale:y", y, SlideTime);
    }
}
