using System;
using System.Collections;
using System.Collections.Generic;
using SimpleBoxing.Enemy;
using SimpleBoxing.Input;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting;
using static UnityEngine.Rendering.DebugUI;
using Random = UnityEngine.Random;

namespace SimpleBoxing.Player
{
    /// <summary>
    /// Main controller for player boxing
    /// </summary>
    public class PlayerBoxingController : MonoBehaviour
    {
        [SerializeField] int m_totalBlockAnimations;
        private Animator m_anim;
        [SerializeField] AnimationCurve m_animationCurve;
        [SerializeField] float m_desiredDuration = 1f;
        public float m_timer = 0f;
        [SerializeField] Transform m_rightHandTarget;
        [SerializeField] Transform m_leftHandTarget;

        [SerializeField] Transform m_rightHandPunchTarget;
        [SerializeField] Transform m_leftHandPunchTarget;

        private Vector3 m_rightHandStartingPosition;
        private Vector3 m_leftHandStartingPosition;

        [SerializeField] float m_distanceThreshold;
        [SerializeField] float m_waitTimeForComingBack;
        private float m_waitTimer = 0f;
        private bool m_shouldGoBack = false;

        [SerializeField] Animator m_rightHandAnim;
        [SerializeField] Animator m_leftHandAnim;

        public CinematicsController.ShakeLevel M_ShakeLevel;
        public float ShakeDuration = .25f;

        [SerializeField] Transform m_particleSpawnTransform;
        public int TotalPunches = 0;
        [Tooltip("So if the player does a punch, he can perform another punch after this seconds")]
        [SerializeField] float m_punchesDelay = 1f;
        [SerializeField] SphereCollider m_hitArea;
        public bool m_isBlocking = false;
        [SerializeField] float m_blockRecoveryTime = 1f;

        public enum PunchState
        {
            NormalPunchRight, NormalPunchLeft, SpecialPunchRight, SpecialPunchLeft, None
        }

        public PunchState M_PunchState;
        public enum PlayerAnimationState
        {
            RightHand, LeftHand, None
        }
        public PlayerAnimationState M_PlayerAnimationState;
        public bool m_canPunch = true;

        void Awake()
        {
            // we need to subscribe the touch events from the InputController
            InputController.Instance.OnLeftScreenPressed.AddListener(OnLeftScreenPressed);
            InputController.Instance.OnRightScreenPressed.AddListener(OnRightScreenPressed);
            InputController.Instance.OnMiddleScreenPressed.AddListener(OnMiddleScreenPressed);
            InputController.Instance.OnLeftScreenHold.AddListener(OnLeftScreenHold);
            InputController.Instance.OnRightScreenHold.AddListener(OnRightScreenHold);
            InputController.Instance.OnFingerReleased.AddListener(OnFingerReleased);


            m_anim = GetComponent<Animator>();

            M_PlayerAnimationState = PlayerAnimationState.None;
            M_PunchState = PunchState.None;

            m_rightHandStartingPosition = m_rightHandTarget.position;
            m_leftHandStartingPosition = m_leftHandTarget.position;

            m_shouldGoBack = false;

            m_rightHintDefaultPosition = m_rightHint.position;
            m_leftHintDefaultPosition = m_leftHint.position;
        }


        private void Update()
        {
            Debug.Log($"HitArea for player {m_hitArea.enabled}, block -> {m_isBlocking}");
            if (M_PlayerAnimationState == PlayerAnimationState.None) return;


            if (M_PlayerAnimationState == PlayerAnimationState.RightHand) RightPunch();
            if (M_PlayerAnimationState == PlayerAnimationState.LeftHand) LeftPunch();



        }

        #region ManualAnimationControl

