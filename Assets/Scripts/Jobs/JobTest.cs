using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using System.Collections.Generic;

public class JobTest : MonoBehaviour
{

    [SerializeField]
    private bool useJobs;

    [SerializeField]
    private Transform templateTransform;

    private List<Pikachu> pikachus;

    public class Pikachu
    {
        public Transform m_Transform;
        public float m_MoveY;
    }

    private void Start()
    {
        pikachus = new List<Pikachu>();
        for(int i = 0; i < 1000; i++)
        {
            Transform pikTrans = Instantiate(templateTransform, new Vector3(UnityEngine.Random.Range(-7f, 7), UnityEngine.Random.Range(-7f, 7), 0), Quaternion.identity);
            pikachus.Add(new Pikachu()
            {
                m_Transform = pikTrans,
                m_MoveY = UnityEngine.Random.Range(1f, 2f)
            }); 
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (useJobs)
        {
            NativeArray<float> moveYArray = new NativeArray<float>(pikachus.Count, Allocator.TempJob);
            NativeArray<float3> positionArray = new NativeArray<float3>(pikachus.Count, Allocator.TempJob);
            NativeArray<JobHandle> handles = new NativeArray<JobHandle>(pikachus.Count, Allocator.TempJob);
            TransformAccessArray transforms = new TransformAccessArray(pikachus.Count);

            for (int i = 0; i < pikachus.Count; i++)
            {
                moveYArray[i] = pikachus[i].m_MoveY;
                //positionArray[i] = pikachus[i].m_Transform.position;
                transforms.Add(pikachus[i].m_Transform);
            }

            //HardParallelJob job = new HardParallelJob
            //{
            //    deltaTime = Time.deltaTime,
            //    moveYArray = moveYArray,
            //    positionsArray = positionArray
            //};

            HardParallelTransformJob job = new HardParallelTransformJob
            {
                deltaTime = Time.deltaTime,
                moveYArray = moveYArray
            };

            //JobHandle handle = job.Schedule(pikachus.Count, 100);
            JobHandle handle = job.Schedule(transforms);
            handle.Complete();

            for(int i = 0; i < pikachus.Count; i++)
            {
                pikachus[i].m_MoveY = moveYArray[i];
                //pikachus[i].m_Transform.position = positionArray[i];
                pikachus[i].m_Transform.position = transforms[i].position;
            }

            //positionArray.Dispose();
            transforms.Dispose();
            moveYArray.Dispose();
        }
        else
        {
            foreach (Pikachu pika in pikachus)
            {
                pika.m_Transform.position += new Vector3(0, pika.m_MoveY * Time.deltaTime, 0);

                if (pika.m_Transform.position.y > 6f)
                    pika.m_MoveY = -math.abs(pika.m_MoveY);
                if (pika.m_Transform.position.y < -5f)
                    pika.m_MoveY = math.abs(pika.m_MoveY);

                CostlyFunction();
            }
        }


        float startTime = Time.realtimeSinceStartup;

        //if (useJobs)
        //{
        //    NativeList<JobHandle> jobHandels = new NativeList<JobHandle>(Allocator.Temp);
        //    for(int i = 0; i < 10; i++)
        //    {
        //        JobHandle handle = CostlyFunctionAsJob();
        //        jobHandels.Add(handle);
        //    }

        //    JobHandle.CompleteAll(jobHandels);
        //    jobHandels.Dispose();
        //}
        //else
        //{
        //    for (int i = 0; i < 10; i++)
        //    {
        //        CostlyFunction();
        //    }
        //}

        //Debug.Log((Time.realtimeSinceStartup - startTime) * 1000f + "ms");
    }

    void CostlyFunction()
    {
        float value = 0f;
        for(int i = 0; i < 50000; i++)
        {
            value = math.exp10(math.sqrt(value));
        }
    }

    JobHandle CostlyFunctionAsJob()
    {
        HardJob hardJob = new HardJob();
        return hardJob.Schedule();
    }
}

[BurstCompile]
struct HardJob : IJob
{
    public void Execute()
    {
        float value = 0f;
        for (int i = 0; i < 50000; i++)
        {
            value = math.exp10(math.sqrt(value));
        }
    }
}

[BurstCompile]
struct HardParallelJob : IJobParallelFor
{
    public NativeArray<float> moveYArray;
    public NativeArray<float3> positionsArray;
    public float deltaTime;

    public void Execute(int index)
    {
        positionsArray[index] += new float3(0, moveYArray[index] * deltaTime, 0);

        if (positionsArray[index].y > 6f)
            moveYArray[index] = -math.abs(moveYArray[index]);
        if (positionsArray[index].y < -5f)
            moveYArray[index] = math.abs(moveYArray[index]);

        float value = 0f;
        for (int i = 0; i < 50000; i++)
        {
            value = math.exp10(math.sqrt(value));
        }
    }
}

[BurstCompile]
struct HardParallelTransformJob : IJobParallelForTransform
{
    public NativeArray<float> moveYArray;
    public float deltaTime;

    public void Execute(int index, TransformAccess transform)
    {
        transform.position += new Vector3(0, moveYArray[index] * deltaTime, 0);

        if (transform.position.y > 6f)
            moveYArray[index] = -math.abs(moveYArray[index]);
        if (transform.position.y < -5f)
            moveYArray[index] = math.abs(moveYArray[index]);

        float value = 0f;
        for (int i = 0; i < 50000; i++)
        {
            value = math.exp10(math.sqrt(value));
        }
    }
}