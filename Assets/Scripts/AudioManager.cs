using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Image volumeIconImage; // 子Image (VolumeIcon)

    public Sprite muteSprite;
    public Sprite lowSprite;
    public Sprite mediumSprite;
    public Sprite highSprite;

    private static int currentVolumeLevel = 3; // 初期: High

    void Start()
    {
        UpdateSound();
    }

    public void OnSoundButtonClicked()
    {
        currentVolumeLevel = (currentVolumeLevel + 1) % 4;
        UpdateSound();
    }

    private void UpdateSound()
    {
        switch (currentVolumeLevel)
        {
            case 0:
                audioMixer.SetFloat("Master", -80f);
                volumeIconImage.sprite = muteSprite;
                break;
            case 1:
                audioMixer.SetFloat("Master", -30f);
                volumeIconImage.sprite = lowSprite;
                break;
            case 2:
                audioMixer.SetFloat("Master", -15f);
                volumeIconImage.sprite = mediumSprite;
                break;
            case 3:
                audioMixer.SetFloat("Master", 0f);
                volumeIconImage.sprite = highSprite;
                break;
        }
    }
}
