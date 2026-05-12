# Defense Prep — Namų darbas 3 (ML-Agents)

**Course:** Dirbtinis intelektas žaidimuose (FMGSB23402)
**Topic:** Mašininio mokymosi (ML) funkcijų įgyvendinimas
**Implementation:** Option B (separate ML scene) **+** Option A (model integrated into FPS as a floating-pill enemy)

---

## 30-Second Elevator Pitch

> *"Sukūriau ML-Agents agentą, kuris naudoja **Proximal Policy Optimization (PPO)** algoritmą, kad išmoktų judėti prie tikslo, vengdamas kliūčių. Apmokiau jį 500,000 žingsnių 6 paralelinėse arenose. Po apmokymo modelį (ONNX) integravau į savo FPS žaidimą kaip skrendantį priešą, kuris persekioja žaidėją."*

In English:
> *"I built an ML-Agents agent that uses Proximal Policy Optimization to learn to navigate toward a target while avoiding obstacles. Trained it for 500,000 steps across 6 parallel arenas. The trained model is exported as ONNX and integrated into my FPS game as a floating pill enemy that chases the player using only the policy it learned during training — no NavMesh, no scripted behavior."*

---

## 1. What the Agent Does (Problem Solved)

**Lithuanian:** *Agentas mokosi pasiekti tikslą vengdamas kliūčių, naudodamas raycast'us kaip "akis" ir santykinę kryptį iki tikslo. FPS žaidime tas pats modelis valdo skrendantį priešą-piliulę, kuris persekioja žaidėją.*

**English:**
- **Training-scene problem:** Reach a randomly-placed target inside a 16×16 arena containing 4 obstacles and 4 walls. The agent doesn't know its absolute coordinates — only direction-to-target, own velocity, and what its rays detect.
- **FPS-game problem:** The same trained policy controls a floating-pill enemy. The "target" is now the moving player; the pill chases continuously without pathfinding.

This is **target-seeking with reactive obstacle avoidance**, learned end-to-end (no rules, no FSM, no behavior tree, no NavMesh).

---

## 2. Observations (Stebėjimai) — 42 floats per decision

| Source | Component | Size | Description |
|---|---|---|---|
| Vector observation | `CollectorAgent.CollectObservations` | 3 | Normalized direction from agent to target, in agent's local frame |
| Vector observation | `CollectorAgent.CollectObservations` | 3 | Agent's current velocity, local frame |
| Ray sensor | `RayPerceptionSensor3D` | 36 | 9 rays × 4 floats each |

**Ray-sensor configuration:**
- 9 rays in total (1 center + 4 left + 4 right), 170° per side → near-360° coverage
- Ray length 10 m (training) / 15 m (FPS scene)
- Sphere cast radius 0.5
- Detectable tags: `Obstacle`, `Target` (renamed to `Player` in the FPS scene — same observation shape)
- Per ray, the sensor reports:
  1. Was tag #1 (`Obstacle`) hit? (0 or 1)
  2. Was tag #2 (`Target` / `Player`) hit? (0 or 1)
  3. Was anything hit? (0 or 1)
  4. Normalized distance to first hit (0 = at agent, 1 = max range)

**Lithuanian defense answer:**
> *Agentas mato 42 skaitines reikšmes: 3 — kryptis iki tikslo, 3 — savo greitis, 36 — 9 raycast'ai, kurie rodo ar ten yra kliūtis, taikinys, ir atstumas iki jų.*

---

## 3. Actions (Veiksmai) — 2 continuous floats

| Index | Range | Effect |
|---|---|---|
| `actions[0]` | `[-1, +1]` | World-space X-axis force on the Rigidbody |
| `actions[1]` | `[-1, +1]` | World-space Z-axis force on the Rigidbody |

- Force applied as `ForceMode.Acceleration` (mass-independent)
- Horizontal velocity clamped at 6 m/s so the policy can't accelerate indefinitely
- Y-axis movement is handled separately: rotation locked, position frozen on Y (training) or ground-following raycast (FPS)
- Decisions are made every 5 physics frames (`DecisionPeriod = 5`) → ~10 decisions per second

