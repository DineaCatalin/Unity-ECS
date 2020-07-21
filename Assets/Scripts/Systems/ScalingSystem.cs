using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;

public class ScalingSystem : ComponentSystem
{
    float minScale = 0.5f;
    float maxScale = 1f;
    float increment = 0.007f;

    protected override void OnUpdate()
    {
        Entities.ForEach((ref Scale scale) =>
        {
            // Increment is negative if we are above max scale
            if (scale.Value >= maxScale)
                increment = -Mathf.Abs(increment);

            // Increment is positive if we are below min scale
            else if (scale.Value <= minScale)
                increment = Mathf.Abs(increment);

            scale.Value += increment;
        });
    }
}
