using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Porta lateral de spawn (PDF, seção 10): pisca um alerta ~1s, abre, solta os inimigos da fila um a um
/// e fecha. A frente da porta fica sempre desobstruída no layout.
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

    public Vector2 SpawnPosition => GlobalPosition + Inward.Normalized() * SpawnDistance * Units.Px;
    public Vector2 EntryPosition => GlobalPosition + Inward.Normalized() * EntryDistance * Units.Px;

    readonly Queue<Action<SpawnDoor>> pending = new Queue<Action<SpawnDoor>>();
    Sprite2D door;
    CanvasItem alert;
    DoorState state;
    float timer;

    public override void _Ready()
    {
        door = GetNode<Sprite2D>("Door");
        alert = GetNode<CanvasItem>("Alert");
        alert.Visible = false;
        door.Frame = 0;
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
                alert.Modulate = new Color(alert.Modulate, 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(timer * 12f)));
                if (timer > 0f) return;
                alert.Visible = false;
                door.Frame = door.Hframes - 1;
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
                    door.Frame = 0;
                    state = DoorState.Idle;
                }
                break;
        }
    }
}
