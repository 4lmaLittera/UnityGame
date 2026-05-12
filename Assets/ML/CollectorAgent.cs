using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MLProject.ML
{
    /// <summary>
    /// ML-Agents agent that learns to move toward a target while avoiding obstacles.
    /// Used in the training scene (target = static cube) AND in the FPS scene
    /// (target = player) via the <see cref="FloatingPillEnemy"/> subclass.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CollectorAgent : Agent
    {
        #region Inspector

        [Header("Movement")]
        [SerializeField] private float _moveForce = 12f;
        [SerializeField] private float _maxSpeed = 6f;

        [Header("Reward Shaping")]
        [Tooltip("Reward granted when the agent enters the Target trigger.")]
        [SerializeField] private float _reachReward = 10.0f;
        [Tooltip("Penalty applied (and episode ended) when the agent hits an obstacle.")]
        [SerializeField] private float _obstaclePenalty = -1.0f;
        [Tooltip("Penalty applied (and episode ended) when the agent falls off the arena.")]
        [SerializeField] private float _fallPenalty = -0.5f;
        [Tooltip("Positive reward proportional to closing distance to target each step.")]
        [SerializeField] private float _proximityScale = 0.5f;
        [Tooltip("Y position below which the agent is considered fallen.")]
        [SerializeField] private float _fallYThreshold = -2f;

        [Header("Episode")]
        [Tooltip("Max decisions per episode. After this many steps the episode auto-resets. Required > 0 to avoid stuck-forever episodes.")]
        [SerializeField] private int _maxStep = 600;

        [Header("Refs")]
        [Tooltip("The transform the agent should reach. Set by TrainingArea on reset, or by FloatingPillEnemy at runtime.")]
        [SerializeField] protected Transform _target;

        #endregion

        #region Internal state

        private Rigidbody _rigidbody;
        private float _prevDistance;

        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        #endregion

        #region Unity / Agent lifecycle

        public override void Initialize()
        {
            _rigidbody = GetComponent<Rigidbody>();

            // Override base Agent's serialized MaxStep so we don't have to edit
            // it in every prefab/scene instance. 0 means "no timeout" which is
            // exactly what we DON'T want — stuck agents would never reset.
            if (_maxStep > 0)
            {
                MaxStep = _maxStep;
            }

            // Lock ALL rotation. With actions in world XZ and observations also in
            // world frame, the agent's facing is irrelevant — freezing it keeps the
            // policy rotation-invariant and removes a useless degree of freedom.
            // Use |= so any position-freezing flags set on the prefab (e.g. FreezePositionY
            // on the FPS floating pill enemy) are preserved.
            _rigidbody.constraints |= RigidbodyConstraints.FreezeRotation;
        }

        public override void OnEpisodeBegin()
        {
            // TrainingArea (if present) will reposition agent/target/obstacles.
            // At inference time (FPS), there is no TrainingArea — episode reset is a no-op there.
            var area = GetComponentInParent<TrainingArea>();
            if (area != null)
            {
                area.ResetArea(this);
            }

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _prevDistance = TargetDistance();
        }

        #endregion

        #region Observations

        public override void CollectObservations(VectorSensor sensor)
        {
            // 3 floats: normalized direction to target (in agent local space)
            // 3 floats: agent local velocity
            // Total vector observation size = 6 (must match BehaviorParameters in Inspector).

            if (_target == null)
            {
                sensor.AddObservation(Vector3.zero);
                sensor.AddObservation(transform.InverseTransformDirection(_rigidbody.linearVelocity));
                return;
            }

            Vector3 toTargetWorld = _target.position - transform.position;
            Vector3 toTargetLocal = transform.InverseTransformDirection(toTargetWorld.normalized);
            sensor.AddObservation(toTargetLocal);

            Vector3 localVel = transform.InverseTransformDirection(_rigidbody.linearVelocity);
            sensor.AddObservation(localVel);

            // RayPerceptionSensor3D (added as a separate Component in the Inspector) provides
            // additional observations automatically — no code needed here.
        }

        #endregion

        #region Actions

        public override void OnActionReceived(ActionBuffers actions)
        {
            // Continuous actions, size = 2: [moveX, moveZ] in [-1, 1].
            float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
            float moveZ = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

            Vector3 force = new Vector3(moveX, 0f, moveZ) * _moveForce;
            _rigidbody.AddForce(force, ForceMode.Acceleration);

            // Clamp horizontal speed so the policy can't just keep accelerating into walls.
            Vector3 horizVel = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
            if (horizVel.magnitude > _maxSpeed)
            {
                horizVel = horizVel.normalized * _maxSpeed;
                _rigidbody.linearVelocity = new Vector3(horizVel.x, _rigidbody.linearVelocity.y, horizVel.z);
            }

            // ---- Rewards ----
            // Time penalty: 1/MaxStep per step => total -1 if it never reaches the target.
            if (MaxStep > 0)
            {
                AddReward(-1f / MaxStep);
            }

            // Proximity shaping (tiny — keeps it from drowning out the sparse +1).
            if (_target != null)
            {
                float dist = TargetDistance();
                float delta = _prevDistance - dist;
                AddReward(delta * _proximityScale);
                _prevDistance = dist;
            }

            // Fall-off-arena check.
            if (transform.position.y < _fallYThreshold)
            {
                AddReward(_fallPenalty);
                EndEpisode();
            }
        }

        #endregion

        #region Heuristic (manual control for testing)

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            // Project uses the new Input System (see PlayerInputHandler).
            // Read Keyboard.current directly for the Heuristic test — no need to
            // wire up an InputAction asset just for manual driving.
            var ca = actionsOut.ContinuousActions;
            var kb = Keyboard.current;
            if (kb == null)
            {
                ca[0] = 0f;
                ca[1] = 0f;
                return;
            }

            float x = 0f, z = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) z -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) z += 1f;

            ca[0] = x;
            ca[1] = z;
        }

        #endregion

        #region Public hooks called by trigger scripts

        public void ReachedTarget()
        {
            AddReward(_reachReward);
            EndEpisode();
        }

        public void HitObstacle()
        {
            AddReward(_obstaclePenalty);
            EndEpisode();
        }

        #endregion

        #region Helpers

        private float TargetDistance()
        {
            if (_target == null) return 0f;
            return Vector3.Distance(transform.position, _target.position);
        }

        #endregion
    }
}
