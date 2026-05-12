# Homework 3 — ML-Agents Integration Plan

## Context

University assignment **Namų darbas 3 — Mašininio mokymosi (ML) funkcijų įgyvendinimas** for *Dirbtinis intelektas žaidimuose (FMGSB23402)*. We must:

- Add **at least one ML-Agents-powered agent** to the FPS project (not NavMesh, not hardcoded logic).
- Define explicit **observations**, **actions**, and a **reward system**.
- Be able to demo the agent and explain training during the defense.
- Option B requires **2–3 meaningful modifications** vs. a stock example (observations, actions, rewards, layout, reset logic, or goal).

**Chosen approach (user-confirmed):** Build a standalone training scene (Option B), train a model locally with Python `mlagents`, then **integrate the trained `.onnx` model into the FPS as a simple floating-pill enemy** that chases the player. This satisfies Option B requirements *and* gives Option A bonus value with low risk.

**Why this approach is "not that hard":**
- `com.unity.ml-agents 4.0.3` is already in `Packages/manifest.json` — no Unity-side install pain.
- Python 3.10 is already available on the user's machine.
- A flat arena with a capsule + goal + obstacles trains in ~5–15 minutes of PPO and is easy to reason about.
- The same agent script works in both the training scene and the FPS scene — only the "target" changes (static goal vs. player Transform).

---

## What the agent does

**Problem the agent solves:** Reach a target in a 2D-ish arena while avoiding obstacles, using learned movement rather than NavMesh.

**Observations (vector + ray sensor):**
- Relative direction to target (`targetPos - agentPos`, normalized) — 3 floats
- Agent velocity (local space) — 3 floats
- **RayPerceptionSensor3D** (component, no code needed) — 7 rays, detects tags `Obstacle` and `Target`
- Total ≈ 6 vector observations + ray sensor stack

