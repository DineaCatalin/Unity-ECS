using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;

public class GameManager : MonoBehaviour
{
    EntityManager entityManager;

    [SerializeField] int numberEntities = 1000;

    private void Start()
    {
        entityManager = World.Active.EntityManager;
        EntityArchetype archetype = entityManager.CreateArchetype(
            typeof(LocalToWorld),
            typeof(Translation),
            typeof(Rotation),
            typeof(SpriteSheetAnimation_Data)
        );

        NativeArray<Entity> entities = new NativeArray<Entity>(numberEntities, Allocator.Temp);
        entityManager.CreateEntity(archetype, entities);

        foreach (Entity entity in entities)
        {
            entityManager.SetComponentData(entity, new Translation
            {
                Value = new float3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-3f, 3f), 0)
            });

            entityManager.SetComponentData(entity, new SpriteSheetAnimation_Data
            {
                currentFrame = UnityEngine.Random.Range(0, 5),
                frameCount = 4,
                frameTimer = 0f,
                frameTimerMax = 0.1f
            });
        }

        entities.Dispose();
    }

    private Mesh CreateMesh(float width, float height)
    {
        Vector3[] vertices = new Vector3[4];
        Vector2[] uv = new Vector2[4];
        int[] triangles = new int[6];

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        vertices[0] = new Vector3(-halfWidth, -halfHeight);    // 0,0
        vertices[1] = new Vector3(-halfWidth, halfHeight);     // 0,1  
        vertices[2] = new Vector3(halfWidth, halfHeight);      // 1,1
        vertices[3] = new Vector3(halfWidth, -halfHeight);     // 1,0

        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(0, 1);
        uv[2] = new Vector2(1, 1);
        uv[3] = new Vector2(1, 0);

        // 1st Triangle
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 3;

        // 2nd Triangle
        triangles[3] = 1;
        triangles[4] = 2;
        triangles[5] = 3;

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        return mesh;
    }
}

