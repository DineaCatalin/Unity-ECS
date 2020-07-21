using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;

[UpdateAfter(typeof(SpriteSheetAnimation_Animate))]
[DisableAutoCreation]
public class SpriteSheetRenderer : ComponentSystem
{
    MaterialPropertyBlock propertyBlock;

    Mesh mesh;
    Material material;

    int shaderPropertyID;

    private struct RenderData
    {
        public Entity entity;
        public float3 position;
        public Vector4 uv;
        public Matrix4x4 matrix;
    }

    [BurstCompile]
    private struct CullAndSortJob : IJobForEachWithEntity<Translation, SpriteSheetAnimation_Data>
    {
        public float yTop_1;
        public float yTop_2;
        public float yBottom;

        // Create the Queues for our RenderData so that we can work on a chunk of the total entities
        // and so that we can work in paralell
        public NativeQueue<RenderData>.ParallelWriter nativeQueue_1;
        public NativeQueue<RenderData>.ParallelWriter nativeQueue_2;

        public void Execute(Entity entity, int index, ref Translation translation, ref SpriteSheetAnimation_Data animData)
        {
            float positionY = translation.Value.y;

            if (positionY > yBottom && positionY < yTop_1)
            {
                RenderData renderData = new RenderData
                {
                    entity = entity,
                    position = translation.Value,
                    uv = animData.uv,
                    matrix = animData.matrix
                };

                if (positionY < yTop_2)
                    nativeQueue_2.Enqueue(renderData);
                else
                    nativeQueue_1.Enqueue(renderData);
            }
        }
    }

    [BurstCompile]
    private struct NativeQueueToArrayJob : IJob
    {
        public NativeArray<RenderData> array;
        public NativeQueue<RenderData> queue;

        public void Execute()
        {
            int index = 0;
            RenderData renderData;

            while(queue.TryDequeue(out renderData))
            {
                array[index] = renderData;
                index++;
            }
        }
    }

    [BurstCompile]
    private struct SortByPossitionJob : IJob
    {
        public NativeArray<RenderData> sortArray;

        public void Execute()
        {
            // SORT
            for (int i = 0; i < sortArray.Length; i++)
            {
                for (int j = i + 1; j < sortArray.Length; j++)
                {
                    if (sortArray[i].position.y < sortArray[j].position.y)
                    {
                        //SWAP
                        RenderData tmpData = sortArray[i];
                        sortArray[i] = sortArray[j];
                        sortArray[j] = tmpData;
                    }
                }
            }
        }
    }

    [BurstCompile]
    private struct FillArraysParallelJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<RenderData> nativeArray;
        [NativeDisableContainerSafetyRestriction] public NativeArray<Matrix4x4> matrixArray;
        [NativeDisableContainerSafetyRestriction] public NativeArray<Vector4> uvArray;

        public int startIndex;

