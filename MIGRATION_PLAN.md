# Plano de migração: Unity → Godot 4 (.NET / C#)

## 1. Diagnóstico do projeto Unity

| Item | Resultado |
|---|---|
| Tipo | **2D top-down** (SpriteRenderer, Rigidbody2D, câmera ortográfica tamanho 9, resolução 1920x1080) |
| Pacotes | `ugui` (UI), `render-pipelines.universal` (URP 2D, só para sprites unlit). Sem TextMeshPro, Cinemachine, DOTween ou New Input System |
| Input | Input Manager clássico: eixos `Horizontal`/`Vertical` (setas, WASD), tecla `E`, scroll do mouse |
| Tags/Layers | Nenhuma tag além de `Player`/`MainCamera`; nenhuma layer custom (tudo em `Default`) |
| Áudio | `AudioManager` com 12 slots de `AudioClip`, **todos vazios** (nenhum arquivo de áudio) |
| Fontes/modelos/shaders | Nenhum. Só a fonte embutida da Unity |
| Persistência | Nenhuma (sem save) |
| Arte | 5 PNGs gerados por código (`Circle`, `Diamond`, `Triangle`, `Square`, `FloorTile`) |
| Cena | `Main.unity` é gerada inteira por `Editor/CassinoBuilder.cs`, que é a fonte de verdade da hierarquia e dos valores |

**Arquitetura:** managers singleton (`GameManager`, `AudioManager`, `PoolManager`, `DropManager`, `EnemySpawner`), `ScriptableObject` para armas e inimigos, pool de prefabs, colisão de projétil/coleta/contato **por distância** (`sqrMagnitude`, sem `Physics2D.Overlap`), eventos C# para a UI, corrotinas só no caça-níquel, `Time.timeScale = 0` para pausar.

## 2. Local do projeto Godot

- **Durante a migração:** `CassinoSurvivals-Godot/` (o Unity continua intacto e compilável na raiz).
- **Antes do merge (Fase 7):** `git mv` do conteúdo para a **raiz** e remoção dos arquivos Unity (`Assets/`, `Packages/`, `ProjectSettings/`) em um commit à parte. O histórico preserva o Unity. O último commit Unity é o anterior ao merge da migração.

## 3. Decisões técnicas

1. **Escala:** o código Unity usa unidades de mundo (1 unidade = 1 sprite). No Godot 1 unidade = **60 px**, o que mantém exatamente o enquadramento da câmera ortográfica 9 em 1080p. A física do Godot é calibrada para pixels, então não dá para usar 1 unidade = 1 px. Os `.tres` guardam os **valores originais em unidades**; o código multiplica por `Units.Px`.
2. **Eixo Y:** o Unity tem +Y para cima; o Godot, para baixo. Posições da cena são espelhadas em Y (o mapa fica igual visualmente). Input usa `Input.GetVector` (já em Y-baixo). A seta do `Wayfinder` usa `rotação = atan2(dir) + 90°`.
3. **Pooling removido:** `PoolManager` e `PrefabPool` saem. O Godot instancia cenas pequenas barato, e o pedido manda `Instantiate`→`PackedScene.Instantiate` e `Destroy`→`QueueFree`. A lista de inimigos ativos do spawner (usada por `FindHit`/`FindNearest`) continua, com remoção O(1).
4. **Pausa:** `Time.timeScale = 0` → `GetTree().Paused`. HUD, modal, tela final e `AudioManager` ficam em `ProcessMode.Always`.
5. **Eventos → Signals:** `GameManager` é autoload e sobrevive ao reload da cena. Com signals as conexões somem sozinhas quando o HUD é liberado, e com eventos C# ficariam referências a nós destruídos. `GameManager` ganha `Reset()` no restart.
6. **Rigidbody2D → `RigidBody2D`** (gravidade 0, rotação travada, `LinearVelocity` por frame) para manter o empurrão entre a horda. O corpo **não** é escalado; o raio do `CircleShape2D` é definido por inimigo.
7. **Câmera:** `CameraFollow` some. `Camera2D` filha do jogador com `position_smoothing`.
8. **Renderer:** `gl_compatibility` (jogo só 2D, equivale ao URP 2D leve para Iris Xe).

