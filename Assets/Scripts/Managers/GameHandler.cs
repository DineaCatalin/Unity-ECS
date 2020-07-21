using UnityEngine;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

//
// -> USED FOR THE FIND TARGET / QUADRANT SYSTEM
//

public class GameHandler : MonoBehaviour {

    [SerializeField] private int units = 10;
    [SerializeField] private int targets = 10;

    [SerializeField] private Material unitMaterial;
    [SerializeField] private Material targetMaterial;
    [SerializeField] private Mesh quadMesh;

    private static EntityManager entityManager;

    private void Start() {
        entityManager = World.Active.EntityManager;

        for (int i = 0; i < units; i++) {
            SpawnUnitEntity();
        }

        for (int i = 0; i < targets; i++) {
            SpawnTargetEntity();
        }
    }

    private float spawnTargetTimer;

    private void Update() {
        spawnTargetTimer -= Time.deltaTime;
        if (spawnTargetTimer < 0) {
            spawnTargetTimer = .1f;
            
            for (int i = 0; i < 10; i++) {
                SpawnTargetEntity();
            }
        }
    }

    private void SpawnUnitEntity() {
        SpawnUnitEntity(new float3(UnityEngine.Random.Range(-20, +20f), UnityEngine.Random.Range(-20, +20f), 0));
    }

    private void SpawnUnitEntity(float3 position) {
        Entity entity = entityManager.CreateEntity(
            typeof(Translation),
            typeof(LocalToWorld),
            typeof(RenderMesh),
            typeof(Scale),
            typeof(Unit),
            typeof(QuadrantEntity)
        );
        SetEntityComponentData(entity, position, quadMesh, unitMaterial);
        entityManager.SetComponentData(entity, new Scale { Value = 1.5f });
        entityManager.SetComponentData(entity, new QuadrantEntity { type = QuadrantEntity.TypeEnum.Unit });
    }

    private void SpawnTargetEntity() {
        Entity entity = entityManager.CreateEntity(
            typeof(Translation),
            typeof(LocalToWorld),
            typeof(RenderMesh),
            typeof(Scale),
            typeof(Target),
            typeof(QuadrantEntity)
        );
        SetEntityComponentData(entity, new float3(UnityEngine.Random.Range(-20, +20f), UnityEngine.Random.Range(-20, +20f), 0), quadMesh, targetMaterial);
        entityManager.SetComponentData(entity, new Scale { Value = .5f });
        entityManager.SetComponentData(entity, new QuadrantEntity { type = QuadrantEntity.TypeEnum.Target });
    }

    private void SetEntityComponentData(Entity entity, float3 spawnPosition, Mesh mesh, Material material) {
        entityManager.SetSharedComponentData<RenderMesh>(entity,
            new RenderMesh {
                material = material,
                mesh = mesh,
            }
        );

        entityManager.SetComponentData<Translation>(entity, 
            new Translation { 
                Value = spawnPosition
            }
        );
    }

}

public struct Unit : IComponentData { }
public struct Target : IComponentData { }

public struct HasTarget : IComponentData {
    public Entity targetEntity;
}

public class HasTargetDebug : ComponentSystem {

    protected override void OnUpdate() {
        Entities.ForEach((Entity entity, ref Translation translation, ref HasTarget hasTarget) => {
            if (World.Active.EntityManager.Exists(hasTarget.targetEntity)) {
                Translation targetTranslation = World.Active.EntityManager.GetComponentData<Translation>(hasTarget.targetEntity);
                Debug.DrawLine(translation.Value, targetTranslation.Value);
            }
        });
    }

}








