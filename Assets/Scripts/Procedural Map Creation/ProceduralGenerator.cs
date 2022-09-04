using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Generates and contains extra information about each vertex in a mesh. Such as which other vertices are in the same location.
/// </summary>
public class SubMeshController
{
    public List<VertexGroup> VertexGroups => vertexGroups;
    private List<VertexGroup> vertexGroups;
    public List<Vector3> VertexPositions => vertexPositions;
    private List<Vector3> vertexPositions;
    //public List<int> VertexIndices => vertexIndices;
    //private List<int> vertexIndices;

    private Dictionary<int, int> vertexGroupIDPerVertex;


    public int GetNumberOfVertices
    {
        get
        {
            return vertexPositions.Count;
        }
    }
    public int GetNumberOfVertexGroups
    {
        get
        {
            return vertexGroups.Count;
        }
    }
    public Mesh ConnectedMesh => connectedMesh;
    private Mesh connectedMesh;

    public SubMeshController(Mesh mesh)
    {
        connectedMesh = mesh;

        vertexPositions = new List<Vector3>();
        connectedMesh.GetVertices(vertexPositions);

        vertexGroups = new List<VertexGroup>();
        vertexGroupIDPerVertex = new Dictionary<int, int>();

        RegroupVertices();
    }

    /// <summary>
    /// Figures out which vertices are in the same location as each other and groups them. Overwrites any previous data.
    /// This is O(n) where n is the number of verts.
    /// </summary>
    public void RegroupVertices(float searchRadius = 0.01f)
    {
        vertexGroupIDPerVertex.Clear();
        vertexGroups.Clear();

        for (int currentVertex = 0; currentVertex < GetNumberOfVertices; currentVertex++)
        {
            VertexGroup vertGroup;
            if (!vertexGroupIDPerVertex.ContainsKey(currentVertex))
            {
                vertGroup = new VertexGroup(vertexGroups.Count);
                vertGroup.AddVertex(currentVertex, ref vertexPositions);
                vertexGroups.Add(vertGroup);
                vertexGroupIDPerVertex[currentVertex] = vertexGroups.Count - 1;

                Debug.Log(vertGroup.Position);
                
            }
            else
            {
                vertGroup = vertexGroups[vertexGroupIDPerVertex[currentVertex]];
            }

            Vector3 currentVertexPos = vertexPositions[currentVertex];

            // Add all vertices that are within range:
            for (int otherVertex = 0; otherVertex < GetNumberOfVertices; otherVertex++)
            {
                if (otherVertex != currentVertex)
                {
                    if (Vector3.Distance(vertexPositions[otherVertex], currentVertexPos) <= searchRadius && !vertexGroupIDPerVertex.ContainsKey(otherVertex))
                    {
                        vertGroup.AddVertex(otherVertex, ref vertexPositions);
                        vertexGroupIDPerVertex[otherVertex] = vertGroup.ID;
                    }
                }
            }
        }

    }
    /// <summary>
    /// Returns the vertex group that a particular vertex is part of
    /// </summary>
    /// <param name="vertex">vertex's ID (in index form)</param>
    /// <returns></returns>
    public VertexGroup GetVertexGroupFromVertex(int vertex)
    {
        return vertexGroups[vertexGroupIDPerVertex[vertex]];
    }

