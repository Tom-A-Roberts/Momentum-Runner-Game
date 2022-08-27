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
        if(localExtrusionDirection == Vector3.zero) { return; }
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
        proBuilderMesh.ToMesh();
        proBuilderMesh.Refresh();
        RebuildRingSegments();
    }

    public void RefreshMesh()
    {
        proBuilderMesh.ToMesh();
        proBuilderMesh.Refresh();
    }

    

}

[RequireComponent(typeof(ProBuilderMesh))]
public class MeshManipulator : MonoBehaviour
{
    ProBuilderMesh myMesh;
    void Start()
    {
        myMesh = GetComponent<ProBuilderMesh>();
        EditMesh();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static Vector3 GetFacePosition(ProBuilderMesh mesh, int faceID)
    {
        Vector3 outPosition = Vector3.zero;
        int vertCount = 0;
        for (int i = 0; i < mesh.faces[faceID].distinctIndexes.Count; i++)
        {
            outPosition += mesh.positions[mesh.faces[faceID].distinctIndexes[i]];
            vertCount += 1;
        }
        if(vertCount >= 0)
        {
            outPosition /= vertCount;
        }
        //Debug.Log(outPosition);
        return outPosition;
    }

    public static List<Face> GetBoundaryFaces(ProBuilderMesh mesh, Vector3 localBoundaryDirection, float searchError = 0.01f)
    {
        localBoundaryDirection.Normalize();
        List<Face> edgeFaces = new List<Face>();

        Dictionary<int, float> distancesOfEachFace = new Dictionary<int, float>();

        float mostExtremeDistance = 0;
        for (int currentFace = 0; currentFace < mesh.faceCount; currentFace++)
        {
            //Vector3 projection = Vector3.Project();
            Vector3 facePosition = GetFacePosition(mesh, currentFace);
            //distancesOfEachFace[currentFace] = Vector3.Project(mesh.faces[currentFace]., localBoundaryDirection).magnitude;
            distancesOfEachFace[currentFace] = Vector3.Dot(facePosition, localBoundaryDirection);
            //float dotP = Vector3.Dot(GetFacePosition(mesh, currentFace), localBoundaryDirection);
            //if (dotP < 0)
            //{
            //    distancesOfEachFace[currentFace] = -distancesOfEachFace[currentFace];
            //}


            if (distancesOfEachFace.Count == 1)
            {
                mostExtremeDistance = distancesOfEachFace[currentFace];
            }
            else if (distancesOfEachFace[currentFace] > mostExtremeDistance)
            {
                mostExtremeDistance = distancesOfEachFace[currentFace];
            }
        }

        for (int currentFace = 0; currentFace < mesh.faceCount; currentFace++)
        {
            float faceDistance = distancesOfEachFace[currentFace];
            if (faceDistance < mostExtremeDistance + searchError && faceDistance > mostExtremeDistance - searchError)
            {
                edgeFaces.Add(mesh.faces[currentFace]);
                //Debug.Log(vertexGroups[currentVertexGroup].Position);
            }
        }

        return edgeFaces;
    }

    static HashSet<int> GetFaceUniqueVertexIDs(List<Face> faces)
    {
        HashSet<int> vertexIDs = new HashSet<int>();
        for (int faceID = 0; faceID < faces.Count; faceID++)
        {
            foreach (int vert in faces[faceID].distinctIndexes)
            {
                if (!vertexIDs.Contains(vert))
                {
                    vertexIDs.Add(vert);
                }
            }
        }
        return vertexIDs;
    }

    void EditMesh()
    {
        ManipulatableCurvedWall wall = new ManipulatableCurvedWall(this.gameObject);
        wall.ExtrudeMeshSide(Vector3.up, 4, 0.3f);

        //print(myMesh.positions.Count);
        //print(myMesh.sharedVertices.Count);
        //myMesh.
        //List<Face> boundaryFaces = GetBoundaryFaces(myMesh, Vector3.up);

        //Face[] newFaces = ExtrudeElements.Extrude(myMesh, boundaryFaces, ExtrudeMethod.FaceNormal, 1);
        //print(boundaryFaces.Count);
        //Face[] newFaces2 = ExtrudeElements.Extrude(myMesh, boundaryFaces, ExtrudeMethod.FaceNormal, 1);

        //print(boundaryFaces.Count);
        //myMesh.ToMesh();
        //myMesh.Refresh();
        //myMesh.gameObject.GetComponent<MeshFilter>().mesh.MarkModified();
        //myMesh.Refresh(RefreshMask.);

        //for (int i = 0; i < boundaryFaces.Count; i++)
        //{
        //    //for (int vertID = 0; vertID < boundaryFaces[vertID].distinctIndexes.Count; vertID++)
        //    //{
        //    //    myMesh.positions[boundaryFaces[i].distinctIndexes[vertID]] += Vector3.up * 0.3f;

        //    //}
        //}
        //HashSet<int> boundaryVerts = GetFaceUniqueVertexIDs(boundaryFaces);

        //myMesh.TranslateVertices(boundaryVerts, Vector3.up * 0.1f);

        //myMesh.Refresh();

    }
}
