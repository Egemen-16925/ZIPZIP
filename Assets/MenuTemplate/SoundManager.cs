using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    [SerializeField] private List<Sound> soundSources;

    private const string SoundKey = "SoundKey";

    private void OnDisable()
    {
        if (PlayerPrefs.GetFloat(SoundKey) == 0f)
            PlayerPrefs.SetFloat(SoundKey, -1f);
    }

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (PlayerPrefs.GetFloat(SoundKey) == 0f)
            PlayerPrefs.SetFloat(SoundKey, .25f);

        else if (PlayerPrefs.GetFloat(SoundKey) == -1f)
            PlayerPrefs.SetFloat(SoundKey, 0f);
    }

    public void PlaySound(SoundType soundType)
    {
        soundSources.Find(x => x.soundType == soundType).audioSource.Play();
    }

    public void StopSound(SoundType soundType)
    {
        soundSources.Find(x => x.soundType == soundType).audioSource.Stop();
    }

}

[System.Serializable]
public struct Sound
{
    public AudioSource audioSource;
    public SoundType soundType;
}

[System.Serializable]
public enum SoundType
{
    GameBackgroundMusic,
    TapSound,
    HitSound,
    DieSound
}