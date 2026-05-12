using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace MLProject.ML
{
    /// <summary>
    /// Ragdoll handler for the floating pill enemy. Unlike the project's
    /// existing <c>SimpleRagdollHandler</c> (which needs a NavMeshAgent and
    /// hard-references EnemyFSM/EnemyBehaviorTree), this one shuts down the
    /// ML-Agents brain so the pill stops issuing physics forces, then frees
    /// the Rigidbody so it can tumble naturally under gravity.
    ///
    /// Hooks into the existing damage pipeline via the <c>IRagdollHandler</c>
    /// interface — <c>EnemyHealth.Die()</c> calls this when HP reaches 0.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PillRagdollHandler : MonoBehaviour, IRagdollHandler
    {
        #region Inspector

        [Header("Ragdoll Physics")]
        [Tooltip("Multiplier applied to the impact force from the damage source.")]
        [SerializeField] private float _forceMultiplier = 1.5f;

        [Tooltip("Random torque magnitude applied on death for visual tumble.")]
        [SerializeField] private float _torqueAmount = 8f;

        [Tooltip("Override Rigidbody linear damping when ragdolled (lower = more bouncy).")]
        [SerializeField] private float _ragdollLinearDamping = 0.2f;

        [Tooltip("Override Rigidbody angular damping when ragdolled.")]
        [SerializeField] private float _ragdollAngularDamping = 0.2f;

        #endregion

        #region State

        private bool _isRagdoll;
        private Rigidbody _rb;

        #endregion

        #region Unity

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        #endregion

        #region IRagdollHandler

        public void TriggerRagdoll(Vector3 impactForce, Vector3 impactPoint, Rigidbody hitBone = null)
        {
            if (_isRagdoll) return;
            _isRagdoll = true;

            // 1. Shut down the ML brain so the pill stops thinking & emitting forces.
            DisableIfPresent<BehaviorParameters>();
            DisableIfPresent<DecisionRequester>();
            DisableIfPresent<RayPerceptionSensorComponent3D>();
            DisableIfPresent<FloatingPillEnemy>();
            // Note: disabling FloatingPillEnemy disables its inherited CollectorAgent
            // ground-follow FixedUpdate too — the pill will no longer snap to the floor.

            // 2. Free the Rigidbody so it can fall and tumble.
            if (_rb != null)
            {
                _rb.constraints = RigidbodyConstraints.None;
                _rb.useGravity = true;
                _rb.linearDamping = _ragdollLinearDamping;
                _rb.angularDamping = _ragdollAngularDamping;

                // 3. Apply the impact force from the projectile.
                _rb.AddForceAtPosition(impactForce * _forceMultiplier, impactPoint, ForceMode.Impulse);

                // 4. Random tumble torque so the death has some visual flair.
                Vector3 randomTorque = new Vector3(
                    Random.Range(-_torqueAmount, _torqueAmount),
                    Random.Range(-_torqueAmount, _torqueAmount),
                    Random.Range(-_torqueAmount, _torqueAmount));
                _rb.AddTorque(randomTorque, ForceMode.Impulse);
            }
        }

        #endregion

        #region Pool reset

        /// <summary>
        /// Called by the pool manager on respawn — clears the ragdoll flag so the
        /// pill can be killed (and ragdoll) again on its next life.
        /// </summary>
        public void ResetRagdollState()
        {
            _isRagdoll = false;
        }

        #endregion

        #region Helpers

        private void DisableIfPresent<T>() where T : Behaviour
        {
            if (TryGetComponent<T>(out var comp))
            {
                comp.enabled = false;
            }
        }

        #endregion
    }
}
