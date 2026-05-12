using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace MLProject.ML
{
    /// <summary>
    /// FPS-side specialization of <see cref="CollectorAgent"/>. Uses the SAME trained
    /// policy (.onnx) but the target is the player Transform (looked up by tag at Start),
    /// and the pill gets a small constant upward force so it "floats" instead of skidding
    /// on the ground. On contact with the player it deals damage via PlayerHealth.TakeDamage.
    ///
    /// Behavior Parameters component on this prefab should be set to Behavior Type =
    /// Inference Only with the trained Collector.onnx model assigned.
    ///
    /// Implements <see cref="IPoolableEnemy"/> so the EnemyPoolManager can recycle
    /// dead pills back into upright, fully-functional state.
    /// </summary>
    public class FloatingPillEnemy : CollectorAgent, IPoolableEnemy
    {
        #region Inspector

        [Header("Floating Pill")]
        [Tooltip("Tag used to locate the player at Start.")]
        [SerializeField] private string _playerTag = "Player";
        [Tooltip("Legacy: not used when ground-following is active. Kept for backward compat.")]
        [SerializeField] private float _floatForce = 0f;
        [Tooltip("Damage dealt to the player on contact.")]
        [SerializeField] private float _contactDamage = 10f;
        [Tooltip("Seconds between contact-damage hits, to avoid one-frame burst kills.")]
        [SerializeField] private float _damageCooldown = 0.6f;

        [Header("Ground Following")]
        [Tooltip("Pill hovers this many units above whatever surface is below it.")]
        [SerializeField] private float _hoverHeight = 0.6f;
        [Tooltip("Maximum drop distance below pill before we consider ground 'lost'.")]
        [SerializeField] private float _groundRayDistance = 50f;
        [Tooltip("Ray cast starts this much above the pill (clears any overlap with self).")]
        [SerializeField] private float _groundRayUpOffset = 3f;

        #endregion

        #region Internal state

        private Rigidbody _rb;
        private float _nextDamageTime;
        private readonly RaycastHit[] _groundHits = new RaycastHit[8];

        // Cached at Awake so OnSpawn can restore the prefab's damping after ragdoll.
        private float _originalLinearDamping = 1.5f;
        private float _originalAngularDamping = 5f;

        #endregion

        #region Lifecycle

        public override void Initialize()
        {
            base.Initialize();
            _rb = GetComponent<Rigidbody>();

            // Cache prefab damping so we can restore it after ragdoll on respawn.
            if (_rb != null)
            {
                _originalLinearDamping = _rb.linearDamping;
                _originalAngularDamping = _rb.angularDamping;
            }

            // Defensive spawn state.
            ApplyAliveState();

            // Immediate ground snap so the very first rendered frame is at the
            // correct altitude instead of waiting one FixedUpdate.
            SnapToGround();

            // Locate the player and use it as the agent's target.
            var playerGO = GameObject.FindGameObjectWithTag(_playerTag);
            if (playerGO != null)
            {
                Target = playerGO.transform;
            }
            else
            {
                Debug.LogWarning($"[FloatingPillEnemy] No GameObject tagged '{_playerTag}' found. Agent will have no target.");
            }
        }

        private void FixedUpdate()
        {
            SnapToGround();

            // Optional legacy hover-force support (only applied if explicitly set).
            if (_rb != null && _floatForce > 0.001f)
            {
                _rb.AddForce(Vector3.up * _floatForce, ForceMode.Acceleration);
            }
        }

        private void SnapToGround()
        {
            // Ground-follow: pill hovers a fixed distance above whatever surface
            // is below. Works on uneven terrain. Physics XZ motion is handled by
            // the learned policy (CollectorAgent.OnActionReceived); we just
            // override Y position each tick.
            Vector3 origin = transform.position + Vector3.up * _groundRayUpOffset;
            int n = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                _groundHits,
                _groundRayDistance + _groundRayUpOffset,
                ~0,
                QueryTriggerInteraction.Ignore);

            float bestY = float.NegativeInfinity;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                var h = _groundHits[i];
                // Skip self (any collider under our own root).
                if (h.collider.transform.root == transform.root) continue;
                // Don't stand on the player's head — keep falling past them.
                if (h.collider.CompareTag(_playerTag)) continue;
                if (h.point.y > bestY)
                {
                    bestY = h.point.y;
                    found = true;
                }
            }

            if (found)
            {
                Vector3 p = transform.position;
                p.y = bestY + _hoverHeight;
                transform.position = p;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryDamagePlayer(collision.collider);
        }

        private void OnCollisionStay(Collision collision)
        {
            TryDamagePlayer(collision.collider);
        }

        #endregion

        #region IPoolableEnemy — called by EnemyPoolManager

        /// <summary>
        /// Reset everything the ragdoll handler shut down. Agent.Initialize() only
        /// runs once per component lifetime, so we re-do its work explicitly here
        /// every time the pool spawns this instance.
        /// </summary>
        public void OnSpawn()
        {
            // 1. Reset the ragdoll handler so the next death can ragdoll again.
            if (TryGetComponent<PillRagdollHandler>(out var ragdoll))
            {
                ragdoll.ResetRagdollState();
            }

            // 2. Re-enable the ML brain components the ragdoll handler disabled.
            //    Must happen BEFORE ApplyAliveState in case any of them touch the
            //    Rigidbody on enable.
            EnableIfPresent<BehaviorParameters>();
            EnableIfPresent<DecisionRequester>();
            EnableIfPresent<RayPerceptionSensorComponent3D>();
            this.enabled = true;

            // 3. Reset physics + transform state.
            ApplyAliveState();

            // 4. Reset damage cooldown.
            _nextDamageTime = 0f;

            // 5. Re-acquire the player as target (in case it changed between spawns).
            var playerGO = GameObject.FindGameObjectWithTag(_playerTag);
            if (playerGO != null)
            {
                Target = playerGO.transform;
            }

            // 6. Snap to ground so the first rendered frame is at correct altitude.
            SnapToGround();
        }

        public void OnDespawn()
        {
            // Nothing to do — ragdoll handler already shut things down. Pool will
            // SetActive(false) immediately after this call.
        }

        /// <summary>
        /// Force the pill into "alive and well" state: upright, no velocity, no
        /// gravity, position-Y frozen, all rotation frozen, prefab damping restored.
        /// Sets BOTH transform.rotation AND Rigidbody.rotation because setting only
        /// transform.rotation can be silently overwritten by the physics system on
        /// the next FixedUpdate.
        /// </summary>
        private void ApplyAliveState()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody>();

            transform.rotation = Quaternion.identity;
            if (_rb != null)
            {
                _rb.rotation = Quaternion.identity;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.useGravity = false;
                _rb.linearDamping = _originalLinearDamping;
                _rb.angularDamping = _originalAngularDamping;
                _rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            }
        }

        private void EnableIfPresent<T>() where T : Behaviour
        {
            if (TryGetComponent<T>(out var c)) c.enabled = true;
        }

        #endregion

        #region Damage

        private void TryDamagePlayer(Collider other)
        {
            if (Time.time < _nextDamageTime) return;
            if (!other.CompareTag(_playerTag)) return;

            // PlayerHealth is on the player root; CompareTag handles that directly.
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health == null) return;

            Vector3 impact = (other.transform.position - transform.position).normalized * 4f;
            health.TakeDamage(_contactDamage, impact);
            _nextDamageTime = Time.time + _damageCooldown;
        }

        #endregion
    }
}
