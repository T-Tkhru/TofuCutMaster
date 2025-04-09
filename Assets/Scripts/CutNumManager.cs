using UnityEngine;

public class CutNumManager : MonoBehaviour
{
    public static CutNumManager Instance;

    public int cutLimitTop = 4;
    public int cutLimitSide = 1;

    void Awake()
    {
        // シングルトンパターン
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
