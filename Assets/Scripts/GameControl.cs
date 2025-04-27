using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using unityroom.Api;
using System.Threading;
using Unity.VisualScripting;
using System;

public class GameControl : MonoBehaviour
{
    public GameObject planePrefab;  // 平面のプレハブ
    private Vector3 dragStartPos;   // ドラッグ開始位置
    private Vector3 dragEndPos;     // ドラッグ終了位置
    private bool isDragging = false; // ドラッグ中かどうか

    [SerializeField]
    private LineRenderer lineRendererPrefab;
    private LineRenderer currentLine;
    private List<LineRenderer> lines = new List<LineRenderer>();

    private Vector3 TofuPos; // Tofuの位置
    private int cutCount = 0; // カット回数をカウント
    private string cameraPos = "Top"; // カメラの位置
    private float cameraTopDistance = 1.25f; // カメラからTofuの面までの距離
    private float cameraSideDistance = 1.5f; // カメラからTofuの面までの距離
    public int cutLimitTop = 4; // 上からのカット回数制限
    public int cutLimitSide = 1; // 横からのカット回数制限
    private float playTime;
    private int cutSuccessFlag;
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



    }

    async void Update()
    {
        // マウスの左ボタンが押された時
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartPos = GetMouseWorldPosition();
            GameObject lineObj = Instantiate(lineRendererPrefab.gameObject);
            currentLine = lineObj.GetComponent<LineRenderer>();

            currentLine.positionCount = 2;
            currentLine.widthMultiplier = 0.01f;
            //非アクティブ
            currentLine.gameObject.SetActive(false);
        }

        // マウスが動いている間
        if (isDragging)
        {
            dragEndPos = GetMouseWorldPosition();

            currentLine.SetPosition(0, dragStartPos + Vector3.up * 0.01f);
            currentLine.SetPosition(1, dragEndPos + Vector3.up * 0.01f);
        }

        // マウスの左ボタンが離された時
        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                //スタートからエンドまでの距離を計算し、短い場合は処理をスキップ
                float distance = Vector3.Distance(dragStartPos, dragEndPos);
                if (distance < 0.7f)
                {
                    Debug.Log("ドラッグ距離が短いため、処理をスキップします。");
                    isDragging = false;
                    Destroy(currentLine.gameObject); // ラインを削除
                    return;
                }
                CreatePlaneFromDrag(dragStartPos, dragEndPos, TofuPos);
                if (cutSuccessFlag == 1)
                {
                    Debug.Log("カット失敗");
                    isDragging = false;
                    Destroy(currentLine.gameObject); // ラインを削除
                    return;
                }
                isDragging = false;
                currentLine.gameObject.SetActive(true);
                lines.Add(currentLine);

                // カット回数をカウント
                cutCount++;
                if (cameraPos == "Top" && cutCount >= cutLimitTop)
                {
                    MoveCamera();
                    cameraPos = "Side";
                    cutCount = 0;
                    foreach (LineRenderer line in lines)
                    {
                        line.gameObject.SetActive(false); // ラインを非アクティブにする
                    }

                }
                else if (cameraPos == "Side" && cutCount >= cutLimitSide)
                {
                    Debug.Log("カット回数の上限に達しました。終了します");
                    await Task.Delay(100);
                    foreach (LineRenderer line in lines)
                    {
                        line.gameObject.SetActive(false); // ラインを非アクティブにする
                    }
                    Result(); // 結果を表示する関数を呼び出す
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
        cutSuccessFlag= newPlane.GetComponent<SliceObjects>().Cutting();
        Destroy(newPlane, 0.01f);
    }

    // カメラを移動する関数
    void MoveCamera()
    {
        Camera.main.transform.position = new Vector3(8, 2, 0); // カメラを上方向に移動
        Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0); // カメラの向きを上に向ける
        Debug.Log("カメラが移動しました");
    }

    void Result()
    {
        playTime = Time.time;
        Debug.Log("プレイ時間: " + playTime + "秒");
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
        Debug.Log("個数: " + volumeList.Length);
        //標準偏差を計算
        float average = volumeList.Average();
        float variance = volumeList.Sum(v => Mathf.Pow(v - average, 2)) / volumeList.Length;
        float standardDeviation = Mathf.Sqrt(variance);
        Debug.Log("標準偏差: " + standardDeviation * 1000);
        float score = (50 - Math.Abs(18 - volumeList.Length) * 5) + (25 - standardDeviation * 1000) + (20 - Mathf.Pow(playTime, 2) / 5) + (20 - 5 * 2);//5*2はカット回数*2やからあとでカット回数取得できるよう調整
        Debug.Log("スコア: " + score);
        if (score >= 100)
        {
            Debug.Log("スコアが100以上です");
            UnityroomApiClient.Instance.SendScore(1, 100.00f, ScoreboardWriteMode.HighScoreDesc);
            UnityroomApiClient.Instance.SendScore(2, playTime, ScoreboardWriteMode.HighScoreAsc);
        }
        else
        {
            Debug.Log("スコアが100未満です");
            UnityroomApiClient.Instance.SendScore(1, score, ScoreboardWriteMode.HighScoreDesc);
        }
    }
}
