# Estações da Sala de Descanso Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dar efeito às 4 estações da Sala de Descanso, criar Bebidas (Bar + drops), aplicar as Tabelas A/B de Sorte e completar a Loja Externa (Barman, Sorte) — issue #16.

**Architecture:** Regras puras e estáticas (`LuckTables`, `DrinkRules`, `UpgradeRoll`, `WeaponSlotRoll`, `PassiveCatalog`) testadas no `--selfcheck`. Estado da run em `RunState` (estático, sobrevive a troca de cena, zerado no `Restart`). Combate só lê `RunState` por ganchos de 1 linha. Modais de Bar/Melhorias/Roleta num `LoungeModalUI` construído em código; Caça-níquel de Armas reaproveita `SlotMachineUI`.

**Tech Stack:** Godot 4.7.2 .NET, C# (net10.0). Sem framework de testes: `SelfCheck.Check`.

**Spec:** `docs/superpowers/specs/2026-11-27-estacoes-sala-descanso-design.md`

## Global Constraints

- Build: `PATH=~/.dotnet:$PATH DOTNET_ROOT=~/.dotnet dotnet build` — 0 erros.
- Testes: `~/Henrique/godot/Godot_v4.7.2-stable_mono_linux.x86_64 --headless --path . -- --selfcheck` imprime `selfcheck OK`.
- Valores literais do PDF (seções 4, 5 e 7). Textos de UI em pt-BR, caixa alta como o resto do jogo.
- Não refatorar `PlayerController`/`WeaponSystem` além dos ganchos (a #15 mexe nesses arquivos).
- Commits só quando o usuário pedir; nunca adicionar `CLAUDE.md`/`.claude/`.

---

### Task 1: Loja Externa — Barman e Sorte

**Files:** Modify `scripts/Meta/MetaProgress.cs`, `scripts/UI/EndScreenUI.cs` (se 5 botões não couberem), `scripts/SelfCheck.cs`.

**Produces:** `MetaProgress.Upgrade.Barman`, `MetaProgress.Upgrade.Sorte`, `MetaProgress.LuckLevel` (int 0–5), `MetaProgress.BarmanLevel` (int 0–5).

- [ ] Check (falha antes): `MetaProgress.CostOf(Upgrade.Barman, 0..4) == {100,200,350,600,950}` e `CostOf(Upgrade.Sorte, …) == {150,300,550,900,1450}`.
- [ ] Enum vira `{ Vigor, Arsenal, Imortalidade, Barman, Sorte }` (append: índices antigos e save por nome continuam válidos); `Names`, `Descriptions`, `Costs`, `levels = new int[Names.Length]`; `public static int CostOf(Upgrade u, int level) => Costs[(int)u][level];`.
- [ ] `LuckLevel => Level(Upgrade.Sorte)` — único ponto de leitura (Hardcore #30 zera aqui).
- [ ] Build + selfcheck verdes.

### Task 2: Sorte — Tabelas A/B, rolos do Caça-níquel de Armas, 1x/2x/3x, teto Nv 10

**Files:** Create `scripts/Casino/LuckTables.cs`, `scripts/Casino/WeaponSlotRoll.cs`. Modify `scripts/Casino/SlotRules.cs`, `scripts/Weapons/WeaponInstance.cs`, `scripts/SelfCheck.cs`.

**Produces:**
- `LuckTables.WeaponTierPercent(int luck, int tier) -> int`, `LuckTables.RollWeaponTier(int luck, System.Random rng) -> int (1..3)`.
- `LuckTables.UpgradeBandPercent(int luck, int band) -> int` (band 0..2), `LuckTables.RollUpgradePercent(int luck, System.Random rng) -> int ∈ {5,10,…,30}`.
- `WeaponSlotRoll.BuildReels<T>(IReadOnlyList<T> pool, int tier, System.Random rng) -> T[3]`.
- `SlotRules.LevelsFor(m)` = 1/2/3; `WeaponInstance.MaxLevel = 10`.

- [ ] Checks: cada linha soma 100; 20.000 sorteios em Nv 0 e Nv 5 ficam a ±2 p.p. da tabela; `BuildReels` com pool de 8 ints dá 3/2/1 valores distintos para tier 1/2/3; `LevelsFor` 1/2/3; `AddLevels(50)` para em 10.
- [ ] Implementar com matrizes literais `{70,27,3},{62,33,5},{54,38,8},{46,43,11},{38,47,15},{30,50,20}` e `{60,35,5},{52,39,9},{44,43,13},{36,46,18},{28,49,23},{20,50,30}`; luck clamp 0..5.
- [ ] Atualizar check antigo `LevelsFor(…)==1/5/9` e o "escala por nível" (AddLevels(9) → Nv 10 continua válido).
- [ ] Build + selfcheck.

### Task 3: Bebidas, passivas e estado da run

**Files:** Create `scripts/Casino/DrinkRules.cs`, `scripts/Casino/PassiveCatalog.cs`, `scripts/Casino/UpgradeRoll.cs`, `scripts/Casino/RunState.cs`. Modify `autoloads/GameManager.cs` (Tick + Reset), `scripts/SelfCheck.cs`.

**Produces:**
- `enum DrinkType { Heal, Damage, Speed }`; `DrinkRules.Efficiency(int barman, bool openBar)`, constantes `Duration=30, HealBase=.35, DamageBase=.40, MoveBase=.25, FireRateBase=.30`; `DrinkRules.Name(DrinkType)`, `DrinkRules.Describe(DrinkType, float eff)`.
- `enum Passive` (12, ordem do PDF); `PassiveCatalog.Name(p)`, `Description(p)`, `Draw(ICollection<Passive> owned, int count, Random rng) -> List<Passive>`.
- `enum UpgradeTarget { Player, Weapon }`, `enum UpgradeStat { MaxHp, MoveSpeed, DashCooldown, Damage, Magazine, Reload }`, `struct UpgradeResult(Target, Stat, Percent)`, `UpgradeRoll.Roll(int luck, Random rng)`, `UpgradeRoll.TargetName/StatName`.
- `RunState`: `Rng`, `Reset()`, `Tick(float dt)`, `StartDrink(DrinkType)`, `Consume(DrinkType)` (cura via `PlayerController.Instance`), `TimeLeft(DrinkType)`, `IsActive(DrinkType)`, `Efficiency`, `ApplyUpgrade(UpgradeResult)`, `Bonus(UpgradeStat) -> float`, `Passives`, `AddPassive(Passive)`, derivados `DamageBonus`, `FireRateMultiplier`, `MoveSpeedMultiplier` (≤ 1.6), `DashCooldownMultiplier`, `MagazineMultiplier`, `ReloadMultiplier`, evento `Changed`.

- [ ] Checks: `Efficiency(0,true)*.35 == .525`, `Efficiency(5,true)*.35 == .665`, dano `.76`, cadência `.57`; mesma bebida reinicia para 30 s; Dano + Velocidade ativas juntas; Tick(31) expira; `Draw` nunca repete nem devolve possuída, devolve ≤ restantes; `UpgradeRoll` 1.000 vezes: stat compatível com target, percent ∈ {5..30}; `ApplyUpgrade` soma em fração; `MoveSpeedMultiplier` com +100% fica em 1.6; `Reset` zera tudo.
- [ ] `GameManager._Process`: `if (State == Playing) RunState.Tick(dt)`; `Restart`: `RunState.Reset()`.
- [ ] Build + selfcheck.

### Task 4: Ganchos no combate

**Files:** Modify `scripts/Weapons/WeaponInstance.cs`, `scripts/Weapons/WeaponSystem.cs`, `scripts/Player/PlayerController.cs`.

**Consumes:** derivados de `RunState` (Task 3).

- [ ] `WeaponInstance`: `MagazineSize => ceil(Data.Magazine(Level) * RunState.MagazineMultiplier)`; recarga = `Data.ReloadTime(Level) * RunState.ReloadMultiplier` (guarda duração atual para `ReloadProgress`); `fireTimer = Data.FireInterval / RunState.FireRateMultiplier`.
- [ ] `WeaponSystem.Fire`: `damage = d.Damage(w.Level) * (MetaProgress.DamageMultiplier + RunState.DamageBonus)` (aditivo, fórmula do PDF). HUD usa `w.MagazineSize`.
- [ ] `PlayerController`: guarda `baseMaxHp`; `RefreshMaxHp()` = `baseMaxHp * (MetaProgress.HpMultiplier + RunState.Bonus(MaxHp))` mantendo fração de HP, inscrito em `RunState.Changed`; movimento × `MoveSpeedMultiplier`; `RollCooldown × DashCooldownMultiplier`.
- [ ] Check: com `WeaponMagazine` +30% e pente 8, `MagazineSize == 11`; `Reset` volta a 8.
- [ ] Build + selfcheck.

### Task 5: Drops de Bebida

**Files:** Modify `scripts/Helpers/DropType.cs`, `scripts/Helpers/Drop.cs`, `scripts/Helpers/DropManager.cs`, `scripts/Resources/EnemyData.cs`.

- [ ] `DropType.Drink`; `EnemyData.DrinkDropChance = 0.02f` (0 no `CreateTrocoMinion`; ignorado se `IsBoss`).
- [ ] `RollDrops`: na mesma rolagem, depois de Points. `Spawn(DropType.Drink, pos)` sorteia `DrinkType` uniforme, `value = (int)type`, cor por tipo (`DrinkColors` exportado), sprite `DrinkSprite ?? HealSprite`.
- [ ] `Drop`: coleta `RunState.Consume((DrinkType)(int)value)`.
- [ ] Build + selfcheck.

### Task 6: Estações — modais

**Files:** Create `scripts/Casino/LoungeModalUI.cs`. Modify `scripts/Casino/LoungeStation.cs`, `scripts/Casino/SlotMachineUI.cs`.

- [ ] `LoungeStation.TryPay()`: valida `CanInteract`, gasta `Cost` fichas, `RecordUse()`. `Activate` abre o modal (não cobra). Roleta sem passivas restantes → estado "SEM PASSIVAS".
- [ ] `SlotMachineUI.Open(LoungeStation station = null)`: cobra no início do giro (`station.TryPay()` ou 1 ficha no modo antigo), tier por `LuckTables.RollWeaponTier(MetaProgress.LuckLevel, RunState.Rng)`, rolos por `WeaponSlotRoll.BuildReels`, botão **RECUSAR** criado em código; resultado mostra o nível final (teto 10).
- [ ] `LoungeModalUI` (CanvasLayer criado sob demanda, pausa o jogo): modo Bar (3 botões com valor já com eficiência; escolher cobra e bebe), Melhorias (GIRAR cobra; 3 rolos animados Quem/O quê/Quanto; aplica), Roleta (GIRAR cobra; 2 cartas; escolher é obrigatório). Botão SAIR antes de pagar não cobra.
- [ ] Build + selfcheck; manual na `SalaDescanso.tscn`.

### Task 7: HUD e documentação

**Files:** Modify `scripts/UI/HUD.cs`, `README.md`.

- [ ] Abaixo da barra de HP: `RichTextLabel` com uma linha por bebida ativa (`[color]DANO +40%  24s[/color]`) e as passivas da run; atualiza no refresh de 10 Hz.
- [ ] README: tabela do caça-níquel (1x/2x/3x = +1/+2/+3, teto Nv 10, 2 fichas, Recusar), estações e bebidas.
- [ ] Build + selfcheck finais; smoke headless da `SalaDescanso.tscn` sem erros no log.
