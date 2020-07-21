using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public class MovementSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref Translation translation, ref MoveSpeedComponent moveSpeedComponent) =>
        {
            //Debug.Log("Before moving " + translation.Value.y);
            translation.Value.y += moveSpeedComponent.m_Speed * Time.deltaTime;
            //Debug.Log("After  moving " + translation.Value.y);
            if (translation.Value.y > 6f)
                moveSpeedComponent.m_Speed = -math.abs(moveSpeedComponent.m_Speed);

            if (translation.Value.y < -5f)
                moveSpeedComponent.m_Speed = math.abs(moveSpeedComponent.m_Speed);


        });
    }
}
