# Parcial 2 — Perception, Graphs and Pathfinding: Plan

Goal: extend Parcial 1 (Boids + Hunter FSM) with wolves, FOV/LOS perception, a hand-built navigation graph and A*, aiming for a perfect grade.
Unity **6000.3.17f1** is the version the brief requires; the project was built on 6000.3.22f1 (see TASKS L23). Public repo: `github.com/Akykes25/Parcial-IA-2`.

## 1. Inputs that drive every decision

| Source | What it demands |
|---|---|
| Brief (`Consigna/Consigna parcial 2.txt`) | Zones, ≥6 boids, Hunter FSM (Search/Attack/Gather/Flee/Exit/Dead), ≥4 wolves, FOV/LOS, graph + A*, waypoint patrol, required feedback and gizmos |
| Parcial 1 feedback | FSM must be **reusable and decoupled** from the agent; no god scripts (>500 lines); no `FindObjectOfType` |
| Class 14 "what not to do" | No `Find*`; no scene references wired into agents; no static wolf list; short Update/Awake; no copied class code; no repeated costly physics queries; English only; unsubscribe events |
| Teacher notes (`Material/Cosas a tener en cuenta para el parcial.md`) | See section 2. These rules override any earlier design choice. |

## 2. Teacher rules and how the design meets them

| # | Rule (verbatim) | Design decision |
|---|---|---|
| N1 | "VISITED NO DEBERIA ESTAR" | A* has no `visited`/closed list. `gScore` + `cameFrom` dictionaries; stale heap entries are skipped when their cost is above the known `gScore`. |
| N2 | "NO USAREMOS HEURISTICMETHOD COMO LO TIENE EL. LA UNICA JUSTIFICACION ES CUANDO EL MAPA ES DEMASIADO GRANDE, SINO NO" | No heuristic method. The open set is ordered by accumulated cost only. Done in T3 (`10c8fa8`). |
| N3 | "EN CASO DE UN MAPA GRANDE, USAR MAS DE UN GRAFO" | Variant A is small (52×32 m, 41 nodes): **one** `NavigationGraph`. Wolves reuse it through a node filter (no safe-zone nodes) instead of a second graph. |
| N4 | "HAY QUE PRESTAR MUCHA ATENCION EN LA OPTIMIZACION" | See section 5 (performance budget). |
| N5 | "SI EL TARGET ESTA EN LA MISMA LINEA DE VISION NO HACE FALTA HACER UN PATFINDING COMPLEJO, SOLAMENTE USAR EL PATFINDING CUANDO NO ESTOY VIENDO AL OBJETIVO" | Every move goes through one rule: **LOS to target → steer directly; no LOS → A***. Applies to Hunter trips, wolf chase/investigate, and patrol waypoints. |
| N6 | "OBVIAMENTE NO USAR NAVMESH" | `com.unity.ai.navigation` removed from the manifest (T0). |
| N7 | "NO USAR WAYPOINTS Y LA GRILLA" | No grid graph: nodes are placed by room/corridor topology, not as a uniform grid. Agents never move by walking a waypoint list blindly. Patrol waypoints exist only because the brief requires them (1.8) and are **graph nodes**, reached with N5. *Assumption to confirm with the teacher.* |
| N8 | "USAR ASTAR PARA MOVERSE" | Single `AStarPathfinder`, shared by Hunter and wolves. |
| N9 | "USAR FSM" | Generic `StateMachine` used by Hunter and wolves. |
| N10 | "MOVIMIENTO DEL PATHFINDING HACERLO CON LOS STEERING BEHAVIOURS" | `PathFollower` returns steering velocity: Seek between nodes, Arrive on the last one. |
| N11 | "PARA LOS LOBOS LO RECOMENDABLE ES USAR UN SINGLETON, UN EVENTO ESTATICO TAMBIEN SE PUEDE USAR, PERO ES MAS RECOMENDABLE EL SINGLETON" | `WolfPack` scene singleton with **instance** events (`HowlRaised`, `HunterSheltered`). No static events, no static wolf list. |

## 3. Architecture (`Assets/Scripts`, one responsibility per file, ~250 lines max)

| Folder | Contents |
|---|---|
| `FSM/` | `State`, `StateMachine<TKey>`: generic, agent-agnostic. Transitions are conditions registered on the machine; "any" transitions are evaluated first in priority order. Used by **both** Hunter and Wolves. |
| `Core/` | `SimulationContext` (scene singleton: graph, zones), `ZoneVolume` + `ZoneType` (trigger, colored floor, gizmo), `GameLayers` |
| `Navigation/` | `GraphNode`, `NavigationGraph`, `AStarPathfinder` (min-heap, `gScore`/`cameFrom`, no heuristic, no visited list, node filter, returns path cost), `PathFollower` (steering along the path; `SteerDirect` when the target is visible), `NavigationAgent` (planned: owns the N5 rule — LOS check, A* only when blocked, repath only when the target changes node) |
| `Perception/` | `PhysicsSensorBuffer` (reused NonAlloc buffer, layer mask), `VisionSensor` (range, optional angle, LOS raycast only against candidates, scheduled scans) |
| `Steering/` | `SteeringBehaviours` (Seek, Arrive, Pursuit with prediction, Evade) |
| `Boids/` | Parcial 1 boids split into agent / flocking / motor; bounded to the hunting ground |
| `Hunter/` | `HunterController` (inspector variables incl. `RequiredGathers`, `RespawnTime`, `SafeZoneWaitTime`), `HunterBrain` (FSM wiring, priority Dead > Flee > Exit > Gather > Attack > Search), one class per state |
| `Wolves/` | `WolfPack` (scene singleton, instance events), `WolfController`, `PatrolRoute` (list of `GraphNode`, loop or ping-pong), states Patrol / Chase / Investigate |
| `Feedback/` | HUD (hunter state, gathers, chosen safe zone, alerts) and gizmos |
| `Editor/` | `VariantALevelBuilder` + `Levels/` (menu builds the Variant A level), `GraphBaker` (links nodes with a sphere cast against `Obstacle`, once, at edit time) |

