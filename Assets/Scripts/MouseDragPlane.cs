using System.Threading.Tasks;
using UnityEngine;
using System.Linq;

public class MouseDragPlane : MonoBehaviour
{
    public GameObject planePrefab;  // 平面のプレハブ
    private Vector3 dragStartPos;   // ドラッグ開始位置
    private Vector3 dragEndPos;     // ドラッグ終了位置
    private bool isDragging = false; // ドラッグ中かどうか

    [SerializeField]
    private LineRenderer _lineRenderer;
    
    private Vector3 TofuPos; // Tofuの位置
    private int cutCount = 0; // カット回数をカウント
    private string cameraPos = "Top"; // カメラの位置
    private float cameraTopDistance = 1.75f; // カメラからTofuの面までの距離
    private float cameraSideDistance = 1.5f; // カメラからTofuの面までの距離
    public int cutLimitTop = 4; // 上からのカット回数制限
    public int cutLimitSide = 1; // 横からのカット回数制限

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

        cutLimitTop = CutNumManager.Instance.cutLimitTop;
        cutLimitSide = CutNumManager.Instance.cutLimitSide;

        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;
        _lineRenderer.widthMultiplier = 0.02f;
    }

    async void Update()
    {
        // マウスの左ボタンが押された時
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartPos = GetMouseWorldPosition();
            _lineRenderer.enabled = true;
        }

        // マウスが動いている間
        if (isDragging)
        {
            dragEndPos = GetMouseWorldPosition();

            _lineRenderer.SetPosition(0, dragStartPos);
            _lineRenderer.SetPosition(1, dragEndPos);
        }

        // マウスの左ボタンが離された時
        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                //スタートからエンドまでの距離を計算し、短い場合は処理をスキップ
                float distance = Vector3.Distance(dragStartPos, dragEndPos);
                if (distance < 0.1f)
                {
                    Debug.Log("ドラッグ距離が短いため、処理をスキップします。");
                    isDragging = false;
                    return;
                }
                CreatePlaneFromDrag(dragStartPos, dragEndPos, TofuPos);
                isDragging = false;

                // カット回数をカウント
                cutCount++;
                if (cameraPos == "Top" && cutCount >= cutLimitTop)
                {
                    MoveCamera();
                    cameraPos = "Side"; // カメラの位置を横に変更
                    cutCount = 0; // カット回数をリセット
                }
                else if (cameraPos == "Side" && cutCount >= cutLimitSide)
                {
                    Debug.Log("カット回数の上限に達しました。終了します");
                    await Task.Delay(100); 
                    GameObject[] sliceables = GameObject.FindGameObjectsWithTag("Sliceable");
                    float[] volumeList = new float[sliceables.Length];
                    foreach (GameObject obj in sliceables)
                    {
                        //Rigidbodyを取得し、kinematicをfalseに設定
                        Rigidbody rb = obj.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.isKinematic = false;
                        }
                        else
                        {
                            Debug.LogWarning("Rigidbodyが見つかりません: " + obj.name);
                        }
                        // TofuPieceスクリプトを取得し、体積を計算
                        TofuVolume tofuPiece = obj.GetComponent<TofuVolume>();
                        if (tofuPiece != null)
                        {
                            float volume = tofuPiece.VolumeOfMesh(obj.GetComponent<MeshFilter>().mesh, obj.transform);
                            volumeList[System.Array.IndexOf(sliceables, obj)] = volume;
                        }
                        else
                        {
                            Debug.LogWarning("TofuPieceスクリプトが見つかりません: " + obj.name);
                        }
                    }
                    Debug.Log("VolumeList: " + string.Join(", ", volumeList));
                    Debug.Log("Total Volume: " + volumeList.Sum());
                    Debug.Log("最小値: " + volumeList.Min());
                    Debug.Log("最大値: " + volumeList.Max());
                }
            }
        }
    }

    // スクリーン座標をワールド座標に変換するヘルパー関数
    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        if (cameraPos == "Top")
        {
            mousePosition.z = cameraTopDistance;  // カメラからTofuの面までの距離
        }
        else if (cameraPos == "Side")
        {
            mousePosition.z = cameraSideDistance;  // カメラからTofuの面までの距離
        }
        return Camera.main.ScreenToWorldPoint(mousePosition);
    }

    // ドラッグ開始位置と終了位置から平面を生成する
    void CreatePlaneFromDrag(Vector3 startPos, Vector3 endPos, Vector3 cubeCenterPos)
    {
        Vector3 centerPos = (startPos + endPos) / 2; // ドラッグの中心位置
        if (cameraPos == "Top")
        {
            centerPos.y = cubeCenterPos.y; 
        }
        else if (cameraPos == "Side")
        {
            centerPos.z = cubeCenterPos.z; 
        }
        
        Vector3 scale = new Vector3(0.2f, 0.1f, 0.2f);  // スケール

        GameObject newPlane = Instantiate(planePrefab, centerPos, Quaternion.identity);
        newPlane.transform.localScale = scale;

        // 平面の向き（ドラッグ範囲に合わせる）
        Vector3 direction = endPos - startPos;
        Quaternion rotation = Quaternion.LookRotation(direction);
        newPlane.transform.rotation = rotation;
        if (cameraPos == "Top")
        {
            newPlane.transform.Rotate(0, 0, 90); 
        }
        newPlane.GetComponent<SliceObjects>().Invoke(nameof(SliceObjects.Cutting), 0.01f);
        Destroy(newPlane, 0.01f); 
    }

    // カメラを移動する関数
    void MoveCamera()
    {
        Camera.main.transform.position = new Vector3(8, 2, 0); // カメラを上方向に移動
        Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0); // カメラの向きを上に向ける
        Debug.Log("カメラが移動しました");
    }
}
