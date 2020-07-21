using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

public class FindTargetJobSystem : JobComponentSystem
{
    private struct EntityWithPosition
    {
        public Entity entity;
        public float3 position;
    }

    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        base.OnCreate();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityQuery query = GetEntityQuery(typeof(Target), ComponentType.ReadOnly<Translation>());

        NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
        NativeArray<Translation> translations = query.ToComponentDataArray<Translation>(Allocator.TempJob);

        NativeArray<EntityWithPosition> entityWithPositions = new NativeArray<EntityWithPosition>(entities.Length, Allocator.TempJob);

        for (int i = 0; i < entityWithPositions.Length; i++)
        {
            entityWithPositions[i] = new EntityWithPosition
            {
                entity = entities[i],
                position = translations[i].Value
            };
        }

        // Disponse native arrays
        entities.Dispose();
        translations.Dispose();

        EntityQuery unitQuery = GetEntityQuery(typeof(Unit), ComponentType.Exclude<HasTarget>());
        NativeArray<Entity> closestTargets = new NativeArray<Entity>(unitQuery.CalculateEntityCount(), Allocator.TempJob);

        //FindTargetBurstJob findTargetBurstJob = new FindTargetBurstJob
        //{
        //    targetArray = entityWithPositions,
        //    closestTargets = closestTargets
        //};
        //JobHandle handle = findTargetBurstJob.Schedule(this, inputDeps);

        FindTargetQuadrantSystemJob findTargetQuadrantSystemJob = new FindTargetQuadrantSystemJob
        {
            quadrantMap = QuadrantSystem.quadrantMap,
            closestTargets = closestTargets
        };

        JobHandle handle = findTargetQuadrantSystemJob.Schedule(this, inputDeps);

        AddComponentJob addComponentJob = new AddComponentJob
        {
            closestTargets = closestTargets,
            entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        };

        handle = addComponentJob.Schedule(this, handle);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(handle);

        return handle;
    }

    [RequireComponentTag(typeof(Target))]
    [BurstCompile]
    private struct FillArrayEntityWithPositionJob : IJobForEachWithEntity<Translation>
    {
        public NativeArray<EntityWithPosition> targetArray;

        public void Execute(Entity entity, int index, ref Translation translation)
        {
            targetArray[index] = new EntityWithPosition
            {
                entity = entity,
                position = translation.Value
            };
        }
    }

    [RequireComponentTag(typeof(Unit))]
    [ExcludeComponent(typeof(HasTarget))]
    private struct AddComponentJob : IJobForEachWithEntity<Translation>
    {
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> closestTargets;
        public EntityCommandBuffer.Concurrent entityCommandBuffer;

        public void Execute(Entity entity, int index, ref Translation translation)
        {
            if(closestTargets[index] != Entity.Null)
            {
                entityCommandBuffer.AddComponent(index, entity, new HasTarget { targetEntity = closestTargets[index] });
            }
        }
    }

   
    private struct FindTargetQuadrantSystemJob : IJobForEachWithEntity<Translation, QuadrantEntity>
    {
        [ReadOnly] public NativeMultiHashMap<int, QuadrantData> quadrantMap;
        public NativeArray<Entity> closestTargets;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref QuadrantEntity quadrantEntity)
        {
            float3 unitPosition = translation.Value;
            Entity closestTargetEntity = Entity.Null;
            float3 closestTargetPosition = float3.zero;

            int key = QuadrantSystem.GetKeyFromPosition(unitPosition);

            NativeMultiHashMapIterator<int> iterator = new NativeMultiHashMapIterator<int>();
            QuadrantData quadrantData;

            if(quadrantMap.TryGetFirstValue(key, out quadrantData, out iterator))
            {
                do
                {
                    if(quadrantEntity.type != quadrantData.quadrantEntity.type)
                    {
                        if (closestTargetEntity == Entity.Null)
                        {
                            // No target
                            closestTargetEntity = quadrantData.entity;
                            closestTargetPosition = math.distance(unitPosition, quadrantData.position);
                        }
                        else
                        {
                            if (math.distance(unitPosition, quadrantData.position) < math.distance(unitPosition, closestTargetPosition))
                            {
                                // This target is closer
                                closestTargetEntity = entity;
                                closestTargetPosition = math.distancesq(unitPosition, quadrantData.position);
                            }
                        }
                    }

                } while (quadrantMap.TryGetNextValue(out quadrantData, ref iterator));
            }

            closestTargets[index] = closestTargetEntity;
        }
    }

    [RequireComponentTag(typeof(Unit))]
    [ExcludeComponent(typeof(HasTarget))]
    [Unity.Burst.BurstCompile]
    private struct FindTargetBurstJob : IJobForEachWithEntity<Translation>
    {
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<EntityWithPosition> targetArray;
        public NativeArray<Entity> closestTargets;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation)
        {
            float3 unitPosition = translation.Value;
            Entity closestTargetEntity = Entity.Null;
            float3 closestTargetPosition = float3.zero;

            for (int i = 0; i < targetArray.Length; i++)
            {
                EntityWithPosition entityWithPos = targetArray[i];

                if (closestTargetEntity == Entity.Null)
                {
                    // No target
                    closestTargetEntity = entityWithPos.entity;
                    closestTargetPosition = entityWithPos.position;
                }
                else
                {
                    if (math.distance(unitPosition, entityWithPos.position) < math.distance(unitPosition, closestTargetPosition))
                    {
                        // This target is closer
                        closestTargetEntity = entityWithPos.entity;
                        closestTargetPosition = entityWithPos.position;
                    }
                }
            }

            closestTargets[index] = closestTargetEntity;
        }
    }

   
}