## 4. Map: Variant A (ring) — main scene `Assets/Scenes/VariantA.unity`

Layout follows `Material/Mapa variante A.png` and is generated by `Tools/Build Variant A Level` (`Assets/Editor/VariantALevelBuilder.cs` + `Assets/Editor/Levels/`): a 52×32 m ring of 3 m corridors; Start (west) and Exit (east) outside the ring, joined by a straight east–west axis that crosses the west ring, the Hunting ground (centre) and the east ring. Two safe zones are dead ends off the axis, inside the ring (SW and NE), away from every patrol route and patrol corner, as the brief asks. North and south rooms hang off the ring. Patrols: L1 west ring (ping-pong), L2 north ring + north room (loop), L3 east ring (ping-pong), L4 south ring + south room (loop). Wolves need A* whenever the Hunter's last known position is around a ring corner.

Graph: 41 nodes at ring corners, crossings, doorways and room centres, plus a 3×3 set in the hunting ground (no uniform grid, N7); 48 explicit links, all clear of walls with a 0.45 m sphere cast; node–wall clearance ≥ 1.5 m.

Why not Variant C (kept as `Assets/Scenes/SampleScene.unity`, commit e84479f): its safe-zone doors sat on patrol corners and its columns hid wolves until 2–4 m; in play mode no full cycle reached the Exit (TASKS L16–L18). Variant A completes cycles repeatedly (L19–L20).

## 5. Performance budget (N4)

- No allocations per frame: reused lists and NonAlloc physics buffers; no LINQ in runtime code.
- Perception scans on a fixed interval (not every frame), LOS raycast only for candidates already inside range.
- A* runs only when there is no LOS (N5), and repaths only when the target moves to another closest node or the path ends — never every frame.
- One reusable `AStarPathfinder` per agent; `sqrMagnitude` for distance comparisons.
- Graph links baked at edit time; no runtime raycasts to build the graph.
- Profiler capture of a full cycle (6 boids, 4 wolves, hunter) attached to the delivery notes.

## 6. Team and phases

Local Unity implementation uses the existing checkout. The pre-established branch/worktree-per-unit layout is reserved for later GitHub publishing, avoiding a duplicate Unity project per branch. Writers and reviewers pair Claude and Codex (GPT): whoever does not write, reviews the diff against the brief, Class 14, the Parcial 1 feedback **and the teacher rules N1–N11**.

| Phase | Unit | Branch | Writer | Reviewer |
|---|---|---|---|---|
| 0 | Setup, shared contracts, docs | `chore/setup` | Claude (orchestrator) | — |
| 1 | Level Variant C + LevelBuilder | `feat/level-variant-c` | Codex | Claude Sonnet |
| 1 | Graph audit against N3/N7 + bake | `feat/level-variant-c` | Claude | Codex |
| 1 | Boids port | `feat/boids-port` | Codex | Claude Sonnet |
| 1 | Graph + A* + PathFollower | `feat/graph-astar` | Claude Opus (medium) | Codex |
| 1 | Perception + FSM tests | `feat/perception-fsm` | Claude Sonnet | Codex |
| 2 | Hunter | `feat/hunter` | Claude Opus (medium) | Codex |
| 2 | Wolves | `feat/wolves` | Codex | Claude Sonnet |
| 3 | Integration, HUD, gizmos | `feat/feedback-integration` | Codex | Claude |
| 4 | README, PDF draft, rubric checklist | `docs/delivery` | Claude | Codex |

Phase 2 starts after Phase 1 is in the main checkout. Merges to `main` are local; push and public visibility are decided by the student.

## 7. Verification

- EditMode tests (Unity Test Runner) for A*, path following, graph baking, perception and FSM transitions/priorities.
- `rg "Find(First)?Object|GameObject\.Find|FindObjectsByType"` over `Assets/Scripts` returns nothing.
- `rg -i "visited|heuristic"` over `Assets/Scripts/Navigation` returns nothing (N1, N2).
- `rg "static event|static List"` over `Assets/Scripts/Wolves` returns nothing (N11).
- Play-mode checklist per brief item 1.1–1.12 (full cycle, flee to nearest safe zone by path length, death + 5 s respawn, howl and pack reaction, wolves never enter safe zones, all gizmos visible), plus: a visible target is reached without an A* path gizmo (N5).
- Profiler: no GC allocation spikes during the full cycle (N4).
- Repo opened while logged out before delivery.
