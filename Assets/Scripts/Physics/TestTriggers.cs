using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Burst;
using Unity.Physics.Systems;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;

public class TestTriggers : JobComponentSystem
{
    private BuildPhysicsWorld buildPhysicsWorld;
    private StepPhysicsWorld  stepPhysicsWorld;

    [BurstCompile]
    private struct TriggerJob : ITriggerEventsJob
    {
        public ComponentDataFromEntity<PhysicsVelocity> physicsVelocityEntities;
        public float3 randomLinearVelocity;

        public void Execute(TriggerEvent triggerEvent)
        {
            if(physicsVelocityEntities.HasComponent(triggerEvent.Entities.EntityA))
            {
                PhysicsVelocity velocity = physicsVelocityEntities[triggerEvent.Entities.EntityA];
                velocity.Linear.x = randomLinearVelocity.x;
                velocity.Linear.y = randomLinearVelocity.y;
                velocity.Linear.z = randomLinearVelocity.z;
                physicsVelocityEntities[triggerEvent.Entities.EntityA] = velocity;
            }
            
            if(physicsVelocityEntities.HasComponent(triggerEvent.Entities.EntityB))
            {
                PhysicsVelocity velocity = physicsVelocityEntities[triggerEvent.Entities.EntityA];
                velocity.Linear.x = randomLinearVelocity.x;
                velocity.Linear.y = randomLinearVelocity.y;
                velocity.Linear.z = randomLinearVelocity.z;
                physicsVelocityEntities[triggerEvent.Entities.EntityB] = velocity;
            }
        }
    }

    protected override void OnCreate()
    {
        base.OnCreate();

        buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float3 randomDir = new float3(
            UnityEngine.Random.Range(-10f, 10f),
            UnityEngine.Random.Range(  2f, 10f),
            UnityEngine.Random.Range(-10f, 10f)
        );

        TriggerJob job = new TriggerJob
        {
            physicsVelocityEntities = GetComponentDataFromEntity<PhysicsVelocity>(),
            randomLinearVelocity = randomDir
        };

        JobHandle handle = job.Schedule(stepPhysicsWorld.Simulation, ref buildPhysicsWorld.PhysicsWorld, inputDeps);

        return handle;
    }
}
