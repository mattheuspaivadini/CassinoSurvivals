# Fase 1 — Design

Fonte: `Cassino Survivors.pdf` (v4.5), seções 2, 8, 9 e 10 (Blockout 1).

## Objetivo

Criar a Fase 1 (Ato 1) seguindo o documento: arena de combate normal com 6 portas laterais,
horda finita que libera a saída ao ser limpa, e IA de inimigos que contorna obstáculos.
Nenhum inimigo novo nesta etapa: os tipos existentes fazem o papel do bestiário.

| Papel no PDF | Stand-in atual |
|---|---|
| Ficha (enxame) | Crupiê Veloz |
| Fichona (tanque comum) | Capanga |
| Dourado (elite com ficha garantida) | Segurança Elite |

## 1. Cenário — `scenes/levels/Fase1.tscn`

- Vira a main scene. `scenes/Main.tscn` continua existindo como modo antigo (sem alterações de comportamento).
- Escala: tiles de 16px com escala 3,75 = 1 unidade (60px, `Units.Px`).
- Arena retangular vertical de 40×52 unidades (2400×3120px). O PDF pede "~1,8–2x a largura por
  ~2,2–2,5x a altura da tela", o que daria uma arena horizontal; seguimos o Blockout 1 (vertical).
- Layout do Blockout 1, de cima para baixo: saída (topo), portas 1/4, pilares P1/P2 com mesa de
  pôquer entre eles, balcões, portas 2/5, 2 mesas de roleta, balcões, pilares P3/P4, portas 3/6,
  spawn do jogador [J], entrada (base).
- Vãos entre obstáculos ≥ 4x a largura do jogador (raio 0,4u ⇒ ≥ 3,2u; usamos ≥ 4u).
- Arte: carpete, paredes, mesas, caça-níqueis decorativos dos balcões e portas animadas vindos
  de `assets/2D Top Down Pixel Art Tileset Casino` e `assets/Dicehead and Casino Asset Pack`.
- Colisão única: todo obstáculo é `StaticBody2D` na layer 1 e bloqueia movimento e projéteis.
  Tapetes e decoração de piso não têm colisão.
- Caça-níqueis jogáveis e Helpers não ficam na arena (pertencem à Sala de Descanso, etapa futura).

## 2. Regras de sala

- `HordeDirector` (novo) controla o spawn; `EnemySpawner` passa a ter `AutoSpawn` (true no
  `Main.tscn`, false na Fase 1) e continua sendo o registro de inimigos ativos.
- Horda: 25 inimigos no total, taxa constante ao longo de ~60s, sorteando portas.
- Cada porta mostra alerta visual ~1s antes de liberar o inimigo.
- Portas 3 e 6 (inferiores, perto do jogador) têm atraso inicial de 2,5s.
- 2 Dourados por sala, espaçados no início e no fim da horda.
- Escala do Ato 1 Fase 1: HP 1,0x, dano 1,0x.
- Clear: todos os 25 spawnados e mortos ⇒ porta superior abre, seta do HUD aponta para a saída.
  Jogador entra na saída ⇒ `GameManager.Victory()` ("Fase concluída") até existir a Sala de Descanso.
- Boss desligado nesta fase.

## 3. HUD

- Indicador "Ato 1 — Fase 1/5" e inimigos restantes no canto superior direito.
- Setas na borda da tela apontando para os últimos 1–3 inimigos vivos fora da câmera.
- Seta para a saída após o clear.

## 4. IA dos inimigos

- `FlowField` (novo): grade de 1 unidade cobrindo a arena; células bloqueadas vêm dos
  obstáculos. BFS (8 vizinhos, sem cortar quinas) a partir da célula do jogador, recalculado
  só quando o jogador muda de célula. Um cálculo compartilhado por todos os inimigos.
- Inimigo com linha de visão livre até o alvo vai direto; senão segue o gradiente do flow field.
- Separação leve entre vizinhos próximos para não formar fila única.
- Crupiê Veloz flanqueia: mira um ponto deslocado ao redor do jogador (ângulo próprio) quando
  está longe, e vai direto quando perto.
- Ao sair de uma porta, o inimigo anda ~1,5u para dentro antes de seguir a IA.
- No `Main.tscn` (sem flow field) o comportamento cai no chase direto atual.

## 5. Testes

- `SelfCheck`: flow field contorna um obstáculo (o caminho não atravessa célula bloqueada e
  chega ao alvo); contagem da horda (25 agendados, 2 dourados, clear só com todos mortos).
- Build `dotnet build` e `godot --headless --path . -- --selfcheck`.
- Rodar a cena e conferir visual e fluxo.
