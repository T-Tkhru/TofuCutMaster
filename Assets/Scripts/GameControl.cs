using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using unityroom.Api;
using System;
using System.Collections;
using UnityEngine.UI;

public class GameControl : MonoBehaviour
{
    public GameObject planePrefab;  // 平面のプレハブ
    private Vector3 dragStartPos;   // ドラッグ開始位置
    private Vector3 dragEndPos;     // ドラッグ終了位置
    private bool isDragging = false; // ドラッグ中かどうか

    [SerializeField]
    private LineRenderer lineRendererPrefab;
    private LineRenderer currentLine;
    private List<LineRenderer> topLines = new List<LineRenderer>();
    private List<LineRenderer> sideLines = new List<LineRenderer>();

    private Vector3 TofuPos; // Tofuの位置
    private int cutCount = 0; // カット回数をカウント
    private string cameraPos = "Top"; // カメラの位置
    private Vector3 cameraPosSide = new Vector3(8, 3, 0); // カメラの位置（上から）
    private Vector3 cameraPosTop = new Vector3(8, 4.5f, 2); // カメラの位置（横から）
    private float cameraTopDistance = 1.25f; // カメラからTofuの面までの距離
    private float cameraSideDistance = 1.5f; // カメラからTofuの面までの距離
    private float startTime;
    private float playTime; // プレイ時間
    private int cutSuccessFlag;

    private bool result = false; // 結果表示フラグ
    public GameObject playingUI;
    [SerializeField]
    private GameObject timer; // 結果表示用のUIオブジェクト
    public GameObject resultUI; // 結果表示用のUIオブジェクト

    [SerializeField]
    private List<GameObject> resultTexts; // 表示したいテキストのリスト
    private int resultCount;
    private float resultScore;
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

        startTime = Time.time; // ゲーム開始時間を記録



    }

    async void Update()
    {
        var currentPlayingTime = Time.time - startTime; // 現在のプレイ時間を計算

        if (result) return; // 結果表示中は処理をスキップ
        var timerText = timer.GetComponent<TMPro.TMP_Text>();
        if (timerText != null)
        {
            timerText.text = $"タイム: {currentPlayingTime:F2}秒"; // タイマーを更新
        }
        else
        {
            Debug.LogWarning("Timer Textが見つかりません！");
        }
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
                if (distance < 0.8f)
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
                if (cameraPos == "Top")
                {
                    topLines.Add(currentLine); // 上からのカットラインを保存
                }
                else if (cameraPos == "Side")
                {
                    sideLines.Add(currentLine); // 横からのカットラインを保存
                }

                // カット回数をカウント
                cutCount++;
            }
        }
        //スペースキー押下時にカメラを移動
        if (cameraPos == "Top" && Input.GetKeyDown(KeyCode.Space))
        {
            MoveCamera();
            cameraPos = "Side";
            foreach (LineRenderer line in topLines)
            {
                line.gameObject.SetActive(false); // ラインを非アクティブにする
            }
            foreach (LineRenderer line in sideLines)
            {
                line.gameObject.SetActive(true); // ラインをアクティブにする
            }

        }
        else if (cameraPos == "Side" && Input.GetKeyDown(KeyCode.Space))
        {
            MoveCamera();
            cameraPos = "Top";
            foreach (LineRenderer line in sideLines)
            {
                line.gameObject.SetActive(false); // ラインを非アクティブにする
            }
            foreach (LineRenderer line in topLines)
            {
                line.gameObject.SetActive(true); // ラインをアクティブにする
            }
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            result = true; // 結果表示フラグを立てる
            Debug.Log("終了します");
            playingUI.SetActive(false); // UIを非アクティブにする
            StartCoroutine(MoveCameraSmoothly(new Vector3(8, 2, 0.4f), Quaternion.Euler(40, 0, 0), 1.3f));
            foreach (LineRenderer line in topLines)
            {
                line.gameObject.SetActive(false); // ラインを非アクティブにする
            }
            foreach (LineRenderer line in sideLines)
            {
                line.gameObject.SetActive(false); // ラインを非アクティブにする
            }
            await Task.Delay(2000); // 1秒待機
            Result(); // 結果を表示する関数を呼び出す
            await Task.Delay(2000);
            resultUI.SetActive(true); // 結果表示用のUIをアクティブにする
            await Task.Delay(1000);
            StartCoroutine(ShowResults());
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
        cutSuccessFlag = newPlane.GetComponent<SliceObjects>().Cutting();
        Destroy(newPlane, 0.01f);
    }

    // カメラを移動する関数
    void MoveCamera()
    {
        if (cameraPos == "Top")
        {
            Camera.main.transform.position = cameraPosSide;
            Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0); // カメラの向きを上に向ける
            Debug.Log("カメラが移動しました");
        }
        else if (cameraPos == "Side")
        {
            Camera.main.transform.position = cameraPosTop;
            Camera.main.transform.rotation = Quaternion.Euler(90, 0, 0); // カメラの向きを横に向ける
            Debug.Log("カメラが移動しました");
        }
    }

    private IEnumerator MoveCameraSmoothly(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        float timeElapsed = 0f;
        Vector3 startPosition = Camera.main.transform.position;
        Quaternion startRotation = Camera.main.transform.rotation;

        while (timeElapsed < duration)
        {
            Camera.main.transform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed / duration);
            Camera.main.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        Camera.main.transform.position = targetPosition;
        Camera.main.transform.rotation = targetRotation;
    }


    void Result()
    {
        Debug.Log("終了時間" + Time.time);
        playTime = Time.time - startTime; // プレイ時間を計算
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
        //スコアを計算
        resultCount = volumeList.Length;
        resultScore = MathF.Max(50 - Math.Abs(18 - volumeList.Length) * 5, 0) + MathF.Max(25 - standardDeviation * 1000, 0) + MathF.Max(20 - Mathf.Pow(playTime, 2) / 5, 0) + MathF.Max(20 - cutCount * 2, 0);
        Debug.Log("スコア: " + resultScore);

        if (resultScore >= 100)
        {
            Debug.Log("スコアが100以上です");
            UnityroomApiClient.Instance.SendScore(1, 100.00f, ScoreboardWriteMode.HighScoreDesc);
            UnityroomApiClient.Instance.SendScore(2, playTime, ScoreboardWriteMode.HighScoreAsc);
        }
        else
        {
            Debug.Log("スコアが100未満です");
            UnityroomApiClient.Instance.SendScore(1, resultScore, ScoreboardWriteMode.HighScoreDesc);
        }
    }
    private IEnumerator ShowResults()
    {
    //スコアを表示
        // 個数用
        var countText = resultTexts[0].GetComponent<TMPro.TMP_Text>();
        if (countText != null) countText.text = $"個数: {resultCount}個";

        // 秒数用
        var timeText = resultTexts[1].GetComponent<TMPro.TMP_Text>();
        if (timeText != null) timeText.text = $"タイム: {playTime:F2}秒";

        // スコア用
        var scoreText = resultTexts[2].GetComponent<TMPro.TMP_Text>();
        if (scoreText != null) scoreText.text = $"スコア: {resultScore:F2}点";
    foreach (GameObject textObj in resultTexts)
        {
            textObj.SetActive(true);
            yield return new WaitForSeconds(0.8f);
        }
    }
}
