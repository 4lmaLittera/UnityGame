# Implementing Enemy Melee Attacks

This guide explains how to set up an enemy so that a specific part of their body (like a hand, foot, or weapon) deals damage to the player only during specific frames of an attack animation.

## Step 1: Set up the Damage Trigger Hitbox

Instead of spawning a collider via code, we parent a permanent (but disabled) collider to the specific bone that should deal damage.

1.  **Locate the Bone**: In the Unity Hierarchy, expand your enemy prefab down to its skeletal rig (e.g., `EnemyRoot/Hips/Spine/Chest/Shoulder.R/Arm.R/Hand.R`).
2.  **Create the Hitbox**: Right-click the chosen bone (e.g., `Hand.R`) and select **Create Empty**. Name it `AttackHitbox`.
3.  **Add Components**:
    *   Add a **Collider** component (Box, Sphere, or Capsule) to the `AttackHitbox`.
    *   Scale and position the collider so it covers the hand/weapon.
    *   **CRITICAL**: Check the **Is Trigger** box on the Collider.
    *   **CRITICAL**: Uncheck the box at the top left of the Inspector to **Disable** the GameObject (or disable the Collider component itself) so it doesn't deal damage by default.
    *   Add the `DamageTrigger.cs` script to this GameObject.
4.  **Configure DamageTrigger**:
    *   Set the `Damage` amount.
    *   Set the `Knockback Force` (try starting with 10-15).
    *   The `Hit Cooldown` prevents hitting the player 60 times a second. 0.5s is usually good.

## Step 2: Set up the Animation Helper

1.  **Select the Enemy Root**: Click on the main parent GameObject of your enemy (the one that has the `Animator` component).
2.  **Add Script**: Add the `AttackAnimationHelper.cs` script to this root object.
3.  **Assign Reference**: Drag the `AttackHitbox` GameObject you created in Step 1 into the `Attack Collider` slot of the `AttackAnimationHelper` component.

## Step 3: Add Animation Events

Now we tell the animation exactly when to turn the hitbox on and off.

1.  **Open Animation Window**: Go to `Window -> Animation -> Animation` in the top menu.
2.  **Select Animation**: With your enemy root selected in the Hierarchy, choose your Attack animation from the dropdown in the Animation window.
3.  **Find the Start Frame**: Scrub through the timeline and find the exact frame where the "swing" begins and should start dealing damage.
4.  **Add Start Event**:
    *   Click the **Add Event** button (a small white rectangle with a plus sign, just below the timeline numbers).
    *   In the Inspector, look for the "Function" dropdown and select `StartAttack()`.
5.  **Find the End Frame**: Scrub forward to the frame where the swing is finished and should stop dealing damage.
6.  **Add End Event**:
    *   Click the **Add Event** button again.
    *   In the Inspector, select `EndAttack()`.

## Summary of How it Works
When the enemy plays the attack animation, it hits the first event marker. That calls `StartAttack()`, which turns on the `DamageTrigger` collider attached to the enemy's hand. If that hand sweeps through the player (who must be tagged "Player"), `OnTriggerEnter` fires, dealing damage and pushing the player back. When the animation hits the second event marker, `EndAttack()` is called, turning the collider off again.