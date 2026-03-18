namespace ABCodeworld.Examples
{
    using ABCodeworld.OmniDoor3D;
    using UnityEngine;

    public class PressureSwitch : MonoBehaviour
    {
        private OmniDoor3DController dc; // Door controller I want to use

        private void Awake()
        {
            dc = GetComponent<OmniDoor3DController>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            dc.OpenDoor?.Invoke(); // Open the door when player enters the trigger
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            dc.CloseDoor?.Invoke(); // Close the door when player exits the trigger
        }
    }
}