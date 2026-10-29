# Parcial 2 — Perception, Graphs and Pathfinding: Plan

Goal: extend Parcial 1 (Boids + Hunter FSM) with wolves, FOV/LOS perception, a hand-built navigation graph and A*, aiming for a perfect grade.
Unity **6000.3.17f1** (mandatory). Public repo: `github.com/Akykes25/Parcial-IA-2`.

## 1. Inputs that drive every decision

| Source | What it demands |
|---|---|
| Brief (`Parcial 2/Consigna`) | Zones, ≥6 boids, Hunter FSM (Search/Attack/Gather/Flee/Exit/Dead), ≥4 wolves, FOV/LOS, graph + A*, required feedback and gizmos |
| Parcial 1 feedback | FSM must be **reusable and decoupled** from the agent; no god scripts (>500 lines); no `FindObjectOfType` |
| Class 14 "what not to do" | No `Find*`; no scene references wired into agents; no static wolf list (use scene singleton + event); short Update/Awake; no copied class code (no `visited` list); no repeated costly physics queries; English only; unsubscribe events |
| Teacher notes | A* only when the target is not in line of sight; path movement with steering behaviours; no NavMesh; no heuristic selector |

## 2. Architecture (`Assets/Scripts`, one responsibility per file, ~250 lines max)

| Folder | Contents |
|---|---|
| `FSM/` | `State`, `StateMachine<TKey>`: generic, agent-agnostic. Transitions are conditions registered on the machine; "any" transitions are evaluated first in priority order. Used by **both** Hunter and Wolves. |
| `Core/` | `SimulationContext` (scene singleton: graph, zones), `ZoneVolume` + `ZoneType` (trigger, colored floor, gizmo), `GameLayers` |
| `Navigation/` | `GraphNode`, `NavigationGraph`, `AStarPathfinder` (min-heap open set, gScore/cameFrom, fixed Euclidean heuristic, node filter to exclude safe-zone nodes for wolves, returns path cost), `PathFollower` (steering along the path, direct steering when target is visible) |
| `Perception/` | `PhysicsSensorBuffer` (reused, layer mask), `VisionSensor` (range, optional angle, LOS raycast only against candidates, scheduled scans) |
| `Steering/` | `SteeringBehaviours` (Seek, Arrive, Pursuit with prediction, Evade) |
| `Boids/` | Parcial 1 boids split into agent / flocking / motor; bounded to the hunting ground |
| `Hunter/` | `HunterController` (inspector variables incl. `RequiredGathers`, `RespawnTime`, `SafeZoneWaitTime`), `HunterBrain` (FSM wiring, priority Dead > Flee > Exit > Gather > Attack > Search), one class per state |
| `Wolves/` | `WolfPack` (scene singleton, `HowlRaised(Vector3)` and `HunterSheltered` events), `WolfController`, `PatrolRoute`, states Patrol / Chase / Investigate |
| `Feedback/` | HUD (hunter state, gathers, chosen safe zone, alerts) and gizmos |
| `Editor/` | `LevelBuilder` (menu builds the Variant C level), `GraphBaker` (connects nodes with raycasts once, at edit time) |

## 3. Map: Variant C (room dungeon)

Columns block line of sight, so wolves must use A* to go around them. There are three safe zones (one is a hunter-only shortcut that shows off the filtered graph). The Start → Hunting ground → Exit route is long enough for encounters. Layout follows `Parcial 2/Material/Mapa Variante C.png`: Start bottom-left, Exit top-left, Hunting ground on the right with a 3×4 node grid, Room A (L2 circuit), west room with column (L1), room B with column (L3), central corridor A↔B (L4 ping-pong).

## 4. Team and phases

Each unit lives in its own branch and worktree (`.worktree/<name>`), with a disjoint edit surface. Writers and reviewers pair Claude and Codex (GPT): whoever does not write, reviews the diff against the brief, Class 14 and the Parcial 1 feedback.

| Phase | Unit | Branch | Writer | Reviewer |
|---|---|---|---|---|
| 0 | Setup, shared contracts, docs | `chore/setup` | Claude (orchestrator) | — |
| 1 | Level Variant C + LevelBuilder | `feat/level-variant-c` | Codex | Claude Sonnet |
| 1 | Boids port | `feat/boids-port` | Codex | Claude Sonnet |
| 1 | Graph + A* + PathFollower | `feat/graph-astar` | Claude Opus (medium) | Codex |
| 1 | Perception + FSM tests | `feat/perception-fsm` | Claude Sonnet | Codex |
| 2 | Hunter | `feat/hunter` | Claude Opus (medium) | Codex |
| 2 | Wolves | `feat/wolves` | Codex | Claude Sonnet |
| 3 | Integration, HUD, gizmos | `feat/feedback-integration` | Codex | Claude |
| 4 | README, PDF draft, rubric checklist | `docs/delivery` | Claude | Codex |

Phase 2 branches are rebased on `main` after Phase 1 merges. Merges to `main` are local; push and public visibility are decided by the student.

## 5. Verification

- EditMode tests (Unity Test Runner) for A*, perception and FSM transitions/priorities.
- `rg "Find(First)?Object|GameObject\.Find|FindObjectsByType"` over `Assets/Scripts` returns nothing.
- Play-mode checklist per brief item 1.1–1.12 (full cycle, flee to nearest safe zone by path length, death + 5 s respawn, howl and pack reaction, wolves never enter safe zones, all gizmos visible).
- Repo opened while logged out before delivery.