        public void Execute(int index)
        {
            RenderData renderData = nativeArray[index];
            matrixArray[startIndex + index] = renderData.matrix;
            uvArray[startIndex + index] = renderData.uv;
        }
    }

    protected override void OnStartRunning()
    {
        base.OnCreate();
        // Cache all the needed data to draw the mesh
        propertyBlock = new MaterialPropertyBlock();
        //uv = new Vector4[1];
        mesh = VisualElementsCache.GetInstance().mesh;
        material = VisualElementsCache.GetInstance().walkingSpriteSheetMaterial;

        shaderPropertyID = Shader.PropertyToID("_MainTex_UV");
    }

    protected override void OnUpdate()
    {
        EntityQuery query = GetEntityQuery(typeof(SpriteSheetAnimation_Data), typeof(Translation));
        NativeArray<SpriteSheetAnimation_Data> animDataArray = query.ToComponentDataArray<SpriteSheetAnimation_Data>(Allocator.TempJob);
        NativeArray<Translation> translations = query.ToComponentDataArray<Translation>(Allocator.TempJob);

        NativeQueue<RenderData> nativeQueue_1 = new NativeQueue<RenderData>(Allocator.TempJob);
        NativeQueue<RenderData> nativeQueue_2 = new NativeQueue<RenderData>(Allocator.TempJob);

        Camera camera = Camera.main;
        float3 cameraPosition = camera.transform.position;
        float yBottom = cameraPosition.y - camera.orthographicSize;
        float yTop_1 = cameraPosition.y + camera.orthographicSize;
        float yTop_2 = cameraPosition.y;

        CullAndSortJob job = new CullAndSortJob()
        {
            yBottom = yBottom,
            yTop_1 = yTop_1,
            yTop_2 = yTop_2,
            nativeQueue_1 = nativeQueue_1.AsParallelWriter(),
            nativeQueue_2 = nativeQueue_2.AsParallelWriter()
        };

        JobHandle handle = job.Schedule(this);
        handle.Complete();

        NativeArray<RenderData> nativeArray_1 = new NativeArray<RenderData>(nativeQueue_1.Count, Allocator.Temp);
        NativeArray<RenderData> nativeArray_2 = new NativeArray<RenderData>(nativeQueue_2.Count, Allocator.Temp);

        NativeQueueToArrayJob queueToArrayJob_1 = new NativeQueueToArrayJob()
        {
            array = nativeArray_1,
            queue = nativeQueue_1
        };

        NativeQueueToArrayJob queueToArrayJob_2 = new NativeQueueToArrayJob()
        {
            array = nativeArray_2,
            queue = nativeQueue_2
        };

        NativeArray<JobHandle> handles = new NativeArray<JobHandle>(2, Allocator.Temp);
        handles[0] = queueToArrayJob_1.Schedule();
        handles[1] = queueToArrayJob_2.Schedule();

        JobHandle.CompleteAll(handles);

        nativeQueue_1.Dispose();
        nativeQueue_2.Dispose();

        SortByPossitionJob sortByPossitionJob_1 = new SortByPossitionJob()
        {
            sortArray = nativeArray_1
        };

        SortByPossitionJob sortByPossitionJob_2 = new SortByPossitionJob()
        {
            sortArray = nativeArray_2
        };

        handles[0] = sortByPossitionJob_1.Schedule();
        handles[1] = sortByPossitionJob_2.Schedule();

        JobHandle.CompleteAll(handles);

        int visibleEntities = nativeArray_1.Length + nativeArray_2.Length;
        NativeArray<Matrix4x4> matrixArray = new NativeArray<Matrix4x4>(visibleEntities, Allocator.Temp);
        NativeArray<Vector4> uvArray = new NativeArray<Vector4>(visibleEntities, Allocator.Temp);

        Matrix4x4[] matrices = new Matrix4x4[visibleEntities];
        Vector4[] uvs = new Vector4[visibleEntities];

        FillArraysParallelJob fillArraysJob_1 = new FillArraysParallelJob()
        {
            nativeArray = nativeArray_1,
            matrixArray = matrixArray,
            uvArray = uvArray,
            startIndex = 0
        };

        FillArraysParallelJob fillArraysJob_2 = new FillArraysParallelJob()
        {
            nativeArray = nativeArray_2,
            matrixArray = matrixArray,
            uvArray = uvArray,
            startIndex = nativeArray_1.Length
        };

        handles[0] = fillArraysJob_1.Schedule(nativeArray_1.Length, 10);
        handles[0] = fillArraysJob_2.Schedule(nativeArray_2.Length, 10);
        JobHandle.CompleteAll(handles);

        int sliceCount = 1023;
        for (int i = 0; i < animDataArray.Length; i += sliceCount)
        {
            int sliceSize = math.min(animDataArray.Length - i, sliceCount);

            NativeArray<Matrix4x4>.Copy(matrixArray, i, matrices, 0, sliceSize);
            NativeArray<Vector4>.Copy(uvArray, i, uvs, 0, sliceSize);

            propertyBlock.SetVectorArray(shaderPropertyID, uvs);

            Graphics.DrawMeshInstanced(
                mesh,
                0,
                material,
                matrices,
                sliceSize,
                propertyBlock
            );
        }

        matrixArray.Dispose();
        uvArray.Dispose();
        animDataArray.Dispose();
        translations.Dispose();
    }
}
