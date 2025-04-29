using UnityEngine;
using UnityEngine.UI;

public class SoundButtonController : MonoBehaviour
{
    public Text buttonText;

    public void OnClickSoundButton()
    {
        UpdateLabel();
    }

    void Start()
    {
        UpdateLabel();
    }

    void UpdateLabel()
    {
    }
}
