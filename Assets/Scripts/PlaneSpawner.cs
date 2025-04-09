using UnityEngine;

public class PlaneSpawner : MonoBehaviour
{
    public GameObject planePrefab; // Inspectorで設定する

    public void SpawnPlane()
    {
        if (planePrefab == null)
        {
            Debug.LogError("planePrefab が設定されていません！");
            return;
        }

        // 生成位置（適当に調整）
        Vector3 spawnPosition = new Vector3(0, 0, 0);

        // ランダムな回転 (0〜360度でY軸をランダム回転)
        Quaternion randomRotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), 0);

        // Planeを生成
        Instantiate(planePrefab, spawnPosition, randomRotation);
    }
}
