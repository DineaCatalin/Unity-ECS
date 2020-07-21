//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Collections;
//using Unity.Transforms;

//[UpdateAfter(typeof(SpriteSheetAnimation_Animate))]
//public class SpriteSheetRenderer : ComponentSystem
//{
//    MaterialPropertyBlock propertyBlock;
//    //Vector4[] uv;

//    Mesh mesh;
//    Material material;

//    Camera camera;

//    int shaderPropertyID;

//    protected override void OnStartRunning()
//    {
//        base.OnStartRunning();

//        // Cache all the needed data to draw the mesh
//        propertyBlock = new MaterialPropertyBlock();
//        //uv = new Vector4[1];
//        mesh = VisualElementsCache.GetInstance().mesh;
//        material = VisualElementsCache.GetInstance().walkingSpriteSheetMaterial;
//        camera = Camera.main;

//        shaderPropertyID = Shader.PropertyToID("_MainTex_UV");
//    }

//    protected override void OnUpdate()
//    {
//        EntityQuery query = GetEntityQuery(typeof(SpriteSheetAnimation_Data), typeof(Translation));
//        NativeArray<SpriteSheetAnimation_Data> animDataArray = query.ToComponentDataArray<SpriteSheetAnimation_Data>(Allocator.TempJob);
//        NativeArray<Translation> translations = query.ToComponentDataArray<Translation>(Allocator.TempJob);

//        // SORT
//        for (int i = 0; i < translations.Length; i++)
//        {
//            for (int j = i+1; j < translations.Length; j++)
//            {
//                if(translations[i].Value.y < translations[j].Value.y)
//                {
//                    //SWAP
//                    Translation tmpTranslation = translations[i];
//                    translations[i] = translations[j];
//                    translations[j] = tmpTranslation;

//                    SpriteSheetAnimation_Data tmpAnimData = animDataArray[i];
//                    animDataArray[i] = animDataArray[j];
//                    animDataArray[j] = tmpAnimData;
//                }
//            }
//        }

//        int sliceCount = 1023;
//        for (int i = 0; i < animDataArray.Length; i += sliceCount)
//        {
//            int sliceSize = math.min(animDataArray.Length - i, sliceCount);

//            List<Vector4> uvList = new List<Vector4>();
//            List<Matrix4x4> matrixList = new List<Matrix4x4>();

//            for (int j = 0; j < sliceSize; j++)
//            {
//                uvList.Add(animDataArray[i+j].uv);
//                matrixList.Add(animDataArray[i+j].matrix);
//            }

//            propertyBlock.SetVectorArray(shaderPropertyID, uvList);

//            Graphics.DrawMeshInstanced(
//                mesh,
//                0,
//                material,
//                matrixList,
//                propertyBlock
//            );
//        }

//        animDataArray.Dispose();
//        translations.Dispose();
//    }
//}
