using UnityEngine;

namespace ABCodeworld.OmniDoor3D
{
    public class OmniDoor3DController : MonoBehaviour
    {
        public delegate void OpenDoorEvt();
        public OpenDoorEvt OpenDoor; // Make your script invoke this event when you want to open doors controlled by this controller.
        public delegate void CloseDoorEvt();
        public CloseDoorEvt CloseDoor; // Make your script invoke this event when you want to close doors controlled by this controller.
        public delegate void ToggleDoorEvt();
        public ToggleDoorEvt ToggleDoor; // Make your script invoke this event when you want to toggle doors (if closed, open; if open, close) controlled by this controller.
        public delegate void UnlockDoorEvt();
        public UnlockDoorEvt UnlockDoor; // Make your script invoke this event when you want to unlock doors controlled by this controller.
        public delegate void LockDoorEvt();
        public LockDoorEvt LockDoor; // Make your script invoke this event when you want to lock doors controlled by this controller.
    }
}