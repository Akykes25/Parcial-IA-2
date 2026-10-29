# Parcial 2 — Task list

Objective: deliver Parcial 2 (perception, graph, A*, wolves) on top of Parcial 1 for a perfect grade. Integration branch: `main` (local merges).
Delivery strategy: one branch + worktree per unit, merged per phase. Test runner: Unity Test Framework (EditMode), Unity 6000.3.17f1.

## Specs

Quotes from the brief (`Parcial 2/Consigna/Consigna parcial 2.txt`) are verbatim.

- **S1 Zones** — "Zona inicial", "Coto de caza", "Zonas seguras: como mínimo 2 áreas del mapa hacia las que el Cazador puede huir", "Salida". "Las zonas deberán poder identificarse visualmente durante la ejecución". Map: Variant C.
- **S2 Boids** — "Deberá existir un mínimo de 6 Boids. Los Boids deberán permanecer exclusivamente dentro del Coto de caza". "Los Boids deberán conservar los criterios del Parcial 1."
- **S3 Hunter FSM** — "utilizando obligatoriamente una Máquina de Estados Finita (FSM)". "Las transiciones entre estados deberán estar controladas por la propia FSM". Variables `RequiredGathers` (3), `RespawnTime` (5), `SafeZoneWaitTime`. "Prioridad entre estados, de mayor a menor: Dead, Flee, Exit, Gather, Attack, Search."
- **S4 Hunter navigation** — "Pathfinding A* sobre el grafo del escenario, utilizando el mismo grafo y la misma implementación de A* que los lobos". Steering only for the final visible stretch to a boid.
- **S5 Hunter perception** — "distancia máxima y obstáculos que impidan la línea de visión (No es necesario Angulo de visión)".
- **S6 Search / Attack / Gather / Flee / Exit / Dead** — behaviour exactly as brief sections "Estado Search" … "Estado Dead" (TBA rules, melee/range/pursuit, gather cancel without credit, flee to nearest safe zone "según la longitud del camino calculado con A*", interest objects "menos de 5 objetos activos").
- **S7 Wolves** — "mínimo de 4 lobos"; waypoint patrol; "más lento que el Cazador en todos sus estados"; FOV/LOS; howl alert; other wolves react; A* to last known position; capture radius → Hunter Dead; "No ingresar a las Zonas seguras"; drop target when the Hunter shelters.
- **S8 Perception** — "Distancia máxima de percepción. Ángulo de visión. Obstáculos que impidan la línea de visión." "Mientras el Cazador se encuentre dentro de una Zona segura, los lobos no podrán detectarlo."
- **S9 Graph** — nodes and connections for corridors, rooms and zones; connections must not cross walls; "No está permitido utilizar NavMesh".
- **S10 A\*** — implemented in the project; used for last known position, unreachable patrol points, hunter trips to hunting ground / safe zone / exit; "No será válida una implementación que utilice una ruta predefinida".
- **S11 Patrol** — "Cuando el siguiente waypoint no se encuentre dentro de la línea de visión del agente, deberá utilizarse A*"; loop or reverse, consistent.
- **S12 Alert** — "La posición transmitida mediante la alerta representa la última posición conocida del Cazador".
- **S13 Integration** — "Boids → Cazador → Lobos"; Parcial 1 systems stay functional.
- **S14 Feedback** — wolf detection, alert emitted/received, known position, A* path, hunter state + gathers, hunter detects wolf + chosen safe zone; gizmos for boid radii, hunter radii, wolf FOV.
- **S15 Delivery** — PDF "Apellido Nombre Parcial #.pdf" with names and repo link; "versión 6000.3.17f1"; public GitHub.
- **S16 Quality (Class 14 + Parcial 1 feedback)** — reusable FSM; no god scripts; no `Find*`; no scene references in agents; no static wolf list; unsubscribe events; no repeated physics queries; English only; no `visited` list.

## Tasks

| ID | Specs | Unit | Writer / Reviewer | Branch | Worktree | Status | Evidence |
|---|---|---|---|---|---|---|---|
| T0 | S15, S16 | Setup: layers, asmdefs, shared contracts (FSM, zones, graph data, context, patrol route), docs | Claude / — | `chore/setup` | main checkout | done | 549d5f7 + docs commit |
| T0b | S15 | Migrate project to Unity 6000.3.17f1 (needs Unity closed) | Claude / — | `chore/setup` | main checkout | pending | |
| T1 | S1, S9 | Level Variant C via `Tools/Build Parcial 2 Level` | Codex / Claude Sonnet | `feat/level-variant-c` | `.worktree/level-variant-c` | pending | |
| T2 | S2, S13 | Boids port from Parcial 1, split + optimized | Codex / Claude Sonnet | `feat/boids-port` | `.worktree/boids-port` | pending | |
| T3 | S9, S10, S4 | GraphBaker, A*, PathFollower + tests | Claude Opus / Codex | `feat/graph-astar` | `.worktree/graph-astar` | done | 18/18 EditMode tests green |
| T4 | S5, S8, S3 | VisionSensor + FSM tests | Claude Sonnet / Codex | `feat/perception-fsm` | `.worktree/perception-fsm` | pending | |
| T5 | S3–S6 | Hunter controller, brain, six states + tests | Claude Opus / Codex | `feat/hunter` | `.worktree/hunter` | pending | |
| T6 | S7, S11, S12 | Wolves, pack alert, patrol/chase/investigate + tests | Codex / Claude Sonnet | `feat/wolves` | `.worktree/wolves` | pending | |
| T7 | S13, S14 | Scene integration, prefabs, HUD, gizmos | Codex / Claude | `feat/feedback-integration` | `.worktree/feedback-integration` | pending | |
| T8 | S15 | README, PDF draft, rubric checklist, final cross-audit | Claude / Codex | `docs/delivery` | `.worktree/delivery` | pending | |

## Log

- **L1 (2026-10-28)** — User request: "Necesito que empecemos con el proyecto, que es basciamente un parcial para mi materia de Inteligencia Artificial. [...] Quiero que junto a Codex trabajen juntos y planifiquen que es lo que haran para este proyecto, tengan en cuenta que necesito sacarme un 10 para poder promocional la materia"
- **L2 (2026-10-28)** — "Para el mapa, quiero que elijas entre las dos varitantes [...] tengas en cuenta [...] la devolucion que me hizo del primer parcial [...] Quiero que se disparen diferentes subagentes [...] junto con Codex [...] en simultaneo [...] Quiero que reutilices lo que esta hecho del parcial 1". Decision: Variant C.
- **L3 (2026-10-28)** — "usa no se, el nivel o los boids con GPT, y Opus no lo hagas mas de nivel medio". Decision: Codex writes level, boids, wolves, integration; Opus capped at medium effort.
- **L4 (2026-10-29)** — "vas a generar un documento donde se muestre este plan y [...] un documento que enliste las tareas [...] diferentes ramas y worktrees para cada uno de estos puntos".
- **L5 (2026-10-29)** — Setup decisions: removed `com.unity.ai.navigation` (NavMesh package) from the manifest; layers 8–12 = Obstacle, Boid, Hunter, Wolf, Interest; FSM API mirrors the class `StateMachine` (RegisterState/Update) but owns transitions.
