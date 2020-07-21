using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualElementsCache : MonoBehaviour
{
    public Mesh mesh;
    public Material walkingSpriteSheetMaterial;

    private static VisualElementsCache instance;
    public static VisualElementsCache GetInstance()
    {
        return instance;
    }

    private void Awake()
    {
        instance = this;
    }
}