        public void RightPunch(bool special = false)
        {
            // so when  the distance is reached, we should get back to the original position as well
            float distance = Vector3.Distance(m_rightHandTarget.position, m_rightHandPunchTarget.position);

            if (!m_shouldGoBack)
            {
                m_timer += Time.deltaTime;
                float percentage = m_timer / m_desiredDuration;


                m_rightHandTarget.position = Vector3.Lerp(m_rightHandStartingPosition, m_rightHandPunchTarget.position,
                    Mathf.SmoothStep(0f, 1f, m_animationCurve.Evaluate(percentage)));

                if (distance <= m_distanceThreshold) m_shouldGoBack = true;

            }
            else if (m_shouldGoBack)
            {
                // the distance has been reached
                // move the target backwards now
                m_waitTimer += Time.deltaTime;
                if (m_waitTimer >= m_waitTimeForComingBack)
                {
                    m_timer += Time.deltaTime;
                    float percentage = m_timer / m_desiredDuration;
                    m_rightHandTarget.position = Vector3.Lerp(m_rightHandPunchTarget.position, m_rightHandStartingPosition,
                Mathf.SmoothStep(0f, 1f, m_animationCurve.Evaluate(percentage)));

                    float recoveringDistance = Vector3.Distance(m_rightHandTarget.position, m_rightHandStartingPosition);
                    if (recoveringDistance <= m_distanceThreshold)
                    {
                        M_PlayerAnimationState = PlayerAnimationState.None;
                        m_timer = 0f;
                        m_waitTimer = 0f;
                    }

                }
                else
                {
                    m_timer = 0f;
                }
            }
        }

        public void LeftPunch(bool special = false)
        {
            // so when  the distance is reached, we should get back to the original position as well
            float distance = Vector3.Distance(m_leftHandTarget.position, m_leftHandPunchTarget.position);

            if (!m_shouldGoBack)
            {
                m_timer += Time.deltaTime;
                float percentage = m_timer / m_desiredDuration;


                m_leftHandTarget.position = Vector3.Lerp(m_leftHandStartingPosition, m_leftHandPunchTarget.position,
                    Mathf.SmoothStep(0f, 1f, m_animationCurve.Evaluate(percentage)));

                if (distance <= m_distanceThreshold) m_shouldGoBack = true;

            }
            else if (m_shouldGoBack)
            {
                // the distance has been reached
                // move the target backwards now
                m_waitTimer += Time.deltaTime;
                if (m_waitTimer >= m_waitTimeForComingBack)
                {
                    m_timer += Time.deltaTime;
                    float percentage = m_timer / m_desiredDuration;
                    m_leftHandTarget.position = Vector3.Lerp(m_leftHandPunchTarget.position, m_leftHandStartingPosition,
                Mathf.SmoothStep(0f, 1f, m_animationCurve.Evaluate(percentage)));

                    float recoveringDistance = Vector3.Distance(m_leftHandTarget.position, m_leftHandStartingPosition);
                    if (recoveringDistance <= m_distanceThreshold)
                    {
                        M_PlayerAnimationState = PlayerAnimationState.None;
                        m_timer = 0f;
                        m_waitTimer = 0f;
                    }
                }
                else
                {
                    m_timer = 0f;
                }
            }
        }

        public void RecoverPunches()
        {
            GameplayManager.Instance.M_GameplayState = GameplayManager.GameplayState.On;
        }
        public void RecoverBlock()
        {
            m_isBlocking = false;
            m_hitArea.enabled = true;
        }
        #endregion


        #region TouchCallbacks

        private void OnFingerReleased(Touch arg0)
        {
            m_isBlocking = false;
            //m_hitArea.enabled = true;
            Debug.Log($"Finger released");
        }

        private void OnRightScreenHold(Touch arg0)
        {
            if (m_canPunch == false) return;

            m_rightHandAnim.CrossFade("HardPunch", .1f);
            M_PunchState = PunchState.SpecialPunchRight;

            TotalPunches++;

            // change the state
            GameplayManager.Instance.M_MainPlayerCurrentState = GameplayManager.MainPlayerCurrentState.Attacking;
            GameplayManager.Instance.I_Am_About_To_Attack(this);

            GameplayManager.Instance.M_GameplayState = GameplayManager.GameplayState.Off;
            Invoke(nameof(RecoverPunches), m_punchesDelay);

        }

        private void OnLeftScreenHold(Touch arg0)
        {
            if (m_canPunch == false) return;

            m_leftHandAnim.CrossFade("HardPunch", .1f);
            M_PunchState = PunchState.SpecialPunchLeft;

            TotalPunches++;

            // change the state
            GameplayManager.Instance.M_MainPlayerCurrentState = GameplayManager.MainPlayerCurrentState.Attacking;
            GameplayManager.Instance.I_Am_About_To_Attack(this);

            GameplayManager.Instance.M_GameplayState = GameplayManager.GameplayState.Off;
            Invoke(nameof(RecoverPunches), m_punchesDelay);
        }