## 4. Mapeamento

### Scripts
| Unity | Godot (`scripts/`) | Tipo |
|---|---|---|
| `Core/GameManager` | `autoloads/GameManager.cs` | `Node` autoload, signals |
| `Core/AudioManager` | `autoloads/AudioManager.tscn` + `.cs` | `Node` autoload (cena, para atribuir clipes) + `AudioStreamPlayer` |
| `Core/CameraFollow` | removido | `Camera2D` suavizada |
| `Core/PoolManager`, `PrefabPool` | removidos | `PackedScene` + `QueueFree` |
| `Player/PlayerController` | `Player/PlayerController.cs` | `RigidBody2D` |
| `Weapons/WeaponData` | `Resources/WeaponData.cs` | `Resource` `[GlobalClass]` |
| `Weapons/WeaponInstance` | `Weapons/WeaponInstance.cs` | classe C# pura |
| `Weapons/WeaponSystem` | `Weapons/WeaponSystem.cs` | `Node` filho do jogador |
| `Weapons/Projectile` | `Weapons/Projectile.cs` | `Node2D` (sem física) |
| `Enemies/EnemyData` | `Resources/EnemyData.cs` | `Resource` `[GlobalClass]` |
| `Enemies/Enemy` | `Enemies/Enemy.cs` | `RigidBody2D` |
| `Enemies/EnemySpawner` | `Enemies/EnemySpawner.cs` | `Node` |
| `Helpers/Drop`, `DropType` | `Helpers/Drop.cs`, `DropType.cs` | `Node2D` |
| `Helpers/DropManager` | `Helpers/DropManager.cs` | `Node` |
| `Helpers/Helper` | `Helpers/Helper.cs` | `Node2D` |
| `Casino/SlotRules` | `Casino/SlotRules.cs` | estática pura |
| `Casino/SlotMachine` | `Casino/SlotMachine.cs` | `Node2D` |
| `Casino/SlotMachineUI` | `Casino/SlotMachineUI.cs` | `CanvasLayer`, `async` com timers |
| `Casino/Wayfinder` | `Casino/Wayfinder.cs` | `Node2D` |
| `UI/HUD` | `UI/HUD.cs` | `CanvasLayer` + `Label`/`RichTextLabel` |
| `UI/EndScreenUI` | `UI/EndScreenUI.cs` | `CanvasLayer` |
| `Editor/CassinoBuilder`, `MainSceneLoader` | removidos | a cena passa a ser `.tscn`. O `SelfCheck` vira `--selfcheck` |

### Cenas e prefabs
| Unity | Godot (`scenes/`) |
|---|---|
| `Scenes/Main.unity` | `Main.tscn` (Map, Player, Managers, SlotMachines, Helpers, 4 CanvasLayers) |
| `Prefabs/Enemy.prefab` | `Enemy.tscn` (`RigidBody2D` + `Sprite2D` + `CollisionShape2D`) |
| `Prefabs/Projectile.prefab` | `Projectile.tscn` (`Node2D` + `Sprite2D`) |
| `Prefabs/Drop.prefab` | `Drop.tscn` (`Node2D` + `Sprite2D`) |
| (inline na cena) Player, SlotMachine, Helper | `Player.tscn`, `SlotMachine.tscn`, `Helper.tscn` |

### Dados e assets
| Unity | Godot |
|---|---|
| `Art/Sprites/*.png` | `assets/sprites/*.png` (sem `.meta`) |
| `Data/Weapons/*.asset` (5) | `resources/weapons/*.tres` |
| `Data/Enemies/*.asset` (4) | `resources/enemies/*.tres` |
| `Settings/URP*`, `DefaultVolumeProfile` | descartados |

### Configuração (`project.godot`)
- Janela 1920x1080, stretch `canvas_items`, aspect `keep`, `Engine.MaxFps = 60`.
- Input Map: `move_left/right/up/down` (setas, WASD, analógico), `interact` (E), `weapon_prev/next` (scroll).
- Layers de física: `1 world`, `2 player`, `3 enemy`.
- Autoloads: `GameManager`, `AudioManager`.

## 5. Fora do escopo (não pedido)
Sem sons novos, sem save, sem testes além do `--selfcheck` herdado, sem exports de build.
