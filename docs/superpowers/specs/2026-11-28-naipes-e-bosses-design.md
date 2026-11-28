# 4 Naipes e 3 Bosses — Design

Issue: #17. Base: seções 8 e 9 de `Cassino Survivors.pdf`, com as decisões do usuário abaixo. Branch: `feat/naipes-e-bosses` (a partir de `develop`).

## Decisões do usuário (sobrepõem o PDF)

- Boss final (F15) é o **Tigrinho**, não a Máquina de Caça-Níquel. A máquina vira monstro no futuro, fora desta tarefa.
- F5 = **Rainha** (`spritesheet_rainha_*`), F10 = **Rei de Paus** (`spritesheet_chefe_paus_*`).
- Kit do Tigrinho: **Pulo do Tigre** (área vermelha no chão, o tigre pula e causa dano na área; rolar para fora evita), **Avanço do Tigre** (linha vermelha, investida com dano na linha), **Invocação de Lacaios**.
- Sprites em `assets/sprites/`: `encapuzado` = Espadas, `losango` = Ouros, `coracao_grande` = Copas, `coracao_pequeno` = Coraçõezinhos. Carta de Paus sem sprite: desenho provisório.

## Arquitetura

- `Enemy` mantém HP, física, animação, morte. Ganha um `EnemyBrain` opcional escolhido por `EnemyData.Behavior`. Sem brain = perseguição atual (Ficha, Fichona, Garrafa, Paus).
- Brains de naipe: `EspadasBrain`, `OurosBrain`, `CopasBrain`.
- `BossBrain` (base): teto de DPS, trava de fase com invulnerabilidade, fúria e fila de passos (`Then(duração, início, tick)`). Subclasses `RainhaBrain`, `ReiBrain`, `TigrinhoBrain`.
- Peças compartilhadas: `EnemyProjectile` (losango desenhado, dano por distância, para em sólidos), `Telegraph` (círculo/linha/cone vermelho que preenche até o golpe; só avança com o jogo rodando).
- Regras puras em `EnemyRules`: `ApplyDpsCap`, `ClampToLock`, `InCircle`, `InLine`, `FanDirections`, `CanSummon`.
- `HordeDirector`: boss por `StageSpec.Boss` (Rainha/Rei/Tigrinho), teto de DPS da fase, `SummonAtDoors` para reforços ($0, sem drop, nunca dourados). Tipos sem export na cena carregam o `.tres` padrão.
- HUD: barra de HP do boss no topo central com nome; dourada quando invulnerável.
- `EnemyData`: `Behavior`, `ChaseBackSheet` (costas ao subir), `FrameHeight`, `SummonEnemy`, `ProjectileDamage`, `BossDpsCap`, `KnockbackResistant` (para a #21).

## Números

Inimigos (comuns, antes da escala da fase): Espadas 32 HP / 10 contato / 3,8 u/s, aviso 0,6 s, investida 175%. Ouros 26 HP / 9 por losango a cada 2,2 s, distância 4,5–7 u. Paus 75 HP / 18 contato / 2,4 u/s, massa 6, `KnockbackResistant`. Copas 45 HP, distância ~7 u, 3 corações a cada 6 s, máx. 6; Coraçãozinho 8 HP / 4 contato / 4,6 u/s, $0.

Bosses (HP e teto vêm do `CampaignRules`): Rainha 3.200 / 55 DPS; Rei 6.800 / 105 DPS; Tigrinho 14.000 / 180 DPS. Excedente do teto no mesmo segundo × 0,2. Fúria < 40%. Trava em 50% (~10–12 s, invulnerável; a barra não passa do marco antes da trava).

- Rainha: Leque (aviso 0,6 s, 5 cartas em 50°), Espiral Real (centro, 2 braços girando), Guarda da Corte (4 Fichas + 1 Espadas, máx. 8 invocados vivos). Fúria: +30% velocidade e leque duplo. Trava: escudo, espiral intensa, 2 Espadas.
- Rei: Impacto do Cetro (círculo 3 u, aviso 1 s; fúria: onda 5 u), Avanço Soberano (alinha no eixo, aviso 0,7 s, investe até bater; 1 s parado depois; fúria: 2 seguidas), Decreto (1 Paus + 1 Ouros). Trava: Cetro, Avanço, Cetro + Paus e Ouros.
- Tigrinho: Pulo (círculo 2,5 u na posição do jogador, aviso 1 s, pousa e causa dano), Avanço (linha até o jogador estendida até a parede, aviso 0,8 s, investida com dano), Lacaios (3–5 do Ato 3 pelas portas). Fúria: 2 pulos seguidos. Trava: 3 pulos + lacaios.

## Fora de escopo

Máquina de Caça-Níquel como monstro; mais monstros/velocidade por round (#26); i-frames do dash (#15); knockback (#21).

## Testes

`--selfcheck`: teto de DPS, clamp da trava, geometria dos avisos, leque, teto de invocação, `.tres` novos carregam com o `Behavior` certo, HordeDirector resolve boss por fase. Smoke headless de cada boss e naipe. Cena temporária para o usuário jogar contra cada um.
