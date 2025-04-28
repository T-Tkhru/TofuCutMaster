using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    [SerializeField]
    GameObject RulePanel;
    public void easyStart()
    {
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
