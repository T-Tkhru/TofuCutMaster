using UnityEngine;

public class MouseTrailOnFace : MonoBehaviour
{
    private Camera mainCamera; // メインカメラ
    private LineRenderer lineRenderer; // ラインレンダラー
    private Vector3 lastPosition; // 最後に記録した位置

    // 描画開始フラグ
    private bool isDrawing = false;

    // Cubeのサイズ（1x1x1）を考慮したマウスの位置調整
    private float cubeSize = 1.0f;

    void Start()
    {
        mainCamera = Camera.main; // メインカメラを取得
        lineRenderer = GetComponent<LineRenderer>(); // LineRendererを取得
        lineRenderer.positionCount = 0; // 初期化
    }

    void Update()
    {
        // マウスが左クリックされている場合
        if (Input.GetMouseButton(0)) 
        {
            // カメラから見える面上にマウス位置を変換
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            // Cubeの面をYZ平面（Z=2）として考える
            Plane plane = new Plane(Vector3.forward, new Vector3(8, 0, 2)); // CubeのZ=2の面に対して

            float distance;
            if (plane.Raycast(ray, out distance)) 
            {
                // マウスの位置が平面上で交差する位置
                Vector3 point = ray.GetPoint(distance);

                // Cubeの面の範囲内に収める
                point.x = Mathf.Clamp(point.x, 8 - cubeSize / 2, 8 + cubeSize / 2); // Cubeの範囲に合わせる
                point.y = Mathf.Clamp(point.y, 0 - cubeSize / 2, 0 + cubeSize / 2); // Cubeの範囲に合わせる

                if (!isDrawing)
                {
                    // 最初の位置を記録
                    isDrawing = true;
                    lastPosition = point;
                    lineRenderer.positionCount = 1; // 最初の1点をセット
                    lineRenderer.SetPosition(0, lastPosition);
                }
                else
                {
                    // 線を追加していく
                    lineRenderer.positionCount++;
                    lineRenderer.SetPosition(lineRenderer.positionCount - 1, point);
                    lastPosition = point; // 最新の位置を更新
                }
            }
        }
        else
        {
            // マウスボタンを離したら描画終了
            if (isDrawing)
            {
                isDrawing = false;
            }
        }
    }
}