**Lithuanian defense answer:**
> *Agentas išveda 2 tolydžias reikšmes nuo -1 iki +1, kurios paverčiamos jėga pasaulio X ir Z ašyse. Sprendimai priimami kas 5 fizikos kadrus.*

---

## 4. Rewards (Atlygiai) — the training signal

| Event | Reward | Why |
|---|---|---|
| Reach target (trigger entry) | **+10.0** | Strong positive signal; ends episode successfully |
| Hit obstacle (collision) | **-1.0** | Negative signal; ends episode as failure |
| Fall off arena | **-0.5** | Safety penalty; ends episode |
| Each step | **-1 / MaxStep** = **-1/600** | Time penalty; encourages efficiency. Standing still for the full episode = -1.0 |
| Per step | **+0.5 × Δdistance** | Proximity shaping — positive if got closer, negative if got farther |

**Telescope property of the proximity reward:**
The sum of all per-step proximity rewards in an episode equals `0.5 × (initial_distance − final_distance)`. So if the agent reaches the target from 10 m away: `+0.5 × 10 = +5` proximity total. Combined with the `+10` reach reward and the small time penalty (`~-0.1`), a perfect episode scores around **+15**.

**Reward design lessons (worth mentioning if asked about iteration):**
- v1: `reach=1.0, proximity=0.01` — agent learned to do *nothing* (standing still = -1.0 timeout was preferable to risking -1.0 obstacle penalty)
- v2: `reach=3.0, proximity=0.05` — still got stuck against walls (walls were untagged → invisible to ray sensor)
- v3 (final): `reach=10.0, proximity=0.5, walls tagged Obstacle, locked rotation, 360° rays` — Mean Reward rose from -1 to +11 in ~200,000 steps

**Lithuanian defense answer:**
> *Atlygio sistema: +10 už tikslo pasiekimą, -1 už kliūties paliestimą, -1/600 už kiekvieną žingsnį, +0.5 × pasikeitusio atstumo už artėjimą prie tikslo. Tai vadinama "reward shaping" — silpną teigiamą signalą paverčiame stipriu.*

---

## 5. Episode Reset Logic — the per-episode randomization

Each episode starts with `TrainingArea.ResetArea()`:
1. Pick a random position for the **target** within 16×16 bounds.
2. Pick random positions for all **4 obstacles**, avoiding the target.
3. Pick a random position for the **agent**, avoiding both target and obstacles (min separation 2 units).
4. Zero the agent's velocity.
5. Reset internal distance tracker.

**Why this matters:** Without randomization, the agent would just memorize one layout and fail at any other. Random initial conditions are required for the policy to *generalize*.

Episode ends when:
- Agent enters target trigger → `+10` reward, `EndEpisode()`
- Agent collides with an obstacle → `-1` reward, `EndEpisode()`
- Agent falls below `y = -2` → `-0.5` reward, `EndEpisode()`
- 600 steps elapse → no explicit reward, episode auto-ends (timeout penalty already accumulated step-by-step)

---

## 6. Meaningful Modifications vs. Stock RollerBall Example

The homework rules require **2–3 meaningful modifications** if using Option B. We have **5**:

| # | Modification | Where |
|---|---|---|
| 1 | **Custom observation stack:** relative target direction + local velocity + 9-ray 360° sensor | `CollectorAgent.CollectObservations()` + `RayPerceptionSensor3D` |
| 2 | **Obstacle-avoidance hazard layer** — agents must dodge 4 randomly-placed cubes | `ArenaObstacle.cs` |
| 3 | **Per-episode randomization** of agent, target, AND obstacles | `TrainingArea.ResetArea()` |
| 4 | **Shaped reward system** — proximity + reach + obstacle + time penalty | `CollectorAgent.OnActionReceived()` |
| 5 | **Cross-scene policy transfer** — same `.onnx` deployed in the FPS scene with a moving target (the player) | `FloatingPillEnemy.cs` |

---

## 7. Training Configuration

**Algorithm:** PPO (Proximal Policy Optimization)
**Network:** 2 fully-connected layers × 128 units
**Total parameters:** ~30,000 weights
**Library:** Unity ML-Agents 4.0.3 + Python mlagents 1.1.0 + PyTorch 2.2.2
**Hardware:** Trained locally on macOS (Apple Silicon)

