using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    [SerializeField]
    GameObject RulePanel;
    public void EasyStart()
    {
        GameSettings.selectedMode = "Easy";
        SceneManager.LoadScene("MainGame");
        
    }

    public void HardStart()
    {
        GameSettings.selectedMode = "Hard";
        SceneManager.LoadScene("MainGame");

    }


    public void rule()
    {
        RulePanel.SetActive(true);
    }
    public void closeRule()
    {
        RulePanel.SetActive(false);
    }
}