        private void OnMiddleScreenPressed(Touch arg0)
        {
            // this is the place where the user blocks the enemy
            /*int currentStateIndex = Random.Range(0, m_totalBlockAnimations);
            m_anim.CrossFade($"Block_{currentStateIndex}", .1f);*/

            m_rightHandAnim.CrossFade("Block", .1f);
            m_leftHandAnim.CrossFade("Block", .1f);

            m_isBlocking = true;
            //m_hitArea.enabled = false;
            //Invoke(nameof(RecoverBlock), m_blockRecoveryTime);

            // also change the Hint IK orientations as well
            SetPosition_IK_Hint();

            // change the state
            GameplayManager.Instance.M_MainPlayerCurrentState = GameplayManager.MainPlayerCurrentState.Blocking;
            //GameplayManager.Instance.I_Am_About_To_Attack(this);
        }

        private void OnRightScreenPressed(Touch arg0)
        {
            //m_anim.SetBool("NormalPunchRight", true);
            //m_anim.SetBool("NormalPunchLeft", false);

            /* m_timer = 0f;
             m_shouldGoBack = false;

             M_PlayerAnimationState = PlayerAnimationState.RightHand;*/
            if (m_canPunch == false) return;

            m_rightHandAnim.CrossFade("Punch", .1f);
            M_PunchState = PunchState.NormalPunchRight;

            TotalPunches++;

            // change the state
            GameplayManager.Instance.M_MainPlayerCurrentState = GameplayManager.MainPlayerCurrentState.Attacking;
            GameplayManager.Instance.I_Am_About_To_Attack(this);

            GameplayManager.Instance.M_GameplayState = GameplayManager.GameplayState.Off;
            Invoke(nameof(RecoverPunches), m_punchesDelay);
        }

        private void OnLeftScreenPressed(Touch arg0)
        {
            //m_anim.SetBool("NormalPunchLeft", true);
            //m_anim.SetBool("NormalPunchRight", false);

            /* m_timer = 0f;
             m_shouldGoBack = false;

             M_PlayerAnimationState = PlayerAnimationState.LeftHand;*/
            if (m_canPunch == false) return;

            m_leftHandAnim.CrossFade("Punch", .1f);
            M_PunchState = PunchState.NormalPunchLeft;

            TotalPunches++;

            // change the state
            GameplayManager.Instance.M_MainPlayerCurrentState = GameplayManager.MainPlayerCurrentState.Attacking;
            GameplayManager.Instance.I_Am_About_To_Attack(this);

            GameplayManager.Instance.M_GameplayState = GameplayManager.GameplayState.Off;
            Invoke(nameof(RecoverPunches), m_punchesDelay);
        }

        #endregion


        #region Animation Callbacks

        [Header("Animation callbacks settings")]
        [SerializeField] Transform m_rightHintTarget;
        [SerializeField] Transform m_leftHintTarget;
        [SerializeField] Transform m_rightHint;
        [SerializeField] Transform m_leftHint;
        Vector3 m_rightHintDefaultPosition;
        Vector3 m_leftHintDefaultPosition;
        [Tooltip("When the player does the block animation, the hint targets are changed, so they need to get to their default position after this time")]
        [SerializeField] float m_fallbackTime = 1;
        [SerializeField] float transitionDuration = 1f;//

        public void TurnOffPunches(string animParamName)
        {
            m_anim.SetBool($"{animParamName}", false);
        }

