using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    [SerializeField] private AudioClip agree;
    [SerializeField] private AudioClip disAgree;
    [SerializeField] private AudioSource sfxSource;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void PlayAgree()
    {
        if (sfxSource != null && agree != null )
        {
          sfxSource.PlayOneShot(agree);
        }
    }
    public void PlayDisAgree()
    {
        if (sfxSource != null && disAgree != null)
        {
            sfxSource.PlayOneShot(disAgree);
        }
    }
}
