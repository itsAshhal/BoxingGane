using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleBoxing.Audio
{
    public class PersistentAudioSource : MonoBehaviour
    {
        // Use this to ensure the GameObject only exists once
        private static PersistentAudioSource instance = null;

        void Awake()
        {
            if (instance == null)
            {
                // Set this instance as the static instance and keep it across scenes
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                // Destroy any duplicates that might have been created during scene loads
                Destroy(gameObject);
            }
        }
    }
}