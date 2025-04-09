using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    public void easyStart()
    {
        CutNumManager.Instance.cutLimitTop = 4;
        CutNumManager.Instance.cutLimitSide = 1;
        SceneManager.LoadScene("MainGame");
    }

    public void hardStart()
    {
        CutNumManager.Instance.cutLimitTop = 6;
        CutNumManager.Instance.cutLimitSide = 2;
        SceneManager.LoadScene("MainGame");
    }
}
