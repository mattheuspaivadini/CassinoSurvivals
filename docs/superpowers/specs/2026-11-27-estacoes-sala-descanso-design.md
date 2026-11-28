# Estações da Sala de Descanso, Bebidas, Sorte e Loja Externa — Design

Issue: #16. Base: seções 5 e 7 de `Cassino Survivors.pdf`. Branch: `feat/estacoes-sala-descanso` (a partir de `develop`).

## Objetivo

Dar efeito às 4 estações que hoje só descontam fichas (`LoungeStation.Activate`), criar o sistema de Bebidas (Bar e drops), aplicar as Tabelas A e B de Sorte e completar a Loja Externa com Barman e Sorte.

## Fora de escopo (dono)

- 8 armas oficiais, 2 slots, Nv 10 / Arma Dourada, ghosting do dash — #15 (Jose-Carlos-Abreu). O pool do Caça-níquel de Armas lê o array de `WeaponData`, então as armas novas entram sem mudança aqui.
- Efeitos das passivas, exceto Open Bar — #21 (Jose-Carlos-Abreu). Aqui só sorteio, escolha e registro.
- Fluxo Arena ↔ Sala de Descanso — #24. Teste das estações rodando `SalaDescanso.tscn` direto.
- Hardcore (Sorte Nv 0) — #30. Nível de Sorte lido num único ponto (`MetaProgress.LuckLevel`).
- Arte e SFX definitivos — #13 e #18. Placeholders: cor/texto e sons atuais do `AudioManager`.

## 1. Estado da run — `scripts/Casino/RunState.cs`

Classe estática (sobrevive à troca de cena), zerada em `GameManager.Restart` e no início de uma run.

- Bebidas ativas: `float[] drinkTimers` indexado por `DrinkType { Heal, Damage, Speed }` (Heal é imediata, sem timer). `Drink(DrinkType)` aplica cura ou (re)inicia o timer de 30 s. `Tick(dt)` desconta só com o jogo despausado.
- Melhorias permanentes da run (somas em fração, ex.: 0,15): `PlayerMaxHp`, `PlayerMoveSpeed`, `PlayerDashCooldown`, `WeaponDamage`, `WeaponMagazine`, `WeaponReload`. Valem para todas as armas, como o termo `MelhoriasDano` da fórmula do PDF.
- Passivas: `HashSet<Passive>` com o enum das 12 passivas do catálogo; `Has(Passive)`.
- Leituras derivadas usadas pelo combate:
  - `DamageBonus` = `WeaponDamage` + bônus da Bebida de Dano ativa (soma aditiva, junto de Arsenal).
  - `FireRateMultiplier` = 1 + bônus de cadência da Bebida de Velocidade.
  - `MoveSpeedMultiplier` = 1 + `PlayerMoveSpeed` + bônus de movimento da Bebida de Velocidade, com teto de 1,6 (+60%, seção 3).
  - `DashCooldownMultiplier` = max(0,2; 1 − `PlayerDashCooldown`).
  - `MagazineMultiplier` = 1 + `WeaponMagazine`; `ReloadMultiplier` = max(0,2; 1 − `WeaponReload`).

## 2. Regras puras (sem engine, cobertas no `--selfcheck`)

- `LuckTables`: Tabela A (tiers 1x/2x/3x) e Tabela B (faixas Baixa/Média/Alta) para Nv 0–5, valores literais do PDF. `RollWeaponTier(level, rand)` e `RollUpgradeAmount(level, rand)` (faixa pela tabela, depois 1 dos 2 valores literais da faixa, uniforme).
- `DrinkRules`: base Cura 35% HP máx, Dano +40%, Velocidade +25% mov / +30% cadência, duração 30 s. `Efficiency = 1 + 0,08 × Barman + (OpenBar ? 0,5 : 0)`. Valor final = base × eficiência (ex.: Cura com Open Bar + Barman Nv 5 = 66,5%).
- `UpgradeRoll`: Rolo 1 Jogador/Arma 50/50; Rolo 2 uniforme entre os 3 atributos do alvo; Rolo 3 via `LuckTables.RollUpgradeAmount`.
- `WeaponSlotRoll`: sorteia o tier antes da animação e monta os 3 rolos — 1x: 3 armas distintas; 2x: 1 arma em 2 slots aleatórios + 1 diferente; 3x: mesma arma nos 3. Requer pool com ≥ 3 armas (hoje 5).
- `SlotRules.LevelsFor`: passa de 1/5/9 para 1/2/3 (seção 4). Nível limitado a 10 em `WeaponInstance.AddLevels`.
- `PassivePool`: sorteia 2 passivas distintas entre as não possuídas, peso igual; se restar 1, oferece 1; se restar 0, a Roleta fica indisponível.

