using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using unityroom.Api;
using System;
using System.Collections;

public class GameControl : MonoBehaviour
{
    // 定数
    private const string CAMERA_POS_TOP = "Top";
    private const string CAMERA_POS_SIDE = "Side";
    private const float MIN_DRAG_DISTANCE = 0.8f;
    private const float LINE_WIDTH = 0.01f;
    
    // ゲームオブジェクト
    public GameObject planePrefab;  // 平面のプレハブ
    [SerializeField] private LineRenderer lineRendererPrefab;
    public GameObject startUI;      // スタート画面のUIオブジェクト
    public GameObject playingUI;
    [SerializeField] private GameObject timer;
    public GameObject resultUI;     // 結果表示用のUIオブジェクト
    [SerializeField] private List<GameObject> resultTexts;

    // ドラッグ関連
    private Vector3 dragStartPos;   // ドラッグ開始位置
    private Vector3 dragEndPos;     // ドラッグ終了位置
    private bool isDragging = false; // ドラッグ中かどうか
    
    // ライン関連
    private LineRenderer currentLine;
    private List<LineRenderer> topLines = new List<LineRenderer>();
    private List<LineRenderer> sideLines = new List<LineRenderer>();

    // カメラ関連
    private Vector3 TofuPos;                                       // Tofuの位置
    private string cameraPos = CAMERA_POS_TOP;                     // カメラの位置
    private Vector3 cameraPosSide = new Vector3(8, 3, 0);          // カメラの位置（上から）
    private Vector3 cameraPosTop = new Vector3(8, 4.5f, 2);        // カメラの位置（横から）
    private float cameraTopDistance = 1.25f;                       // カメラからTofuの面までの距離
    private float cameraSideDistance = 1.5f;                       // カメラからTofuの面までの距離
    
    // ゲーム状態
    private bool start = false;       // ゲーム開始フラグ
    private float startTime;          // ゲーム開始時間
    private float playTime;           // プレイ時間
    private int cutCount = 0;         // カット回数をカウント
    private int cutSuccessFlag;       // カット成功フラグ
    private bool result = false;      // 結果表示フラグ

    // 結果関連
    private int resultCount;          // 結果の個数
    private float resultScore;        // 結果のスコア
    private float resultPercent;      // 結果の正確性パーセント
    
