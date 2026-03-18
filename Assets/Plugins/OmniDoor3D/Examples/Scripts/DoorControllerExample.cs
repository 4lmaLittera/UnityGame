namespace ABCodeworld.Examples
{
    using ABCodeworld.OmniDoor3D;
    using UnityEngine;

    public class DoorControllerExample : MonoBehaviour
    {
        [HideInInspector, SerializeField] public SpriteRenderer spaceHint; // This sprite will be shown as a hint

        private OmniDoor3DController dc; // Door controller I want to use

        private void Awake()
        {
            dc = GetComponent<OmniDoor3DController>();
        }

        // These methods are called from the buttons in this script's component foldout in the Unity editor's Inspector window. You can directly control doors from your own code using event invocations
        // like below. See ExampleInspector.cs for how the button are set up. You do not need buttons, they are for convenience in this example scenario.
        // Without buttons you can just invoke the events whenever it's appropriate in your own logic.
        public void OpenDoor()
        {
            dc.OpenDoor?.Invoke();
        }

        public void CloseDoor()
        {
            dc.CloseDoor?.Invoke();
        }

        public void LockDoor()
        {
            dc.LockDoor?.Invoke();
        }

        public void UnlockDoor()
        {
            dc.UnlockDoor?.Invoke();
        }

        // These methods are part of a simple interaction system where the player can press a key to open and close the controlled door(s). (See PlayerOrbital.cs for the input code)
        // I do not recommend doing your UI as sprites in world space - this is a very specific use case made for the example scene.

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            spaceHint.enabled = true; // Show UI prompt
            other.GetComponent<PlayerOrbital>().doorController = dc; // Give a controller ref to the player script so the input code can use it
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            spaceHint.enabled = false; // Hide UI prompt
            other.GetComponent<PlayerOrbital>().doorController = null; // Clear ref
        }
    }
}