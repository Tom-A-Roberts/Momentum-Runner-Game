using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;


public class ManipulatableCurvedWall
{
    public GameObject gameObject;
    public ProBuilderMesh proBuilderMesh;
    public Vector3 LocalExtrusionDirection => localExtrusionDirection;
    private Vector3 localExtrusionDirection = Vector3.zero;
    public Dictionary<int, List<int>> RingSegments => ringSegments;
    private Dictionary<int, List<int>> ringSegments;
    private HashSet<int> addedVerts;

    public ManipulatableCurvedWall(GameObject proBuilderGameObject)
    {
        gameObject = proBuilderGameObject;
        proBuilderMesh = gameObject.GetComponent<ProBuilderMesh>();
        ringSegments = new Dictionary<int, List<int>>();
        addedVerts = new HashSet<int>();
    }

    public void RebuildRingSegments(float errorRange = 0.01f)
    {
        ringSegments.Clear();
        addedVerts.Clear();
        if (localExtrusionDirection == Vector3.zero) { return; }
        //ringsTracker = new Dictionary<int, List<int>>();
        for (int vert = 0; vert < proBuilderMesh.vertexCount; vert++)
        {
            float distance = Vector3.Dot(proBuilderMesh.positions[vert], localExtrusionDirection);
            distance /= errorRange;
            int bucket = Mathf.RoundToInt(distance);
            if (ringSegments.ContainsKey(bucket))
            {
                ringSegments[bucket].Add(vert);
            }
            else
            {
                ringSegments[bucket] = new List<int>();
                ringSegments[bucket].Add(vert);
            }
        }
        Debug.Log("Segments: " + ringSegments.Values.Count.ToString());
    }


    /// <summary>
    /// Finds the boundary face(s) in direction and extrudes them by extrusionDistance n times.
    /// </summary>
    /// <param name="direction">Direction of the side to extrude. E.g Vector3.up will extrude the top faces.</param>
    /// <param name="numberOfExtrusions">Number of rings to add .E.g. 1 would be a single extrusion on the shape, resulting in 1 extra ring.</param>
    public void ExtrudeMeshSide(Vector3 direction, int numberOfExtrusions, float extrusionDistance)
    {
        localExtrusionDirection = direction.normalized;
        List<Face> boundaryFaces = MeshManipulator.GetBoundaryFaces(proBuilderMesh, localExtrusionDirection);

        for (int i = 0; i < numberOfExtrusions; i++)
        {
            ExtrudeElements.Extrude(proBuilderMesh, boundaryFaces, ExtrudeMethod.FaceNormal, extrusionDistance);
        }
        RefreshMesh();
        RebuildRingSegments();
    }

    public Face[] ExtrudeMeshFace(List<Face> boundaryFaces, float extrusionDistance)
    {
        return ExtrudeElements.Extrude(proBuilderMesh, boundaryFaces, ExtrudeMethod.FaceNormal, extrusionDistance);
    }



    public void RefreshMesh()
    {
        proBuilderMesh.ToMesh();
        proBuilderMesh.Refresh();
    }

    public Vector3 GetFacePosition(int faceID)
    {
        Vector3 outPosition = Vector3.zero;
        int vertCount = 0;
        for (int i = 0; i < proBuilderMesh.faces[faceID].distinctIndexes.Count; i++)
        {
            outPosition += proBuilderMesh.positions[proBuilderMesh.faces[faceID].distinctIndexes[i]];
            vertCount += 1;
        }
        if (vertCount >= 0)
        {
            outPosition /= vertCount;
        }
        //Debug.Log(outPosition);
        return outPosition;
    }

    public List<Face> GetBoundaryFaces(Vector3 localBoundaryDirection, float searchError = 0.01f)
    {
        localBoundaryDirection.Normalize();
        List<Face> edgeFaces = new List<Face>();

        Dictionary<int, float> distancesOfEachFace = new Dictionary<int, float>();

        float mostExtremeDistance = 0;
        for (int currentFace = 0; currentFace < proBuilderMesh.faceCount; currentFace++)
        {
            Vector3 facePosition = GetFacePosition(currentFace);
            distancesOfEachFace[currentFace] = Vector3.Dot(facePosition, localBoundaryDirection);

            if (distancesOfEachFace.Count == 1)
            {
                mostExtremeDistance = distancesOfEachFace[currentFace];
            }
            else if (distancesOfEachFace[currentFace] > mostExtremeDistance)
            {
                mostExtremeDistance = distancesOfEachFace[currentFace];
            }
        }

        for (int currentFace = 0; currentFace < proBuilderMesh.faceCount; currentFace++)
        {
            float faceDistance = distancesOfEachFace[currentFace];
            if (faceDistance < mostExtremeDistance + searchError && faceDistance > mostExtremeDistance - searchError)
            {
                edgeFaces.Add(proBuilderMesh.faces[currentFace]);
            }
        }

        return edgeFaces;
    }