| Hyperparameter | Value |
|---|---|
| `batch_size` | 1024 |
| `buffer_size` | 10240 |
| `learning_rate` | 3e-4 (linear decay) |
| `beta` (entropy coefficient) | 5e-3 |
| `epsilon` (PPO clip) | 0.2 |
| `lambd` (GAE) | 0.95 |
| `num_epoch` | 3 per update |
| `gamma` (discount) | 0.99 |
| `max_steps` | 500,000 |
| `time_horizon` | 64 |
| Parallel arenas | 6 |

**Training time:** ~10 minutes wall-clock (with 6 parallel arenas + Unity time scale boost).

**Result:** Mean Reward rose from -1.0 (random) → +11.5 (near-optimal) by step 480,000. TensorBoard chart available as `Environment/Cumulative Reward`.

---

## 8. How the Decision Loop Works at Runtime

```
                ┌─────────────────────────────────┐
                │  Every ~0.1s (DecisionPeriod=5) │
                └─────────────┬───────────────────┘
                              ▼
            ┌─────────────────────────────────────────┐
            │ COLLECT OBSERVATIONS (42 floats)        │
            │   ─ Direction to player (3)             │
            │   ─ Own velocity (3)                    │
            │   ─ 9 rays × {tag0, tag1, anyHit, dist}│
            └─────────────┬───────────────────────────┘
                          ▼
            ┌─────────────────────────────────────────┐
            │ NEURAL NETWORK FORWARD PASS             │
            │   42 floats → [128] → [128] → 2 floats  │
            │   (trained .onnx, no learning at runtime)│
            └─────────────┬───────────────────────────┘
                          ▼
            ┌─────────────────────────────────────────┐
            │ APPLY ACTIONS                           │
            │   forceX = clamp(out[0]) × moveForce    │
            │   forceZ = clamp(out[1]) × moveForce    │
            │   Rigidbody.AddForce(...)               │
            └─────────────────────────────────────────┘
```

**Key insight:** At inference time there is *no learning*, *no reward computation*, *no Python*. Just a static neural network mapping observations to actions. The network IS the learned behavior.

---

## 9. Demo Script (what to show, in order)

### Part A — Training scene (`MLTrainingScene`)

1. **Open the scene** → show the 6-arena layout with floor, walls, obstacles, agent capsule, green target.
2. **Show the Inspector on the Agent:**
   - `CollectorAgent.cs` (highlight `_proximityScale = 0.5`, `_reachReward = 10`, `_maxStep = 600`)
   - `BehaviorParameters` (Behavior Name = "Collector", VectorObservationSize = 6, Continuous Actions = 2, Behavior Type = **Inference Only**, Model = Collector.onnx)
   - `RayPerceptionSensor3D` (9 rays, 170° per side, Obstacle + Target tags)
3. **Press Play.** Agents reach targets quickly, dodge obstacles. Episodes reset constantly.
4. **Open TensorBoard** (`results/Collector_v1/...`) → show the Mean Reward curve climbing from -1 to +11.

### Part B — FPS integration (`SampleScene`)

1. **Open SampleScene** → show the `FloatingPillEnemy_01` GameObject in the Hierarchy.
2. **Show its components:** same `BehaviorParameters` (Inference Only, same Collector.onnx model), `FloatingPillEnemy.cs` (subclass of CollectorAgent), Ray sensor tagged `Obstacle` + `Player`.
3. **Press Play.** Walk around. The red pill chases you across uneven terrain (ground-follow raycast).
4. Let it catch you → take damage (10 HP per hit, 0.6 s cooldown).

### Part C — The "explain" beat

Walk through Sections 2, 3, 4 of this document (observations / actions / rewards) using the Inspector to ground the explanation.

---

## 10. Likely Defense Questions + Prepared Answers

### Q: Kokia mašininio mokymosi technika naudojama?
**A:** PPO — Proximal Policy Optimization, on-policy actor-critic reinforcement learning algorithm. Implemented in ML-Agents 4.0.3.

### Q: Kaip agentas mokosi?
**A:** Trial and error in 6 parallel arenas. After every ~10k physics steps, PPO computes gradients on collected `(observation, action, reward)` triples and updates the network so high-reward actions become more likely. Process repeated 500,000 steps total.

