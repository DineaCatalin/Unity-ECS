using Unity.Entities;
using Unity.Physics;
using UnityEngine;

public class BallJumpSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref PhysicsVelocity velocity) =>
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                velocity.Linear.y = 5f;
            }
        });
    }
}
