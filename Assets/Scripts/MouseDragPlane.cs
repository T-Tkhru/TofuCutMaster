using UnityEngine;

public class MouseDragPlane : MonoBehaviour
{
    public GameObject planePrefab;  // 平面のプレハブ
    private Vector3 dragStartPos;   // ドラッグ開始位置
    private Vector3 dragEndPos;     // ドラッグ終了位置
    private bool isDragging = false; // ドラッグ中かどうか
    
    private Vector3 TofuPos; // Tofuの位置

    void Start()
    {
        // Tofuの位置を取得
        GameObject targetCube = GameObject.Find("Tofu");
        if (targetCube != null)
        {
            TofuPos = targetCube.transform.position;
        }
        else
        {
            Debug.LogError("Tofuオブジェクトが見つかりません！");
        }
    }

    void Update()
    {
        // マウスの左ボタンが押された時
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartPos = GetMouseWorldPosition();
            // ドラッグ開始位置に点を表示
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = dragStartPos;
            sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        }

        // マウスが動いている間
        if (isDragging)
        {
            dragEndPos = GetMouseWorldPosition();
        }

        // マウスの左ボタンが離された時
        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = dragEndPos;
                sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                CreatePlaneFromDrag(dragStartPos, dragEndPos, TofuPos);
                isDragging = false;
            }
        }
    }

    // スクリーン座標をワールド座標に変換するヘルパー関数
    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = 1.5f;  // カメラからの距離を設定（調整が必要な場合があります）        
        return Camera.main.ScreenToWorldPoint(mousePosition);
    }

    // ドラッグ開始位置と終了位置から平面を生成する
    void CreatePlaneFromDrag(Vector3 startPos, Vector3 endPos, Vector3 cubeCenterPos)
    {
        Vector3 centerPos = (startPos + endPos) / 2; // ドラッグの中心位置
        centerPos.z = cubeCenterPos.z; // Y座標をCubeのY座標に合わせる
        Vector3 scale = new Vector3(0.2f, 0.1f, 0.2f);  // スケール

        GameObject newPlane = Instantiate(planePrefab, centerPos, Quaternion.identity);
        newPlane.transform.localScale = scale;

        // 平面の向き（ドラッグ範囲に合わせる）
        Vector3 direction = endPos - startPos;
        Quaternion rotation = Quaternion.LookRotation(direction);
        newPlane.transform.rotation = rotation;

        newPlane.GetComponent<SliceObjects>().Invoke(nameof(SliceObjects.Cutting), 0.1f);
        Destroy(newPlane, 0.2f); 

    }
}
