using UnityEngine;

public class TofuVolume : MonoBehaviour
{
    void Start()
    {
        float volume = VolumeOfMesh(GetComponent<MeshFilter>().mesh, transform);
        Debug.Log("Name: " + gameObject.name + ", Volume: " + volume);
    }

    public float VolumeOfMesh(Mesh mesh, Transform meshTransform)
    {
        if (mesh == null) return 0;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        float volume = 0;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // ローカル座標 → ワールド座標へ変換
            Vector3 p1 = meshTransform.TransformPoint(vertices[triangles[i + 0]]);
            Vector3 p2 = meshTransform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 p3 = meshTransform.TransformPoint(vertices[triangles[i + 2]]);
            volume += SignedVolumeOfTriangle(p1, p2, p3);
        }

        return Mathf.Abs(volume);
    }

    float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float v321 = p3.x * p2.y * p1.z;
        float v231 = p2.x * p3.y * p1.z;
        float v312 = p3.x * p1.y * p2.z;
        float v132 = p1.x * p3.y * p2.z;
        float v213 = p2.x * p1.y * p3.z;
        float v123 = p1.x * p2.y * p3.z;
        return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
    }
}
