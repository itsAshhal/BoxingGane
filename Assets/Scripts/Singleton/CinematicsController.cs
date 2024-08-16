using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace SimpleBoxing
{
    public class CinematicsController : Singleton<CinematicsController>
    {
        [SerializeField] CinemachineVirtualCamera m_mainCamera;
        public CinemachineVirtualCamera MainCamera
        {
            get { return m_mainCamera; }
            set { m_mainCamera = value; }
        }
        [Header("Noise Profiles")]
        public float M_amplitudeGain_Normal;
        public float M_amplitudeGain_Low;
        public float M_amplitudeGain_Medium;
        public float M_amplitudeGain_High;
        public float ShakeDuration;

        public NoiseSettings PlayerStunNoise;


        public enum ShakeLevel
        {
            Low, Medium, High
        }
        public ShakeLevel M_ShakeLevel { get; set; }

        private void Start()
        {
            DoShake(ShakeLevel.Low);
            StunPlayer(false);
        }

        public void DoShake(ShakeLevel shakeLevel)
        {
            StartCoroutine(DoShakeCoroutine(shakeLevel));
        }
        IEnumerator DoShakeCoroutine(ShakeLevel shakeLevel)
        {
            M_ShakeLevel = shakeLevel;
            float noiseSettings = 0f;
            CinemachineBasicMultiChannelPerlin noiseChannel = m_mainCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (M_ShakeLevel == ShakeLevel.Low) noiseSettings = M_amplitudeGain_Low;
            else if (M_ShakeLevel == ShakeLevel.Medium) noiseSettings = M_amplitudeGain_Medium;
            else if (M_ShakeLevel == ShakeLevel.High) noiseSettings = M_amplitudeGain_High;

            noiseChannel.m_AmplitudeGain = noiseSettings;

            // wait for the duration
            yield return new WaitForSeconds(ShakeDuration);

            // get the camera back to normal state
            noiseChannel.m_AmplitudeGain = M_amplitudeGain_Normal;


        }


        /// <summary>
        /// When the enemy breaks the block of the player, use this method to apply the stun effect (camera animation of course)
        /// </summary>
        public void StunPlayer(bool isStunned = false)
        {
            Debug.Log($"Camera stunned called {isStunned}");
            Animator anim = m_mainCamera.GetComponent<Animator>();
            anim.enabled = isStunned;
            anim.SetBool("IsStunned", isStunned);
        }


    }
}