    public List<int> GetIndicesInFaces(List<Face> boundaryFaces)
    {
        List<int> indicesList = new List<int>();

        for (int i = 0; i < boundaryFaces.Count; i++)
        {
            for (int index = 0; index < boundaryFaces[i].distinctIndexes.Count; index++)
            {
                indicesList.Add(boundaryFaces[i].distinctIndexes[index]);
            }
        }

        return indicesList;
    }

    public List<Edge> GetEdgesInFaces(List<Face> boundaryFaces)
    {
        List<Edge> edgeList = new List<Edge>();
        for (int i = 0; i < boundaryFaces.Count; i++)
        {

            for (int e = 0; e < boundaryFaces[i].edges.Count; e++)
            {
                edgeList.Add(boundaryFaces[i].edges[e]);
            }
        }

        return edgeList;
    }

    public static void ResizeProbuilderMesh(ProBuilderMesh _mesh, Vector3 newLocalSize)
    {
        List<Vector3> newPositions = new List<Vector3>();
        for (int vertexID = 0; vertexID < _mesh.positions.Count; vertexID++)
        {
            newPositions.Add(new Vector3(_mesh.positions[vertexID].x * newLocalSize.x, _mesh.positions[vertexID].y * newLocalSize.y, _mesh.positions[vertexID].z * newLocalSize.z));
        }
        _mesh.positions = newPositions;
    }

}

public class MeshCreator : MonoBehaviour
{
    public GameObject probuilderUnitCubePrefab;
    public GameObject probuilderWallPrefab;
    public GameObject grapplePointPrefab;

    [Header("Floor Size Settings")]

    public float floorDepthMaximum = 4f;
    public float floorDepthMinimum = 0.5f;
    public float largestFloorMargin = 3f;

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

        ManipulatableCurvedWall.ResizeProbuilderMesh(cubeMesh, size);

        cubeMesh.ToMesh();
        cubeMesh.Refresh();
    }

    public void CreateWallrunningWall(List<Vector3> positions, Vector3 wallDirection, Vector3 forwards,float difficulty)
    {
        if(positions.Count > 1)
        {
            const float wallPrefabStartingDepth = 0.1f;

            const float wallWidth = 0.8f;
            float wallHeight = 5f + Random.value * 5;


            const int placeRingsModValue = 8;

            Vector3 startingPosition = positions[0] - (forwards * wallPrefabStartingDepth / 2);
            startingPosition += wallDirection.normalized * wallWidth*(0.5f+ (Random.value*2));

            GameObject wallGameobject = Instantiate(probuilderWallPrefab, startingPosition, Quaternion.identity);

            ManipulatableCurvedWall wallManager = new ManipulatableCurvedWall(wallGameobject);

            ManipulatableCurvedWall.ResizeProbuilderMesh(wallManager.proBuilderMesh, new Vector3(wallWidth, wallHeight, 1));

            List<Face> boundaryFaces = wallManager.GetBoundaryFaces(forwards);
            List<Edge> boundaryEdges = wallManager.GetEdgesInFaces(boundaryFaces);

            Vector3 lastPoint = positions[0];
            for (int posID = 1; posID < positions.Count; posID++)
            {
                if(posID % placeRingsModValue == 0 || posID == positions.Count - 1)
                {
                    Vector3 difference = positions[posID] - lastPoint;
                    float extrusionDistance = Vector3.Dot(difference, forwards);

                    wallManager.ExtrudeMeshFace(boundaryFaces, extrusionDistance);

                    wallManager.proBuilderMesh.TranslateVertices(boundaryEdges, difference - forwards * extrusionDistance);

                    lastPoint = positions[posID];
                }
            }
            wallManager.RebuildRingSegments();

            wallManager.RefreshMesh();
        }
    }

    public void CreateGrapplePoint(Vector3 grapplePoint)
    {
        const float smallestScale = 1;
        const float LargestScale = 3;
        GameObject grapplePointGameobject = Instantiate(grapplePointPrefab, grapplePoint, Quaternion.identity);
        float x_size = (LargestScale - smallestScale) * Random.value + smallestScale;
        float y_size = (LargestScale - smallestScale) * Random.value + smallestScale;
        float z_size = (LargestScale - smallestScale) * Random.value + smallestScale;
        grapplePointGameobject.transform.localScale = new Vector3(x_size, y_size, z_size);
    }
}
