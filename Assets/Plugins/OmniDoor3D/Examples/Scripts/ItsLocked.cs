namespace ABCodeworld.Examples
{
    using ABCodeworld.OmniDoor3D;
    using System.Collections;
    using UnityEngine;

    public class ItsLocked : MonoBehaviour
    {
        public OmniDoor3D door; // Door I want to monitor

        private bool flashing; // Spam control
        private SpriteRenderer spr;  // Locked sprite

        void Start()
        {
            door.DoorOpenFailed += OnDoorOpenFailed; // Subscribe to the door's event that fires when it attempts to open but is locked
            spr = GetComponent<SpriteRenderer>();
        }

        // All the door events have an OmniDoor3D parameter that is the door that fired the event. Useful on a per case basis. If listening to multiple doors, you might want to check it.
        // This time, I'm only listening to one door.
        private void OnDoorOpenFailed(OmniDoor3D lockedDoor)
        {
            if (!flashing)
                StartCoroutine(FlashLockedSprite());
        }

        // Flash on and off 3 times
        private IEnumerator FlashLockedSprite()
        {
            flashing = true;
            spr.enabled = true;
            yield return new WaitForSeconds(0.5f);
            spr.enabled = false;
            yield return new WaitForSeconds(0.5f);
            spr.enabled = true;
            yield return new WaitForSeconds(0.5f);
            spr.enabled = false;
            yield return new WaitForSeconds(0.5f);
            spr.enabled = true;
            yield return new WaitForSeconds(0.5f);
            spr.enabled = false;
            flashing = false;
        }
    }
}