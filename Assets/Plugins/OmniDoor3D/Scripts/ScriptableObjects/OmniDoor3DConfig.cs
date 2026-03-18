using UnityEngine;

namespace ABCodeworld.OmniDoor3D
{
    [CreateAssetMenu(fileName = "OmniDoor3DConfig", menuName = "Scriptable Objects/ABCodeworld/OmniDoor3D Config")]
    public class OmniDoor3DConfig : ScriptableObject
    {
        public float checkTime = 0.05f; // Frequency of checks for door state changes
        public OmniDoorDB[] DBList; // List of all databases
    }
}