    // オーディオ関連
    public AudioClip cutSE;          // カット音
    public AudioClip drumrollSE;     // ドラムロール音
    public AudioClip resultSE;       // 結果音
    public AudioClip resultSE2;      // 結果音2
    private AudioSource audioSource;  // 音声再生用のAudioSource
    private GameObject BGM;           // BGM管理オブジェクト

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
        BGM = GameObject.Find("BGM"); // BGMオブジェクトを取得
        audioSource = GetComponent<AudioSource>(); // AudioSourceを取得
        Debug.Log("Mode: " + GameSettings.selectedMode); // 選択されたモードを表示
    }
    void StartGame()
    {
        start = true; // ゲーム開始フラグを立てる
        playingUI.SetActive(true); // UIをアクティブにする
        startUI.SetActive(false); // スタート画面のUIを非アクティブにする
        startTime = Time.time; // ゲーム開始時間を記録
    }
    void Update()
    {
        // 結果表示中は処理をスキップ
        if (result) return;
        
        // ゲーム開始前の処理
        if (!start)
        {
            CheckForGameStart();
            return;
        }
        
        // タイマー表示の更新
        UpdateTimer();
        
        // 入力処理
        HandleDragInput();
        
        // カメラ切り替え処理
        HandleCameraSwitch();
        
        // ゲーム終了処理
        CheckForGameEnd();
    }

    // ゲーム開始のチェック
    private void CheckForGameStart()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            StartGame();
        }
    }

    // タイマー表示の更新
    private void UpdateTimer()
    {
        var currentPlayingTime = Time.time - startTime;
        var timerText = timer.GetComponent<TMPro.TMP_Text>();
        if (timerText != null)
        {
            timerText.text = $"タイム: {currentPlayingTime:F2}秒";
        }
        else
        {
            Debug.LogWarning("Timer Textが見つかりません！");
        }
    }

    // ドラッグ入力の処理
    private void HandleDragInput()
    {
        // マウスの左ボタンが押された時
        if (Input.GetMouseButtonDown(0))
        {
            StartDragging();
        }

        // マウスが動いている間
        if (isDragging)
        {
            UpdateDragLine();
        }

        // マウスの左ボタンが離された時
        if (Input.GetMouseButtonUp(0))
        {
            EndDragging();
        }
    }

    // ドラッグ開始処理
    private void StartDragging()
    {
        isDragging = true;
        dragStartPos = GetMouseWorldPosition();
        
        // ラインレンダラーの初期化
        GameObject lineObj = Instantiate(lineRendererPrefab.gameObject);
        currentLine = lineObj.GetComponent<LineRenderer>();
        currentLine.positionCount = 2;
        currentLine.widthMultiplier = LINE_WIDTH;
        currentLine.gameObject.SetActive(false);
    }

    // ドラッグ中の処理
    private void UpdateDragLine()
    {
        dragEndPos = GetMouseWorldPosition();
        
        // ラインの位置を更新
        currentLine.SetPosition(0, dragStartPos + Vector3.up * 0.01f);
        currentLine.SetPosition(1, dragEndPos + Vector3.up * 0.01f);
    }

    // ドラッグ終了処理
    private void EndDragging()
    {
        if (!isDragging) return;
        
        // ドラッグ距離のチェック
        float distance = Vector3.Distance(dragStartPos, dragEndPos);
        if (distance < MIN_DRAG_DISTANCE)
        {
            CancelDragging("ドラッグ距離が短いため、処理をスキップします。");
            return;
        }
        
        // 平面を生成してカット処理
        CreatePlaneFromDrag(dragStartPos, dragEndPos, TofuPos);
        
        // カット失敗時の処理
        if (cutSuccessFlag == 1)
        {
            CancelDragging("カット失敗");
            return;
        }
        
        // カット成功時の処理
        CompleteDragging();
    }

    // ドラッグをキャンセル
    private void CancelDragging(string message)
    {
        Debug.Log(message);
        isDragging = false;
        Destroy(currentLine.gameObject);
    }

    // ドラッグを完了
    private void CompleteDragging()
    {
        audioSource.PlayOneShot(cutSE);
        isDragging = false;
        currentLine.gameObject.SetActive(true);
        
        // カメラ位置に応じてラインを保存
        if (cameraPos == CAMERA_POS_TOP)
        {
            topLines.Add(currentLine);
        }
        else if (cameraPos == CAMERA_POS_SIDE)
        {
            sideLines.Add(currentLine);
        }
        
        // カット回数をカウント
        cutCount++;
    }

    // カメラ切り替えの処理
    private void HandleCameraSwitch()
    {
        if (cameraPos == CAMERA_POS_TOP && Input.GetKeyDown(KeyCode.Space))
        {
            SwitchCameraToSide();
        }
        else if (cameraPos == CAMERA_POS_SIDE && Input.GetKeyDown(KeyCode.Space))
        {
            SwitchCameraToTop();
        }
    }

    // カメラを横向きに切り替え
    private void SwitchCameraToSide()
    {
        MoveCamera();
        cameraPos = CAMERA_POS_SIDE;
        
        // ラインの表示/非表示を切り替え
        SetLinesVisibility(topLines, false);
        SetLinesVisibility(sideLines, true);
    }

    // カメラを上向きに切り替え
    private void SwitchCameraToTop()
    {
        MoveCamera();
        cameraPos = CAMERA_POS_TOP;
        
        // ラインの表示/非表示を切り替え
        SetLinesVisibility(sideLines, false);
        SetLinesVisibility(topLines, true);
    }

    // ラインの表示/非表示を設定
    private void SetLinesVisibility(List<LineRenderer> lines, bool isVisible)
    {
        foreach (LineRenderer line in lines)
        {
            line.gameObject.SetActive(isVisible);
        }
    }

    // ゲーム終了のチェック
    private void CheckForGameEnd()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            EndGame();
        }
    }

    // ゲーム終了処理
    private void EndGame()
    {
        // 時間計測を終了
        playTime = Time.time - startTime;
        
        // BGM停止・効果音再生
        BGM.GetComponent<AudioSource>().Stop();
        audioSource.PlayOneShot(drumrollSE);
        
        // 結果表示フラグを立てる
        result = true;
        
        Debug.Log("終了します");
        
        // UI関連の処理
        playingUI.SetActive(false);
        
        // すべてのラインを非表示
        SetLinesVisibility(topLines, false);
        SetLinesVisibility(sideLines, false);
        
        // 結果表示シーケンスを開始
        StartCoroutine(ShowResultSequence());
    }

    private IEnumerator ShowResultSequence()
    {

        // カメラ移動（非同期）
        yield return StartCoroutine(MoveCameraSmoothly(new Vector3(8, 2, 0.4f), Quaternion.Euler(40, 0, 0), 1.3f));

        yield return new WaitForSeconds(2f);
        Result(); // 結果を表示する関数を呼び出す
        yield return new WaitForSeconds(2f);
        resultUI.SetActive(true); // 結果表示UIを有効化
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(ShowResults());
    }

    // スクリーン座標をワールド座標に変換するヘルパー関数
    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        if (cameraPos == CAMERA_POS_TOP)
        {
            mousePosition.z = cameraTopDistance;  // カメラからTofuの面までの距離
        }
        else if (cameraPos == CAMERA_POS_SIDE)
        {
            mousePosition.z = cameraSideDistance;  // カメラからTofuの面までの距離
        }
        return Camera.main.ScreenToWorldPoint(mousePosition);
    }

    // ドラッグ開始位置と終了位置から平面を生成する
    void CreatePlaneFromDrag(Vector3 startPos, Vector3 endPos, Vector3 cubeCenterPos)
    {
        // 定数
        const float PLANE_SCALE_X = 0.2f;
        const float PLANE_SCALE_Y = 0.1f;
        const float PLANE_SCALE_Z = 0.2f;
        const float PLANE_LIFETIME = 0.01f;
        
        // ドラッグの中心位置を計算
        Vector3 centerPos = (startPos + endPos) / 2;
        
        // カメラ位置に応じて座標を調整
        AdjustPositionBasedOnCamera(ref centerPos, cubeCenterPos);

        // 平面のスケールを設定
        Vector3 scale = new Vector3(PLANE_SCALE_X, PLANE_SCALE_Y, PLANE_SCALE_Z);
        
        // 平面を生成
        GameObject newPlane = Instantiate(planePrefab, centerPos, Quaternion.identity);
        newPlane.transform.localScale = scale;

        // 平面の向きを設定
        SetPlaneRotation(newPlane, startPos, endPos);
        
        // カット処理を実行
        cutSuccessFlag = newPlane.GetComponent<SliceObjects>().Cutting();
        
        // 平面を短時間で削除
        Destroy(newPlane, PLANE_LIFETIME);
    }
    
    // カメラ位置に応じて座標を調整
    private void AdjustPositionBasedOnCamera(ref Vector3 position, Vector3 centerPos)
    {
        if (cameraPos == CAMERA_POS_TOP)
        {
            position.y = centerPos.y;
        }
        else if (cameraPos == CAMERA_POS_SIDE)
        {
            position.z = centerPos.z;
        }
    }
    
    // 平面の回転を設定
    private void SetPlaneRotation(GameObject plane, Vector3 startPos, Vector3 endPos)
    {
        Vector3 direction = endPos - startPos;
        Quaternion rotation = Quaternion.LookRotation(direction);
        plane.transform.rotation = rotation;
        
        if (cameraPos == CAMERA_POS_TOP)
        {
            plane.transform.Rotate(0, 0, 90);
        }
    }

    // カメラを移動する関数
    void MoveCamera()
    {
        if (cameraPos == CAMERA_POS_TOP)
        {
            // トップからサイドへ
            MoveCameraTo(cameraPosSide, Quaternion.Euler(0, 0, 0));
        }
        else if (cameraPos == CAMERA_POS_SIDE)
        {
            // サイドからトップへ
            MoveCameraTo(cameraPosTop, Quaternion.Euler(90, 0, 0));
        }
    }

    // カメラを指定位置に移動
    private void MoveCameraTo(Vector3 position, Quaternion rotation)
    {
        Camera.main.transform.position = position;
        Camera.main.transform.rotation = rotation;
        Debug.Log("カメラが移動しました");
    }

    // カメラをスムーズに移動
    private IEnumerator MoveCameraSmoothly(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        Debug.Log("カメラがスムーズに移動します");
        float timeElapsed = 0f;
        Vector3 startPosition = Camera.main.transform.position;
        Quaternion startRotation = Camera.main.transform.rotation;

        while (timeElapsed < duration)
        {
            float t = timeElapsed / duration;
            Camera.main.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            Camera.main.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // 最終位置を確実に設定
        MoveCameraTo(targetPosition, targetRotation);
    }

    // 物理演算の有効化とスライスの評価を行う
    void Result()
    {
        Debug.Log("終了時間" + Time.time);
        Debug.Log("プレイ時間: " + playTime + "秒");
        
        // スライスされたオブジェクトを取得し評価
        float[] volumeList = EvaluateSlicedObjects();
        
        // 標準偏差を計算
        float standardDeviation = CalculateStandardDeviation(volumeList);
        Debug.Log("標準偏差: " + standardDeviation * 1000);
        
        // パーセント表示の計算
        resultPercent = 100 / (1 + standardDeviation * 20);
        
        // 結果の数をセット
        resultCount = volumeList.Length;
        
        // スコアを計算して送信
        CalculateAndSubmitScore(volumeList.Length, standardDeviation);
        
        Debug.Log("スコア: " + resultScore);
    }

    // スライスされたオブジェクトを評価する
    private float[] EvaluateSlicedObjects()
    {
        GameObject[] sliceables = GameObject.FindGameObjectsWithTag("Sliceable");
        float[] volumeList = new float[sliceables.Length];
        
        for (int i = 0; i < sliceables.Length; i++)
        {
            GameObject obj = sliceables[i];
            
            // Rigidbodyを有効化
            EnableRigidbody(obj);
            
            // 体積を計算
            volumeList[i] = CalculateObjectVolume(obj);
        }
        
        Debug.Log("VolumeList: " + string.Join(", ", volumeList));
        Debug.Log("個数: " + volumeList.Length);
        
        return volumeList;
    }
    
    // Rigidbodyを有効化する
    private void EnableRigidbody(GameObject obj)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        else
        {
            Debug.LogWarning("Rigidbodyが見つかりません: " + obj.name);
        }
    }
    
    // オブジェクトの体積を計算する
    private float CalculateObjectVolume(GameObject obj)
    {
        TofuVolume tofuPiece = obj.GetComponent<TofuVolume>();
        if (tofuPiece != null)
        {
            return tofuPiece.VolumeOfMesh(obj.GetComponent<MeshFilter>().mesh, obj.transform);
        }
        else
        {
            Debug.LogWarning("TofuPieceスクリプトが見つかりません: " + obj.name);
            return 0;
        }
    }
    
    // 標準偏差を計算する
    private float CalculateStandardDeviation(float[] values)
    {
        if (values.Length == 0) return 0;
        
        float average = values.Average();
        float variance = values.Sum(v => Mathf.Pow(v - average, 2)) / values.Length;
        return Mathf.Sqrt(variance);
    }
    
    // スコアを計算して送信する
    private void CalculateAndSubmitScore(int pieceCount, float standardDeviation)
    {
        if (GameSettings.selectedMode == "Easy")
        {
            CalculateEasyModeScore(pieceCount, standardDeviation);
        }
        else if (GameSettings.selectedMode == "Hard")
        {
            CalculateHardModeScore(pieceCount, standardDeviation);
        }
    }
    
    // イージーモードのスコア計算
    private void CalculateEasyModeScore(int pieceCount, float standardDeviation)
    {
        // 各項目のスコアを計算
        float countScore = MathF.Max(50 - Math.Abs(18 - pieceCount) * 5, 0);
        float accuracyScore = MathF.Max(25 - standardDeviation * 1000, 0);
        float timeScore = MathF.Max(20 - Mathf.Pow(playTime, 2) / 5, 0);
        float cutScore = MathF.Max(20 - cutCount * 2, 0);
        
        // 合計スコアを計算
        resultScore = countScore + accuracyScore + timeScore + cutScore;
        
        // スコアを送信
        float scoreToSubmit = resultScore >= 100 ? 100.0f : resultScore;
        UnityroomApiClient.Instance.SendScore(1, scoreToSubmit, ScoreboardWriteMode.HighScoreDesc);
    }
    
    // ハードモードのスコア計算
    private void CalculateHardModeScore(int pieceCount, float standardDeviation)
    {
        // 各項目のスコアを計算
        float countScore = MathF.Max(50 - Math.Abs(48 - pieceCount) * 5, 0);
        float accuracyScore = MathF.Max(25 - standardDeviation * 1000, 0);
        float timeScore = MathF.Max(20 - Mathf.Pow(playTime, 2) / 10, 0);
        float cutScore = MathF.Max(26 - cutCount * 2, 0);
        
        // 合計スコアを計算
        resultScore = countScore + accuracyScore + timeScore + cutScore;
        
        // スコアを送信
        float scoreToSubmit = resultScore >= 100 ? 100.0f : resultScore;
        UnityroomApiClient.Instance.SendScore(2, scoreToSubmit, ScoreboardWriteMode.HighScoreDesc);
    }

    private IEnumerator ShowResults()
    {
        // 結果テキストを設定
        SetupResultTexts();
        
        // テキストを順番に表示
        yield return DisplayResultTextsSequentially();
    }
    
    // 結果テキストの設定
    private void SetupResultTexts()
    {
        // 個数
        UpdateResultText(0, $"豆腐の数: {resultCount}個");
        
        // 正確性
        UpdateResultText(1, $"正確性: {resultPercent:F2}%");
        
        // カット回数
        UpdateResultText(2, $"カット回数: {cutCount}回");
        
        // 秒数
        UpdateResultText(3, $"タイム: {playTime:F2}秒");
        
        // スコア
        UpdateResultText(4, $"スコア: {resultScore:F2}点");
    }
    
    // 指定indexのテキストを更新
    private void UpdateResultText(int index, string text)
    {
        if (index >= 0 && index < resultTexts.Count)
        {
            var textComponent = resultTexts[index].GetComponent<TMPro.TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }
    }
    
    // テキストを順番に表示
    private IEnumerator DisplayResultTextsSequentially()
    {
        const float SCORE_DISPLAY_DELAY = 1.2f;
        const float NORMAL_DISPLAY_DELAY = 0.8f;
        
        foreach (GameObject textObj in resultTexts)
        {
            // スコア表示の場合
            if (textObj.name == "Score")
            {
                audioSource.PlayOneShot(resultSE2);
                textObj.SetActive(true);
                yield return new WaitForSeconds(SCORE_DISPLAY_DELAY);
            }
            // ボタングループの場合
            else if (textObj.name == "ButtonGroup")
            {
                textObj.SetActive(true);
                yield return new WaitForSeconds(NORMAL_DISPLAY_DELAY);
            }
            // その他の結果表示の場合
            else
            {
                audioSource.PlayOneShot(resultSE);
                textObj.SetActive(true);
                yield return new WaitForSeconds(NORMAL_DISPLAY_DELAY);
            }
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // 現在のシーンを再読み込み
    }
    public void Title()
    {
        // タイトル画面に戻る処理をここに追加
        SceneManager.LoadScene("StartMenu");

    }
}
