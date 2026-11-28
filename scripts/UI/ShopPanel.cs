using Godot;

/// <summary>
/// Loja Externa (PDF, seção 7): saldo em $ e um botão por upgrade permanente. Usada na tela final e
/// no Menu Principal.
/// </summary>
public partial class ShopPanel : VBoxContainer
{
    readonly Button[] buyButtons = new Button[MetaProgress.Names.Length];
    /// <summary>Prefixo do saldo ("LOJA" na tela final; vazio no Menu Principal, que já tem título).</summary>
    public string Title { get; set; } = "LOJA";
    Label balance;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 6);

        balance = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        balance.AddThemeFontSizeOverride("font_size", 34);
        balance.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.5f));
        AddChild(balance);

        for (int i = 0; i < buyButtons.Length; i++)
        {
            int index = i;
            var b = new Button { CustomMinimumSize = new Vector2(0f, 42f), FocusMode = FocusModeEnum.None };
            b.AddThemeFontSizeOverride("font_size", 22);
            b.Pressed += () =>
            {
                if (MetaProgress.TryBuy((MetaProgress.Upgrade)index)) Refresh();
            };
            AddChild(b);
            buyButtons[i] = b;
        }
        Refresh();
    }

    public void Refresh()
    {
        if (balance == null) return;
        balance.Text = string.IsNullOrEmpty(Title) ? $"SALDO: ${MetaProgress.Dollars}" : $"{Title}  -  SALDO: ${MetaProgress.Dollars}";
        for (int i = 0; i < buyButtons.Length; i++)
        {
            var u = (MetaProgress.Upgrade)i;
            int lvl = MetaProgress.Level(u), cost = MetaProgress.NextCost(u);
            string price = cost < 0 ? "MÁXIMO" : $"${cost}";
            buyButtons[i].Text = $"{MetaProgress.Names[i]}  Nv {lvl}/{MetaProgress.MaxLevel}  -  {MetaProgress.Descriptions[i]}  -  {price}";
            buyButtons[i].Disabled = cost < 0 || MetaProgress.Dollars < cost;
        }
    }
}