**Actions (Continuous, size = 2):**
- `action[0]` → X-axis force/velocity
- `action[1]` → Z-axis force/velocity
- Applied to a `Rigidbody` via `AddForce` (matches the project's physics-first style — see `CLAUDE.md` §3).

**Rewards:**
- `+1.0` on reaching target → `EndEpisode()`
- `-1.0` on hitting obstacle → `EndEpisode()`
- `-0.5` on falling off arena / timeout → `EndEpisode()`
- `-1f / MaxStep` per step (time penalty, encourages speed)
- Optional shaping: small `+0.01 * (prevDist - newDist)` per step to accelerate early learning (kept tiny so the policy still learns the sparse reward)

**Episode reset (randomized):**
- Agent spawn point randomized within arena bounds
- Target position randomized
- 3–5 obstacles repositioned randomly each episode
- Agent velocity zeroed

**Meaningful modifications vs. stock RollerBall (more than the required 2–3):**
1. Custom observation stack (relative target + velocity + ray sensor) — *changes what agent sees*.
2. Obstacle avoidance — *new hazard layer*.
3. Per-episode randomization of agent, target, **and** obstacles — *new reset logic*.
4. Shaped reward (proximity + time penalty + obstacle penalty) — *new reward system*.
5. The same trained policy is reused in a **different scene** with a *moving* target (the player) — non-trivial generalization story for the defense.

---

## Files to create

| Path | Purpose |
|---|---|
| `Assets/Scripts/ML/CollectorAgent.cs` | `Agent` subclass — observations, actions, rewards. Used in both scenes. |
| `Assets/Scripts/ML/TrainingArea.cs` | Owns one arena. Randomizes positions on `OnEpisodeBegin` callback from agent. |
| `Assets/Scripts/ML/ArenaObstacle.cs` | Tiny component; tag = `Obstacle`. Detects collisions, calls `agent.HitObstacle()`. |
| `Assets/Scripts/ML/CollectorTarget.cs` | Tiny component; tag = `Target`. Trigger volume; calls `agent.ReachedTarget()`. |
| `Assets/Scripts/ML/FloatingPillEnemy.cs` | FPS-side wrapper. Sets `target = Player.transform` each frame and applies a small upward force so the pill floats. Damages player on contact via existing `PlayerHealth.TakeDamage`. |
| `Assets/Scenes/MLTrainingScene.unity` | New scene: floor, walls, 1 training area (can be duplicated 4–8× for faster training). |
| `Assets/Prefabs/FloatingPillEnemy.prefab` | Capsule mesh, Rigidbody, CapsuleCollider, `BehaviorParameters` (model assigned, Behavior Type = **Inference Only**), `DecisionRequester`, `RayPerceptionSensor3D`, `FloatingPillEnemy.cs`. |
| `ml-training/collector_config.yaml` | PPO training config (outside `Assets/` so Unity doesn't import it). |
| `ml-training/results/Collector_v1/Collector.onnx` | Output of training — to be copied/linked into `Assets/ML/Models/`. |
| `Assets/ML/Models/Collector.onnx` | Final model used at inference time. |

**Files to modify:** None of the existing player/enemy scripts need editing. The pill enemy is a *new* enemy type living alongside `EnemyFSM` / `EnemyBehaviorTree`.

---

## Implementation steps

### 1. Python environment (one-time, ~5 min)
```bash
cd /Users/almalittera/Documents/Unity/My_project
python3.10 -m venv ml-training/venv
source ml-training/venv/bin/activate
pip install mlagents==1.1.0   # matches Unity package 4.0.3
mlagents-learn --help          # sanity check
```

### 2. Build training scene (~30 min)
- New scene `Assets/Scenes/MLTrainingScene.unity`.
- Floor plane (20×20), 4 walls, 1 empty `TrainingArea` parent.
- Add agent (Capsule + Rigidbody, freeze Y position + X/Z rotation), target (small cube, isTrigger), 4 obstacle cubes.
- Add `BehaviorParameters` to agent: Behavior Name = `Collector`, Vector Observation size = 6, Continuous Actions = 2, Behavior Type = **Default** (for training).
- Add `DecisionRequester` (Decision Period = 5).
- Add `RayPerceptionSensor3D` Component: 7 rays, 60° angle, length 10, detects tags `Obstacle`, `Target`.
- Attach `CollectorAgent.cs` and `TrainingArea.cs`.
- Tag obstacles `Obstacle`, target `Target` (create tags in Tag Manager).

### 3. Author `CollectorAgent.cs`
Follow project style from `CLAUDE.md` §5: `[Header]`, `[SerializeField] private`, `_camelCase` for private fields, `#region` blocks. Override:
- `OnEpisodeBegin()` → call `_area.Reset(this)`.
- `CollectObservations(VectorSensor)` → add relative direction + local velocity.
- `OnActionReceived(ActionBuffers)` → read 2 continuous actions, `AddForce`, apply step penalty, optional shaped reward.
- `Heuristic(in ActionBuffers)` → WASD via `Input.GetAxis` for manual testing.
- Public methods `HitObstacle()` and `ReachedTarget()` called by trigger scripts; both set reward and `EndEpisode()`.

### 4. Author `collector_config.yaml`
Standard PPO config (batch_size 1024, buffer_size 10240, learning_rate 3e-4, beta 5e-3, ~500k steps, summary_freq 10000). Use the project's Unity behavior name `Collector`.

### 5. Train
```bash
source ml-training/venv/bin/activate
mlagents-learn ml-training/collector_config.yaml --run-id=Collector_v1
# Then press Play in Unity (training scene).
# Watch TensorBoard: tensorboard --logdir ml-training/results
```
Duplicate the `TrainingArea` 4–8 times in the scene before training for 4–8× faster wall-clock convergence (each area is an independent environment).

### 6. Use the trained model
- Copy `ml-training/results/Collector_v1/Collector.onnx` → `Assets/ML/Models/Collector.onnx`.
- In the training scene, switch `BehaviorParameters` → Behavior Type = **Inference Only**, assign the `.onnx`. Press Play — agent should now solve the task autonomously. ✅ This is enough to defend the homework.

### 7. FPS integration (~30 min)
- Build `FloatingPillEnemy.prefab` (capsule with same `BehaviorParameters` / `DecisionRequester` / `RayPerceptionSensor3D` setup, model assigned, **Inference Only**).
- `FloatingPillEnemy.cs` extends/wraps `CollectorAgent`: each `FixedUpdate`, set the internal `_target` reference to the Player's transform (found by tag `Player` once at `Start`). Apply a constant small upward force so it visibly "floats". Tag the player as `Target` for the ray sensor.
- On collision with Player, call `PlayerHealth.TakeDamage(int)` (see `Assets/PlayerHealth.cs`) and `EndEpisode()` (so internal counters reset, even though episodes don't really matter at inference).
- Drop the prefab into `Assets/Scenes/SampleScene.unity` somewhere reachable.

### 8. Defense prep — script for the demo
Have ready (assignment §"Atsiskaitymo metu"):
- **Show:** training scene running in Inference mode (agent reaches goal, avoids obstacles), then FPS scene where the floating pill chases the player.
- **Explain training:** PPO, ~Xk steps, TensorBoard reward curve screenshot.
- **Observations:** 6 vector floats + 7 rays (read straight from `CollectorAgent.cs`).
- **Actions:** 2 continuous floats → force on Rigidbody.
- **Rewards:** the table above.
- **Problem solved:** target-seeking + obstacle avoidance learned end-to-end, reused as a chasing enemy.

---

## Critical files / references to reuse

- `Packages/manifest.json` — confirms `com.unity.ml-agents 4.0.3` is installed; nothing to add.
- `Assets/PlayerHealth.cs` — existing `TakeDamage` API for the pill-on-contact damage hookup.
- `Assets/PlayerInputHandler.cs`, `PlayerMotor.cs` — physics conventions (`linearVelocity`, `AddForce`) to mirror in `CollectorAgent.cs` per `CLAUDE.md` §3.
- `Assets/EnemyFSM.cs` — pattern for how an enemy hooks into the existing scene (component layout, layers, damage). The pill enemy mimics this loosely.
- `CLAUDE.md` — style rules (`#region`, `[SerializeField] private`, `_field`, modern C# 12).

---

## Verification

1. **Training launches:** `mlagents-learn ml-training/collector_config.yaml --run-id=Collector_v1` prints "Listening on port 5004. Start training by pressing the Play button in the Unity Editor." Pressing Play in `MLTrainingScene` starts emitting cumulative reward in the terminal.
2. **Reward curve rises:** TensorBoard `Environment/Cumulative Reward` trends from ~−1 → ~+0.9 over training.
3. **Heuristic works first:** Before training, set Behavior Type = **Heuristic Only**, use WASD, confirm `HitObstacle`/`ReachedTarget` fire correctly. This catches reward/trigger bugs without spending training time.
4. **Inference works in training scene:** Set Behavior Type = **Inference Only** + assigned `.onnx`, press Play — agent navigates to target without manual input across many randomized resets.
5. **Inference works in FPS:** Drop `FloatingPillEnemy.prefab` into `SampleScene`, press Play, walk around — pill follows the player and avoids walls/obstacles. Take damage on contact.
6. **No regressions:** existing `EnemyFSM` mutants in `SampleScene` still patrol/chase as before (we added an enemy, didn't touch the old ones).

**Rollback:** delete `Assets/Scripts/ML/`, `Assets/Scenes/MLTrainingScene.unity`, `Assets/Prefabs/FloatingPillEnemy.prefab`, `Assets/ML/`, and the pill in `SampleScene`. ML-Agents package stays installed; no other files touched.
