using UnityEngine;

namespace MLProject.ML
{
    /// <summary>
    /// Marks a GameObject as an obstacle. When a CollectorAgent collides with it,
    /// the agent is penalized and the episode ends.
    ///
    /// Setup: GameObject needs a non-trigger Collider AND the tag "Obstacle" so the
    /// RayPerceptionSensor3D on the agent can detect it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ArenaObstacle : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            var agent = collision.collider.GetComponentInParent<CollectorAgent>();
            if (agent != null)
            {
                agent.HitObstacle();
            }
        }
    }
}
