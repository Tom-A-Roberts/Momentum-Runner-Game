using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class MeshCreator : MonoBehaviour
{
    public GameObject probuilderUnitCubePrefab;
    public GameObject probuilderWallPrefab;

    [Header("Floor Size Settings")]

    public float floorDepthMaximum = 2f;
    public float floorDepthMinimum = 0.5f;
    public float largestFloorMargin = 1f;

    //[Header("Wallrunning Size Settings")]
    
    //[Tooltip("How quick the playersim runs and wallruns")]

    //private ProBuilderMesh probuilderCube;
    //private ProBuilderMesh probuilderWall;

    public SimpleTuple<Vector3, Vector3> BoundingBoxFromList(List<Vector3> positions)
    {
        Vector3 smallestCorner = Vector3.zero;
        Vector3 largestCorner = Vector3.zero;
        for (int posID = 0; posID < positions.Count; posID++)
        {
            if (posID == 0)
            {
                smallestCorner = positions[0];
                largestCorner = positions[0];
            }
            if (positions[posID].x < smallestCorner.x) { smallestCorner.x = positions[posID].x; }
            if (positions[posID].y < smallestCorner.y) { smallestCorner.y = positions[posID].y; }
            if (positions[posID].z < smallestCorner.z) { smallestCorner.z = positions[posID].z; }

            if (positions[posID].x > largestCorner.x) { largestCorner.x = positions[posID].x; }
            if (positions[posID].y > largestCorner.y) { largestCorner.y = positions[posID].y; }
            if (positions[posID].z > largestCorner.z) { largestCorner.z = positions[posID].z; }
        }
        return new SimpleTuple<Vector3, Vector3>(smallestCorner, largestCorner);
    }

    public void CreateRunningCube(List<Vector3> positions, float difficulty)
    {
        SimpleTuple<Vector3, Vector3> boundingBox = BoundingBoxFromList(positions);
        Vector3 smallestCorner = boundingBox.item1;
        Vector3 largestCorner = boundingBox.item2;

        smallestCorner.y -= 1 + Random.value * floorDepthMaximum;

        Vector3 size = new Vector3(largestCorner.x - smallestCorner.x, largestCorner.y - smallestCorner.y, largestCorner.z - smallestCorner.z);
        

        float margin = (1 - Mathf.Clamp(difficulty, 0, 0.9f)) * largestFloorMargin;
        size.x += margin;
        size.z += margin;

        //Vector3 center = new Vector3((largestCorner.x + smallestCorner.x)/2, (largestCorner.y + smallestCorner.y)/2, (largestCorner.z + smallestCorner.z)/2);
        Vector3 center = (smallestCorner + largestCorner) / 2;

        GameObject cubeGameobject = Instantiate(probuilderUnitCubePrefab, center, Quaternion.identity);
        ProBuilderMesh cubeMesh = cubeGameobject.GetComponent<ProBuilderMesh>();

        List<Vector3> newPositions = new List<Vector3>();

        for (int vertexID = 0; vertexID < cubeMesh.positions.Count; vertexID++)
        {
            //cubeMesh.positions[vertexID] = new Vector3(cubeMesh.positions[vertexID].x * size.x, cubeMesh.positions[vertexID].y * size.y, cubeMesh.positions[vertexID].z * size.z);
            newPositions.Add(new Vector3(cubeMesh.positions[vertexID].x * size.x, cubeMesh.positions[vertexID].y * size.y, cubeMesh.positions[vertexID].z * size.z));
        }
        cubeMesh.positions = newPositions;
        cubeMesh.ToMesh();
        cubeMesh.Refresh();
    }

    public void CreateWallrunningWall(List<Vector3> positions, bool leftSide, float difficulty)
    {         
        GameObject wallGameobject = Instantiate(probuilderWallPrefab, positions[0], Quaternion.identity);
        ProBuilderMesh wallMesh = wallGameobject.GetComponent<ProBuilderMesh>();


    }

    void Start()
    {
        //probuilderCube = probuilderCubePrefab.GetComponent<ProBuilderMesh>();


    }
    void Update()
    {
        
    }
}
