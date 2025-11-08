using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletAudioManager : MonoBehaviour
{
    public static AudioSource Instance { get; private set; }

    private void Awake()
    {
        Instance = GetComponent<AudioSource>();
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

}