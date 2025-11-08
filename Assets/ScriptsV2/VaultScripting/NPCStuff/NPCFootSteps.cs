using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCFootsteps : MonoBehaviour
{
    public AudioSource footstepSource; // optional
    public AudioClip footstepClip;     // optional

    // This must match the AnimationEvent name exactly
    public void Footsteps()
    {
        // Play sound, spawn particles, or just debug
        

        if (footstepSource && footstepClip)
        {
            footstepSource.PlayOneShot(footstepClip);
        }
    }

    // If you have separate events for strafing or backward run:
    public void FootstepsStrafe()
    {
     
        if (footstepSource && footstepClip) footstepSource.PlayOneShot(footstepClip);
    }

    public void FootstepsRunBack()
    {
      
        if (footstepSource && footstepClip) footstepSource.PlayOneShot(footstepClip);
    }
}

