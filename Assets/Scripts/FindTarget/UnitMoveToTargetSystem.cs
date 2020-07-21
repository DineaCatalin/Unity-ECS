using UnityEngine;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;

public class UnitMoveToTargetSystem : ComponentSystem {

    protected override void OnUpdate() {
        Entities.ForEach((Entity unitEntity, ref HasTarget hasTarget, ref Translation translation) => {
            if (World.Active.EntityManager.Exists(hasTarget.targetEntity)) {
                Translation targetTranslation = World.Active.EntityManager.GetComponentData<Translation>(hasTarget.targetEntity);

                float3 targetDir = math.normalize(targetTranslation.Value - translation.Value);
                float moveSpeed = 5f;
                translation.Value += targetDir * moveSpeed * Time.deltaTime;

                if (math.distance(translation.Value, targetTranslation.Value) < .2f) {
                    // Close to target, destroy it
                    PostUpdateCommands.DestroyEntity(hasTarget.targetEntity);
                    PostUpdateCommands.RemoveComponent(unitEntity, typeof(HasTarget));
                }
            } else {
                // Target Entity already destroyed
                PostUpdateCommands.RemoveComponent(unitEntity, typeof(HasTarget));
            }
        });
    }

}
