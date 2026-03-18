using System;
using UnityEngine;

/// <summary>
/// A static system that allows GameObjects to emit "noises" that can be heard by enemies.
/// </summary>
public static class NoiseSystem
{
    /// <summary>
    /// Event fired when a noise is emitted.
    /// Vector3: Position of the noise.
    /// float: Radius of the noise (hearing range).
    /// </summary>
    public static event Action<Vector3, float> OnNoiseEmitted;

    /// <summary>
    /// Emits a noise at the specified position with a given radius.
    /// </summary>
    /// <param name="position">World position of the noise.</param>
    /// <param name="radius">How far the noise can be heard.</param>
    public static void EmitNoise(Vector3 position, float radius)
    {
        OnNoiseEmitted?.Invoke(position, radius);
    }
}