    /// <summary>
    /// Using the mesh's local coordinates identify which vertex groups lie on the edge of the mesh, according to a certain direction.
    /// For a cube example, Vector3.up would identify all 4 vertex groups that are on the "up" side of the cube.
    /// </summary>
    /// <param name="localBoundaryDirection">Direction to search, in local mesh coordinates</param>
    /// <returns></returns>
    public List<int> GetVertexGroupsFromBoundary(Vector3 localBoundaryDirection, float searchError = 0.01f)
    {
        localBoundaryDirection.Normalize();
        List<int> edgeVertexGroups = new List<int>();

        Dictionary<int, float> distancesOfEachVertexGroup = new Dictionary<int, float>();

        float mostExtremeDistance = 0;
        for (int currentVertexGroup = 0; currentVertexGroup < GetNumberOfVertexGroups; currentVertexGroup++)
        {
            //Vector3 projection = Vector3.Project();
            distancesOfEachVertexGroup[currentVertexGroup] = Vector3.Project(vertexGroups[currentVertexGroup].Position, localBoundaryDirection).magnitude;
            float dotP = Vector3.Dot(vertexGroups[currentVertexGroup].Position, localBoundaryDirection);
            if(dotP < 0)
            {
                distancesOfEachVertexGroup[currentVertexGroup] = -distancesOfEachVertexGroup[currentVertexGroup];
            }


            if (distancesOfEachVertexGroup.Count == 1)
            {
                mostExtremeDistance = distancesOfEachVertexGroup[currentVertexGroup];
            }
            else if(distancesOfEachVertexGroup[currentVertexGroup] > mostExtremeDistance)
            {
                mostExtremeDistance = distancesOfEachVertexGroup[currentVertexGroup];
            }
        }

        for (int currentVertexGroup = 0; currentVertexGroup < GetNumberOfVertexGroups; currentVertexGroup++)
        {
            float vertexGroupDistance = distancesOfEachVertexGroup[currentVertexGroup];
            if(vertexGroupDistance < mostExtremeDistance + searchError && vertexGroupDistance > mostExtremeDistance - searchError)
            {
                edgeVertexGroups.Add(currentVertexGroup);
                Debug.Log(vertexGroups[currentVertexGroup].Position);
            }
        }

        return edgeVertexGroups;
    }

    public void MoveVertexGroup(int vertexGroupID, Vector3 translation)
    {
        vertexGroups[vertexGroupID].MoveVertices(ref vertexPositions, translation);
        connectedMesh.SetVertices(vertexPositions);
        //connectedMesh.MarkModified();
    }

}


/// <summary>
/// A list of vertices (in int index form) that are in the same position
/// </summary>
public class VertexGroup
{
    public int ID => id;
    private int id;

    public Vector3 Position => position;
    public List<int> Vertices => vertices;

    private Vector3 position;
    private List<int> vertices;

    public VertexGroup(int groupID)
    {
        id = groupID;
        vertices = new List<int>();
    }
    public void AddVertex(int vertexID, ref List<Vector3> meshVertexPositions)
    {
        if(vertices.Count == 0)
        {
            position = meshVertexPositions[vertexID];
        }
        vertices.Add(vertexID);
    }
    
    /// <summary>
    /// Modifies the vertex positions of all vertices in this group, according to a certain translation
    /// </summary>
    /// <param name="meshVertexPositions">Vertex positions retrieved using "currentMesh.GetVertices(x)"</param>
    public void MoveVertices(ref List<Vector3> meshVertexPositions, Vector3 translation)
    {
        if(vertices.Count == 0)
        {
            Debug.LogWarning("Attempting to translate vertices in a group that contains no vertices!");
            return;
        }

        for (int currentIndex = 0; currentIndex < vertices.Count; currentIndex++)
        {
            
            meshVertexPositions[vertices[currentIndex]] += translation;
            position += translation;
        }
    }

}


public class ProceduralGenerator : MonoBehaviour
{
    void Start()
    {
        EditObject(this.gameObject);
    }
    void Update()
    {
        
    }

    void EditObject(GameObject obj)
    {
        MeshFilter objMeshFilter = obj.GetComponent<MeshFilter>();
        Mesh currentMesh = objMeshFilter.mesh;

        SubMeshController meshController = new SubMeshController(currentMesh);

        List<int> edgeGroups = meshController.GetVertexGroupsFromBoundary(Vector3.up);
        for (int i = 0; i < edgeGroups.Count; i++)
        {

            meshController.MoveVertexGroup(edgeGroups[i], Vector3.up * 0.3f);
        }

    }

}
