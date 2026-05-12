using UnityEngine;

namespace MLProject.ML
{
    /// <summary>
    /// Marks a GameObject as the agent's target. When a CollectorAgent enters
    /// the trigger volume, the agent is rewarded and the episode ends.
    ///
    /// Setup: Collider with isTrigger=true, tag "Target" so the
    /// RayPerceptionSensor3D on the agent can detect it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CollectorTarget : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var agent = other.GetComponentInParent<CollectorAgent>();
            if (agent != null)
            {
                agent.ReachedTarget();
            }
        }
    }
}
