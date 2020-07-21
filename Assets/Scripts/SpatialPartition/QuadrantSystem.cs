using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using CodeMonkey.Utils;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Burst;

public struct QuadrantEntity : IComponentData
{
    public TypeEnum type;

    public enum TypeEnum
    {
        Unit,
        Target
    }
}

public struct QuadrantData
{
    public Entity entity;
    public float3 position;
    public QuadrantEntity quadrantEntity;
}

public class QuadrantSystem : ComponentSystem
{
    public static NativeMultiHashMap<int, QuadrantData> quadrantMap;

    private const int quadrantYMultiplier = 1000;
    private const int quadrantCellSize = 5;

    protected override void OnCreate()
    {
        base.OnCreate();

        quadrantMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        quadrantMap.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        EntityQuery entityQuery = GetEntityQuery(typeof(Translation), typeof(QuadrantEntity));

        quadrantMap.Clear();
        if(entityQuery.CalculateEntityCount() > quadrantMap.Capacity)
        {
            quadrantMap.Capacity = entityQuery.CalculateEntityCount();
        }

        // Create Job
        SetQuadrantDataHashMapJob job = new SetQuadrantDataHashMapJob
        {
            hashMap = quadrantMap.AsParallelWriter()
        };

        // Schedule and complete Job
        JobHandle jobHandle = JobForEachExtensions.Schedule(job, entityQuery);
        jobHandle.Complete();

        DebugDrawQuadrant(UtilsClass.GetMouseWorldPosition());

        int quadrantKey = GetKeyFromPosition(UtilsClass.GetMouseWorldPosition());
        int entityCount = GetEntityCountInQuadrant(quadrantKey);
        Debug.Log("There are " + entityCount + " in Quadrant " + quadrantKey);
    }

    private static int GetEntityCountInQuadrant(int quadrantKey)
    {
        QuadrantData quadrantData;
        NativeMultiHashMapIterator<int> iterator = new NativeMultiHashMapIterator<int>();
        int count = 0;

        // We have at least 1 value for our key
        // This is how you iterate over 
        if (quadrantMap.TryGetFirstValue(quadrantKey, out quadrantData, out iterator))
        {
            do
            {
                count++;

            } while (quadrantMap.TryGetNextValue(out quadrantData, ref iterator)); // Keep iterationg as long as there are values for this key
        }
        return count;
    }

    [BurstCompile]
    private struct SetQuadrantDataHashMapJob : IJobForEachWithEntity<Translation, QuadrantEntity>
    {
        public NativeMultiHashMap<int, QuadrantData>.ParallelWriter hashMap;

        public void Execute(Entity entity, int index, ref Translation translation, ref QuadrantEntity quadrantEntity)
        {
            // Add Entities in the HashMap based on their position in the world
            int hashKey = GetKeyFromPosition(translation.Value);
            hashMap.Add(hashKey, new QuadrantData
            {
                entity = entity,
                position = translation.Value,
                quadrantEntity = quadrantEntity
            });
        }
    }

    public static int GetKeyFromPosition(float3 position)
    {
        return (int)(math.floor(position.x / quadrantCellSize) + (quadrantYMultiplier * math.floor(position.y / quadrantCellSize)));
    }

    private static void DebugDrawQuadrant(float3 position)
    {
        Vector3 lowerLeft = new Vector3(math.floor(position.x / quadrantCellSize) * quadrantCellSize, math.floor(position.y / quadrantCellSize) * quadrantCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(1, 0) * quadrantCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(0, 1) * quadrantCellSize);
        Debug.DrawLine(lowerLeft + new Vector3(1, 0) * quadrantCellSize, lowerLeft + new Vector3(1, 1) * quadrantCellSize);
        Debug.DrawLine(lowerLeft + new Vector3(0, 1) * quadrantCellSize, lowerLeft + new Vector3(1, 1) * quadrantCellSize);
        Debug.Log(GetKeyFromPosition(position) + "  " + position);
    }
}
