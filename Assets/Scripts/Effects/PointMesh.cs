using UnityEngine;

[ExecuteInEditMode]
public class PointMesh : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var meshFilter = GetComponent<MeshFilter>();
        var mesh = meshFilter.mesh;

        if (mesh == null)
            return;

        var vertexCount = mesh.vertexCount;
        if (vertexCount == 0)
            return;

        var indices = new int[vertexCount];
        for (int i = 0; i < vertexCount; i++)
            indices[i] = i;

        mesh.subMeshCount = 1;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
    }

}
