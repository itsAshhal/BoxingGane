using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleBoxing.Audio
{
    public enum PunchSound
    {
        Normal, Hard, Block, None
    }
    public class AudioController : MonoBehaviour
    {
        public AudioSource NormalPunchHit;
        public AudioSource HardPunchHit;
        public AudioSource Block;

        public static AudioController Instance;
        private void Awake()
        {
            if (Instance != this && Instance != null) Destroy(this);
            else Instance = this;
        }


        public void PlaySound(PunchSound punchSound = PunchSound.Block)
        {
            switch (punchSound)
            {
                case PunchSound.Normal:
                    NormalPunchHit.Play();
                    break;
                case PunchSound.Hard:
                    HardPunchHit.Play();
                    break;
                case PunchSound.Block:
                    Block.Play();
                    break;

            }
        }
    }
}
