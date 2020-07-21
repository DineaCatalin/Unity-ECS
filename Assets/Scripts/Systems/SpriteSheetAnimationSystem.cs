using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;

public struct SpriteSheetAnimation_Data : IComponentData
{
    public int currentFrame;
    public int frameCount;
    public float frameTimer;
    public float frameTimerMax;

    public Vector4 uv;
    public Matrix4x4 matrix;
}


public class SpriteSheetAnimation_Animate : JobComponentSystem
{
    [BurstCompile]
    public struct Job : IJobForEach<SpriteSheetAnimation_Data, Translation>
    {
        public float deltaTime;

        private float uvWidth;
        private float uvHeight;
        private float uvOffsetX;
        private float uvOffsetY;

        public void Execute(ref SpriteSheetAnimation_Data animData, ref Translation translation)
        {
            animData.frameTimer += deltaTime;

            while (animData.frameTimer >= animData.frameTimerMax)
            {
                animData.frameTimer -= animData.frameTimerMax;
                animData.currentFrame = (animData.currentFrame + 1) % animData.frameCount;
            }

            uvWidth = 1f / animData.frameCount;
            uvHeight = 1f;
            uvOffsetX = uvWidth * animData.currentFrame;
            uvOffsetY = 0f;
            animData.uv = new Vector4(uvWidth, uvHeight, uvOffsetX, uvOffsetY);

            float3 position = translation.Value;
            position.z = position.y * 0.01f;
            animData.matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Job job = new Job { deltaTime = Time.deltaTime };
        return job.Schedule(this, inputDeps);
    }
}