## 3. Estações — modais

`LoungeStation.Activate` deixa de descontar direto: abre o modal da estação; o modal cobra as fichas e chama `RecordUse()` quando o giro/compra acontece. ESC/fechar antes de girar não cobra.

- **Balcão do Bar (1 ficha):** modal com 3 botões (Cura, Dano, Velocidade) mostrando o valor já com eficiência. Escolher cobra e aplica.
- **Caça-níquel de Melhorias (1 ficha):** cobra, gira 3 rolos (Quem / O quê / Quanto) com a animação do `SlotMachineUI`, mostra o resultado e soma em `RunState`. HP máximo atualiza `PlayerController.MaxHp` e mantém a fração atual de HP.
- **Caça-níquel de Armas (2 fichas):** cobra, sorteia tier, anima, jogador escolhe 1 dos 3 slots ou **Recusar** (ficha não volta — risco do PDF). Generaliza o `SlotMachineUI` existente.
- **Mesa de Roleta (4 fichas, `LoungeRules`):** cobra, mostra 2 cartas de passiva (nome + efeito do PDF), jogador escolhe 1. Limites por sala/run já existem em `LoungeStation`.

Modais pausam o jogo (`GameManager.SetPaused`), igual ao `SlotMachineUI`.

## 4. Drops de Bebida

- `DropType.Drink`; `Drop` guarda o `DrinkType` sorteado (uniforme entre os 3) e chama `RunState.Drink` ao coletar.
- `EnemyData.DrinkDropChance` (padrão 0,02 nos comuns, 0 nos Bosses), dentro da mesma rolagem única do `DropManager.RollDrops`. Valor ajustável na #26 (planilha de balanceamento não está no repo).
- Cor/escala exportadas no `DropManager`, como os outros drops.

## 5. Efeitos no combate (ganchos mínimos, para não conflitar com #15)

- `WeaponSystem`/`WeaponInstance`: dano × (1 + bônus de nível + Arsenal + `RunState.DamageBonus`); intervalo de tiro ÷ `FireRateMultiplier`; pente × `MagazineMultiplier` (ceil); recarga × `ReloadMultiplier`.
- `PlayerController`: `MoveSpeed × MoveSpeedMultiplier`; `RollCooldown × DashCooldownMultiplier` (dash = dodge roll); HP máx × (1 + `PlayerMaxHp`) junto do Vigor.

## 6. HUD

Abaixo da barra de HP: uma linha por bebida ativa (`DANO 24s`, `VELOCIDADE 12s`) com a cor da bebida, atualizada no refresh de 10 Hz que já existe. Lista curta das passivas da run ao lado.

## 7. Loja Externa — `MetaProgress`

Enum ganha `Barman` e `Sorte`, com nomes, descrições e custos do PDF:
- Barman: 100 / 200 / 350 / 600 / 950 — +8% eficiência por nível.
- Sorte: 150 / 300 / 550 / 900 / 1450 — avança 1 linha nas Tabelas A e B.

`LuckLevel` e `BarmanLevel` expostos; o save em `user://meta.cfg` já é por nome, então saves antigos continuam válidos. `EndScreenUI` já percorre o catálogo.

## 8. Testes

- `dotnet build` sem erros.
- `--selfcheck`, novos checks: cada linha das Tabelas A/B soma 100%; distribuição empírica dos tiers (10.000 sorteios, ±2 p.p.); rolos 1x/2x/3x têm 3/2/1 armas distintas; valores do Rolo 3 ∈ {5..30, passo 5}; eficiência das bebidas bate com a tabela do PDF (52,5%, 66,5%, +76% etc.); mesma bebida reinicia timer e tipos diferentes acumulam; `PassivePool` nunca repete nem oferece passiva possuída; custos novos da loja.
- Manual: rodar `SalaDescanso.tscn`, usar as 4 estações, conferir HUD e efeitos; drop de bebida na `Fase1.tscn`.