### Q: Kokias problemas sprendžia šis agentas?
**A:** Target-seeking with reactive obstacle avoidance. No NavMesh, no A*. The agent reads its surroundings via rays and a direction-to-target vector, then outputs movement forces.

### Q: Ar tai iš anksto apmokytas modelis? Iš kur paimtas?
**A:** No, I trained the model myself from scratch. Configuration: `ml-training/collector_config.yaml`. Trained locally with `mlagents-learn`. Final ONNX at `Assets/ML/Models/Collector.onnx`. TensorBoard logs at `results/Collector_v1/`.

### Q: Kodėl tie konkretūs atlygio skaičiai?
**A:** I tuned them iteratively. v1 had `reach=1.0, proximity=0.01` — the agent learned a "do nothing" policy because the safest action was to never move (any movement risked an obstacle, while standing still scored a known -1.0 timeout). v2 fixed wall visibility (tagged them Obstacle) and locked agent rotation. v3 scaled rewards up (`reach=10, proximity=0.5`) so the positive signal dominated. Mean Reward immediately took off.

### Q: Kuo ML agentas skiriasi nuo jūsų esamų priešų (EnemyFSM)?
**A:** The existing mutante enemies use a Finite State Machine with hand-coded transitions and NavMesh pathfinding. The floating pill uses *only* the learned neural network — no NavMesh, no explicit state machine, no hand-coded rules. Same chase behavior, different implementation paradigm.

### Q: Ar agentas matosi pats? (Self-observation?)
**A:** No — it has no observation of its own absolute position or rotation. Only relative direction to target, own velocity vector, and what its 9 rays detect nearby. This is intentional — it forces the policy to be position-invariant and generalize across the whole arena.

### Q: Kodėl įšaldote rotaciją?
**A:** With actions in world XZ and observations in agent-local frame, a rotating agent creates an inconsistent training signal (the same world-space action produces different observations after rotation). Freezing all rotation makes the local and world frames identical and removes a useless degree of freedom. The 360° ray coverage means the agent never needs to "turn to look."

### Q: Ar pasiruošę?
**A:** Yes. 🙂

---

## 11. Files to Reference During Demo

| File | What it shows |
|---|---|
| `Assets/ML/CollectorAgent.cs` | Observations, actions, rewards |
| `Assets/ML/TrainingArea.cs` | Episode reset logic, randomization |
| `Assets/ML/FloatingPillEnemy.cs` | FPS integration subclass |
| `ml-training/collector_config.yaml` | PPO hyperparameters |
| `Assets/Scenes/MLTrainingScene.unity` | Training environment with 6 arenas |
| `Assets/Prefabs/FloatingPillEnemy.prefab` | The deployed runtime enemy |
| `Assets/ML/Models/Collector.onnx` | The trained neural network (95 KB) |
| `results/Collector_v1/` | TensorBoard logs + checkpoints |

---

## 12. Lithuanian Glossary (key terms you may need)

| English | Lithuanian |
|---|---|
| Machine learning | Mašininis mokymasis |
| Reinforcement learning | Sustiprintas mokymasis / atlygio mokymasis |
| Neural network | Neuroninis tinklas |
| Observation | Stebėjimas |
| Action | Veiksmas |
| Reward | Atlygis |
| Episode | Epizodas |
| Policy | Politika / sprendimo strategija |
| Training | Mokymas / apmokymas |
| Inference | Inferencija / sprendimo priėmimas (be mokymo) |
| Hyperparameter | Hiperparametras |
| Raycast | Spindulinis tyrimas / raycast'as |
| Gradient | Gradientas |
| Trial and error | Bandymai ir klaidos |

---

## 13. Backup talking points (if something breaks during demo)

- **If the FPS pill gets stuck mid-air:** "The ground-follow raycast couldn't find a surface — this is a known v2 issue. The training scene demo still works."
- **If the pill ignores the player:** "The ray sensor isn't tagged Player on this build — but you can see the direction-to-target vector in the Inspector. The chase logic is correct; only the runtime tag is mismatched."
- **If Unity crashes:** "The model is portable — let me show you the TensorBoard logs and the CollectorAgent script directly to walk you through the design."

---

**Good luck. Sėkmės. 🍀**
