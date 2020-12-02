using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class UpdateNavMesh : MonoBehaviour
{
    public NavMeshSurface surface;
    // Start is called before the first frame update
    public bool refresh;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

     void OnValidate() {
        surface.BuildNavMesh();
                            // NavMeshAssetManager.instance.StartBakingSurfaces(targets);

    }
}