        /// <summary>
        /// Uset to set the value of the Right and Left Hint to manage the orientation of the elbows
        /// </summary>
        public void SetPosition_IK_Hint()
        {
            // so use this event when the player is doing the Block animations, we need to change the orientation of 
            // the knees as well in order for the block animation to truly work out
            StartCoroutine(IK_Hint_Coroutine());
        }
        IEnumerator IK_Hint_Coroutine()
        {
            //transitionDuration = 1.0f; // Duration of the transition in seconds

            // Smoothly transition to target positions
            float elapsedTime = 0f;
            Vector3 initialRightHintPosition = m_rightHint.position;
            Vector3 initialLeftHintPosition = m_leftHint.position;

            while (elapsedTime < transitionDuration)
            {
                m_rightHint.position = Vector3.Lerp(initialRightHintPosition, m_rightHintTarget.position, elapsedTime / transitionDuration);
                m_leftHint.position = Vector3.Lerp(initialLeftHintPosition, m_leftHintTarget.position, elapsedTime / transitionDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure final positions are set exactly to targets
            m_rightHint.position = m_rightHintTarget.position;
            m_leftHint.position = m_leftHintTarget.position;

            yield return new WaitForSeconds(m_fallbackTime);

            // Smoothly transition back to default positions
            elapsedTime = 0f;
            initialRightHintPosition = m_rightHint.position;
            initialLeftHintPosition = m_leftHint.position;

            while (elapsedTime < transitionDuration)
            {
                m_rightHint.position = Vector3.Lerp(initialRightHintPosition, m_rightHintDefaultPosition, elapsedTime / transitionDuration);
                m_leftHint.position = Vector3.Lerp(initialLeftHintPosition, m_leftHintDefaultPosition, elapsedTime / transitionDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure final positions are set exactly to default positions
            m_rightHint.position = m_rightHintDefaultPosition;
            m_leftHint.position = m_leftHintDefaultPosition;
        }




        #endregion

        #region TriggerEvents


        public void OnTriggerEnter_Hit(Collider collider)
        {
            // since this is the place where enemy is being hit on the face
            // try to get the component of the NPC
            // since we've the reference of the NPC and mainPlayer
            // we don't need to dynamically get it from the collider
            try
            {
                var npc = GameplayManager.Instance.NPC;
                if (npc.m_isBlocking == false)
                {


                    // Apply the damage as well
                    GameplayManager.Instance.RegisterHit(GameplayManager.HitFrom.Player);

                    // do the head hit
                    npc.DoHeadHit();


                    // now we need some particles as well

                    if (M_PunchState == PunchState.SpecialPunchRight || M_PunchState == PunchState.SpecialPunchLeft)
                    {
                        // do the shake as well

                        CinematicsController.Instance.DoShake(CinematicsController.ShakeLevel.High);

                        EffectsController.Instance.SpawnParticle
                                                (
                                                EffectsController.Instance.HitEffects_NormalPunch[Random.Range(0, EffectsController.Instance.HitEffects_NormalPunch.Length)],
                                                m_particleSpawnTransform.position
                                                );
                    }
                    else
                    {
                        CinematicsController.Instance.DoShake(CinematicsController.ShakeLevel.Low);

                        EffectsController.Instance.SpawnParticle
                                                (
                                                EffectsController.Instance.HitEffects_SpecialPunch[Random.Range(0, EffectsController.Instance.HitEffects_SpecialPunch.Length)],
                                                m_particleSpawnTransform.position
                                                );
                    }


                }

                else
                {
                    // ok since we're blocking, lets do a different animation and deflect the player punches back


                    // checking the block breaker
                    npc.TotalPunchesOnBlock++;
                    if (npc.TotalPunchesOnBlock >= npc.MaxPunchesToBreakTheBlock)
                    {
                        // break the NPC block
                        npc.TotalPunchesOnBlock = 0;

                        npc.CancelAutomatedBlock();

                        // Apply the damage as well
                        GameplayManager.Instance.RegisterHit(GameplayManager.HitFrom.Player);

                        // do the head hit
                        npc.DoHeadHit();
                    }

                    GameplayManager.Instance.NPC.SetupCombo();

                    bool isRight = M_PunchState == PunchState.NormalPunchRight || M_PunchState == PunchState.SpecialPunchRight;

                    if (isRight) m_rightHandAnim.CrossFade("Deflect", .1f);
                    else m_leftHandAnim.CrossFade("Deflect", .1f);



                    CinematicsController.Instance.DoShake(CinematicsController.ShakeLevel.Medium);

                    EffectsController.Instance.SpawnParticle
                                            (
                                            EffectsController.Instance.BlockEffects[Random.Range(0, EffectsController.Instance.BlockEffects.Length)],
                                            m_particleSpawnTransform.position
                                            );
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting NPC {e.Message}");
            }
        }
        public void OnTriggerStay_Hit(Collider collider)
        {
            Debug.Log($"Trigger stay");
        }
        public void OnTriggerExit_Hit(Collider collider)
        {
            Debug.Log($"Trigger exit");
        }


        #endregion


    }

}