using UnityEngine;

namespace MLProject.ML
{
    /// <summary>
    /// One self-contained training arena. Each TrainingArea owns one agent, one target,
    /// and N obstacles. On <see cref="ResetArea"/> (called by the agent's OnEpisodeBegin),
    /// it randomizes positions inside the arena bounds so the agent learns a general policy
    /// rather than memorizing one layout.
    ///
    /// Duplicate this GameObject in the scene (4-8 copies) to train in parallel.
    /// </summary>
    public class TrainingArea : MonoBehaviour
    {
        #region Inspector

        [Header("Arena Bounds (local space, centered on this transform)")]
        [SerializeField] private Vector2 _arenaSize = new Vector2(16f, 16f);
        [SerializeField] private float _spawnHeight = 0.6f;
        [SerializeField] private float _minSeparation = 2f;

        [Header("Refs")]
        [SerializeField] private Transform _target;
        [SerializeField] private Transform[] _obstacles;

        #endregion

        #region Public API

        public void ResetArea(CollectorAgent agent)
        {
            // Place target first.
            Vector3 targetPos = RandomLocalPosition();
            if (_target != null)
            {
                _target.localPosition = targetPos;
            }

            // Place obstacles, avoiding overlap with target.
            if (_obstacles != null)
            {
                for (int i = 0; i < _obstacles.Length; i++)
                {
                    if (_obstacles[i] == null) continue;

                    Vector3 candidate = SamplePositionAwayFrom(targetPos);
                    _obstacles[i].localPosition = candidate;
                }
            }

            // Place the agent, avoiding overlap with target & obstacles.
            Vector3 agentPos = SampleAgentPosition(targetPos);
            agent.transform.localPosition = agentPos;

            // Fixed facing (rotation is frozen on the rigidbody anyway).
            // Removing the random Y rotation makes ray observations align with
            // the world-frame action space, which is what the policy expects.
            agent.transform.localRotation = Quaternion.identity;
        }

        #endregion

        #region Sampling

        private Vector3 RandomLocalPosition()
        {
            float x = Random.Range(-_arenaSize.x * 0.5f, _arenaSize.x * 0.5f);
            float z = Random.Range(-_arenaSize.y * 0.5f, _arenaSize.y * 0.5f);
            return new Vector3(x, _spawnHeight, z);
        }

        private Vector3 SamplePositionAwayFrom(Vector3 other)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector3 p = RandomLocalPosition();
                if (Vector3.Distance(p, other) >= _minSeparation) return p;
            }
            return RandomLocalPosition();
        }

        private Vector3 SampleAgentPosition(Vector3 targetPos)
        {
            for (int attempt = 0; attempt < 15; attempt++)
            {
                Vector3 p = RandomLocalPosition();
                if (Vector3.Distance(p, targetPos) < _minSeparation * 1.5f) continue;

                bool clash = false;
                if (_obstacles != null)
                {
                    foreach (var o in _obstacles)
                    {
                        if (o == null) continue;
                        if (Vector3.Distance(p, o.localPosition) < _minSeparation)
                        {
                            clash = true; break;
                        }
                    }
                }
                if (!clash) return p;
            }
            return RandomLocalPosition();
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.25f);
            Vector3 center = transform.position + Vector3.up * _spawnHeight;
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(_arenaSize.x, 0.1f, _arenaSize.y));
        }

        #endregion
    }
}
