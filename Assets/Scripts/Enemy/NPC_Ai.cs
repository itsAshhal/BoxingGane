using SimpleBoxing.Audio;
using SimpleBoxing.Player;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static SimpleBoxing.Player.PlayerBoxingController;
using Random = UnityEngine.Random;

namespace SimpleBoxing.Enemy
{
    public class NPC_Ai : MonoBehaviour
    {
        // Develop some features of the NPC
        [SerializeField] Animator m_rightHandAnim;
        public Animator RightHandAnim { get { return m_rightHandAnim; } }
        [SerializeField] Animator m_leftHandAnim;
        [SerializeField] RigBuilder m_rigBuilder;
        public Animator LeftHandAnim { get { return m_leftHandAnim; } }
        [SerializeField] float m_headAnimationRecoveryTime;
        [SerializeField] Collider m_hitArea;//
        public Collider HitArea() => m_hitArea;
        private Animator m_anim;
        public bool m_isBlocking = false;
        [SerializeField] Transform m_particleSpawnTransform;
        public int TotalPunches = 0;
        public int TotalBlocks = 0;
        public int HitsTaken = 0;
        public float m_comboStartsAfter = 3f;
        public float m_comboRepititionRate = 3f;
        [Tooltip("Minimum time for random enemy blocking apart from general blocking")]
        public float BlockTimerMin;
        [Tooltip("Maximum time for random enemy blocking apart from general blocking")]
        public float BlockTimerMax;
        private float m_blockTimer = 0f;
        /// <summary>
        /// This block time is extracted from the Randomeness of Min and Max BlockTime
        /// </summary>
        private float ExactBlockTime;
        public float BlockDurationMin = 2f;
        public float BlockDurationMax = 4f;
        private float ExactBlockDuration = 0f;
        private float m_blockDurationTimer = 0f;
        [Header("Consistent punches break the block")]
        public int MaxPunchesToBreakTheBlock = 3;
        /// <summary>
        /// So when these punches exceed the limit for breaker, NPC block is broken
        /// </summary>
        public int TotalPunchesOnBlock = 0;
        public bool IsAutomatedBlocking = false;
        public bool IsComboEnabled = false;
        public bool CanPunch = true;
        public float EnemyPunchRecoveryTime = 1.5f;
        public bool IsPunching = false;
        [Tooltip("So it will take this number of punches to break the player block")]
        public int PlayerBlockBreakerPunches = 2;
        [Tooltip("Separate punches for breaking player's block")]
        public int CurrentBlockPunches = 0;
        [Tooltip("So when the enemy starts doing a combo, he can throw minimum of these punches")]
        public int MaxCombosAtATimeMin = 3;
        [Tooltip("So when the enemy starts doing a combo, he can throw maximum of these punches")]
        public int MaxCombosAtATimeMax = 5;
        public float StunRecoveryTime = 5f;  // as because the animation takes 5 seconds approx seconds to complete
        public bool IsStunned = false;
        public float m_stunTimer = 0.0f;

        public enum PunchState
        {
            None, Left, Right
        }
        public PunchState M_PunchState;

        private void Awake()
        {


            m_anim = GetComponent<Animator>();
            m_rigBuilder = GetComponent<RigBuilder>();
            SetupCombo();
            ExactBlockTime = Random.Range(BlockTimerMin, BlockTimerMax);
            ExactBlockDuration = Random.Range(BlockDurationMin, BlockDurationMax);


        }

        private void Start()
        {
            // when the enemy is awaken, play a particle effect for spawning
            var effects = EffectsController.Instance.EnemyRespawnEffect;
            foreach (var effect in effects) Instantiate(effect, this.transform.position, Quaternion.identity);

            Debug.Log($"NPC Ai Started");
            HitArea().enabled = false;

            Invoke(nameof(EnableHitAre), GameplayManager.Instance.GameplayStartTime);
        }

        void EnableHitAre()
        {
            HitArea().enabled = true;
        }

        private void Update()
        {
            // Checking for difficulty level
            if (GameplayManager.Instance.Get_DifficultyLevel() >= 5)
            {
                if (IsComboEnabled == false) AutomatedBlock();
                PunchWhenPlayerIsBlocking();//
            }

            // Checking for Stun
            if (IsStunned)
            {
                m_stunTimer += Time.deltaTime;
                //GameplayManager.Instance.PlayerController.m_canPunch = false;
                m_rigBuilder.enabled = false;
                CanPunch = false;
                m_isBlocking = false;
                //m_hitArea.enabled = false;
                //m_anim.SetBool("IsStunned", true);
                m_anim.CrossFade("StunnedDance", .1f);
                m_anim.SetLayerWeight(2, 1f);  // the layer for the Stun dance but not for the whole body as avatar masks are being used

                if (m_stunTimer >= StunRecoveryTime)
                {
                    m_rigBuilder.enabled = true;
                    m_hitArea.enabled = true;
                    GameplayManager.Instance.PlayerController.m_canPunch = true;
                    IsStunned = false;
                    //m_anim.SetBool("IsStunned", false);
                    m_stunTimer = 0.0f;
                    CanPunch = true;
                    m_anim.SetLayerWeight(2, 0f);
                }




            }

            // Checking for default block count when the enemy leaves or enters the block state
            if (m_isBlocking)
            {
                if (BlockAlreadyRedefined) return;
                TotalBlocks = 0;
                BlockAlreadyRedefined = true;
            }
            else BlockAlreadyRedefined = false;

        }

        bool BlockAlreadyRedefined = false;

        /// <summary>
        /// Call this method when the blocks of the enemy are broken by player's consistent punches
        /// </summary>
        public void DoStun()
        {
            IsStunned = true;

            // spawn a stun particle as well
            var tr = EffectsController.Instance.StunTransform;
            var particles = EffectsController.Instance.StunParticles;
            var part = particles[Random.Range(0, particles.Length)];
            var part_2 = EffectsController.Instance.SpawnParticle(part, tr.position, true);   //Instantiate(part, tr.position, Quaternion.identity);
            part_2.gameObject.transform.SetParent(EffectsController.Instance.StunTransform);
            part_2.transform.rotation = part_2.transform.parent.transform.rotation;
            part_2.transform.localScale = part_2.transform.parent.localScale;
            //instantiatedPart.AddComponent<Destroyer>().destroyTime = 2f;
        }



        public void RecoverEnemyPunches()
        {
            StartCoroutine(RecoverEnemyPunchesCoroutine());
        }

        IEnumerator RecoverEnemyPunchesCoroutine()
        {
            CanPunch = false;
            yield return new WaitForSeconds(EnemyPunchRecoveryTime);
            CanPunch = true;
        }

        /// <summary>
        /// Triggered mostly when the player is blocking, for a specified time,
        /// then the enemy tries to make some punches but usually after level 5
        /// </summary>
        void PunchWhenPlayerIsBlocking()
        {
            Debug.Log($"Player is blocking so enemy is trying to punch, yes");
            EnableCombo();
        }



        public void CancelAutomatedBlock()
        {
            m_blockTimer = 0f;
            m_blockDurationTimer = 0f;
            m_isBlocking = false;
            IsAutomatedBlocking = false;
        }

        void AutomatedBlock()
        {
            m_blockTimer += Time.deltaTime;
            if (m_blockTimer >= ExactBlockTime)
            {
                m_blockDurationTimer += Time.deltaTime;
                IsAutomatedBlocking = true;

                if (m_blockDurationTimer >= ExactBlockDuration)
                {
                    ExactBlockTime = Random.Range(BlockTimerMin, BlockTimerMax);
                    ExactBlockDuration = Random.Range(BlockDurationMin, BlockDurationMax);
                    CancelAutomatedBlock();
                }
                else
                {
                    m_isBlocking = true;
                    DoBlock();
                }
            }
        }


        [ContextMenu("HeadHit")]
        public void DoHeadHit()
        {
            // since we're using a whole body animation where we only need the hit to
            // be able to be effected by the animation


            Debug.Log($"HeadHit called");
            //if (m_isBlocking) return;
            StartCoroutine(HeadAnimationCoroutine());
        }

        IEnumerator HeadAnimationCoroutine()
        {
            m_anim.enabled = false;
            m_anim.enabled = true;
            m_anim.SetLayerWeight(1, 0f);
            m_anim.SetLayerWeight(1, 1f);

            if (GameplayManager.Instance.PlayerController.M_PunchState == PlayerBoxingController.PunchState.NormalPunchRight
                ||
                GameplayManager.Instance.PlayerController.M_PunchState == PlayerBoxingController.PunchState.SpecialPunchRight
                )
            {
                m_anim.CrossFade("HeadHit", .1f);
            }

            if (GameplayManager.Instance.PlayerController.M_PunchState == PlayerBoxingController.PunchState.NormalPunchLeft
               ||
               GameplayManager.Instance.PlayerController.M_PunchState == PlayerBoxingController.PunchState.SpecialPunchLeft
               )
            {
                m_anim.CrossFade("HeadHit_2", .1f);
            }


            yield return new WaitForSeconds(m_headAnimationRecoveryTime);

            // Smoothly transition the layer weight to 0 over a given duration
            float duration = .5f; // Duration of the smooth transition (in seconds)
            float elapsedTime = 0f;
            float startWeight = 1f;
            float endWeight = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float newWeight = Mathf.Lerp(startWeight, endWeight, elapsedTime / duration);
                m_anim.SetLayerWeight(1, newWeight);
                yield return null;
            }

            // Ensure the final weight is set to 0
            m_anim.SetLayerWeight(1, 0f);

            // this can be discussed but still doesn't feel bad
            DoInstantAttack();
        }

        [ContextMenu("RightPunch")]
        public void DoRightHandPunch()
        {
            if (IsAutomatedBlocking) return;
            if (CanPunch == false) return;
            IsPunching = true;
            CancelInvoke(nameof(ResetIsPunching));
            Invoke(nameof(ResetIsPunching), .6f);
            m_rightHandAnim.CrossFade("Punch", .1f);
            M_PunchState = PunchState.Right;
        }

        [ContextMenu("LeftPunch")]
        public void DoLeftHandPunch()
        {
            if (IsAutomatedBlocking) return;
            if (CanPunch == false) return;
            IsPunching = true;
            CancelInvoke(nameof(ResetIsPunching));
            Invoke(nameof(ResetIsPunching), .6f);
            m_leftHandAnim.CrossFade("Punch", .1f);
            M_PunchState = PunchState.Left;
        }

        void ResetIsPunching() => IsPunching = false;

        [ContextMenu("Block")]
        public void DoBlock()
        {
            //IsPunching = false;
            m_rightHandAnim.CrossFade("Block", .1f);
            m_leftHandAnim.CrossFade("Block", .1f);

            // lets a random value which will decide whether the enemy should initiate an instant punch right after the block
            DoInstantAttack();

            // since we're blocking here, we need somehow to disable the trigger controller
            // so the NPC doesn't register the head blow
            //m_hitArea.enabled = false;
        }

        void DoInstantAttack()
        {
            if (GameplayManager.Instance.Get_DifficultyLevel() < GameplayManager.Instance.AtWhichLevelTheInstantAttackShouldBeStarted) return;
            int tooManyTakes = Random.Range(0, 100);
            if (tooManyTakes % 2 == 0)
            {
                // do the instant attack
                if (Random.Range(0, 10) % 2 == 0) DoRightHandPunch();
                else DoLeftHandPunch();
            }
        }




        public void PlayerIsAttacking(PlayerBoxingController playerController)
        {
            //return;
            /*m_isBlocking = true;
            CinematicsController.Instance.DoShake(CinematicsController.ShakeLevel.High);
            DoBlock();
            return;*/
            // this is the place where the NPC will decide what to do
            // usually he'll block but he can do other sort of decisions as well

            // lets try block first
            // but make sure the block is randomized, so it creates a natural feel

            if (ShouldBlock())
            {
                // here the player needs to block
                // check if he's out of blocks or not
                if (Random.Range(1, 10) % 2 == 0)
                {
                    Debug.Log($"Test, we're blocking");
                    m_isBlocking = true;
                    TotalBlocks++;
                    DoBlock();
                }
                else
                {
                    m_isBlocking = false;
                }
            }
            else
            {
                // here the NPC can't block
                Debug.Log($"Test, we're not blocking");
                m_isBlocking = false;
                m_hitArea.enabled = true;
            }



            // Cancelling the invokes so we can start again
            CancelInvoke(nameof(DoSomeAttack));

            // since the enemy has blocked, after some time, he needs to attack as well
            // we're calling InvokeRepeating because we need consistent attacks
            InvokeRepeating(nameof(DoSomeAttack), PunchTimeRate(), PunchTimeRate());


        }

        public void DoSomeAttack()
        {
            // we need to check the gameplayState. so if its off, we can't let the player do anything
            if (GameplayManager.Instance.M_GameplayState == GameplayManager.GameplayState.Off) return;

            // since we have 2 attacks, left and the right one, suggest a random one
            int randomization = Random.Range(1, 3);

            switch (randomization)
            {
                case 1:
                    DoLeftHandPunch();
                    break;
                case 2:
                    DoRightHandPunch(); break;
            }

            // since the enemy has punched, do an increment to TotalPunches of the NPC
            TotalPunches++;
            Debug.Log($"TotalPunches {TotalPunches}");

            // ok here since the enemy has generated an attack, we need to make sure the player
            // can't respond otherwise it will create conflicts with scoring and with
            // the gameplay as well
            GameplayManager.Instance.PlayerController.m_canPunch = false;
            Invoke(nameof(RecoverPlayerPunches), 2f);

            // since this is the attack place, enable the trigger controller
            GameplayManager.Instance.M_EnemyCurrentState = GameplayManager.EnemyCurrentState.Attacking;
            m_hitArea.enabled = true;
        }

        public void RecoverPlayerPunches()
        {
            GameplayManager.Instance.PlayerController.m_canPunch = true;
        }

        public void SetupCombo()
        {
            // we need to setup a combo with a normal delay after a specific difficultyLevel
            if (PlayerPrefs.HasKey("DifficultyLevel") == false) return;
            if (PlayerPrefs.GetInt("DifficultyLevel") <= GameplayManager.Instance.ComboSystemStartsAfterDifficulty) return;

            int level = PlayerPrefs.GetInt("DifficultyLevel");

            EnableCombo();
        }

        void EnableCombo()
        {
            if (IsComboEnabled) return;

            // Cancelling the invokes so we can start again
            CancelInvoke(nameof(DoSomeAttack));

            StartCoroutine(ComboCoroutine());
        }

        IEnumerator ComboCoroutine()
        {
            IsComboEnabled = true;
            yield return new WaitForSeconds(m_comboStartsAfter);

            // Cancelling the invokes so we can start again
            CancelInvoke(nameof(DoSomeAttack));

            // define max number of combos, lets say they are 3
            for (int i = 1; i <= Random.Range(MaxCombosAtATimeMin, MaxCombosAtATimeMax + 1); i++)
            {
                DoSomeAttack();
                yield return new WaitForSeconds(m_comboRepititionRate);
            }
            IsComboEnabled = false;
        }

        bool ShouldBlock()
        {
            // now deal with the probability here to make sure the NPC can block
            float prob = GameplayManager.Instance.GetBlockingProbability();

            // so we have values like 0.1%, 0.2%, 0.3%.....
            // 0.1% means out of 100 attacks, enemy can block 1
            // we still need to generate random attacks, so if the limit is reached by generating random attacks
            // then for the rest of the attacks, the enemy won't block

            // also check for 0 probability
            if (prob == 0f) return false;   // in this case since its the first stage, the most easiest of the dofficulties



            // ok lets say we have the probability of 0.1%, it means the enemy can block 1 out of 10 punches
            var allowedBlocks = prob * 10;
            return TotalBlocks <= allowedBlocks;


            // ok so there is one more algorithm for more dynamic gameplay
            // if lets say the enemy has 5 blocks allowed
            // he can max take n punches from the player
            // so the higher the difficulty is, lower the punches enemy can take

            // so here it is 
            // 2 blocks allowed, it means the difficulty is easy, now 10-2 = 8
            // it means enemy can take 8 consecutive hits from the player
            // 6 blocks are allowed, it means the difficulty level is relatively higher, so 10-6 = 4
            // enemy can take 4 consecutive hits from the player

        }
        int ConsecutiveHitsAllowed()
        {
            var value = 10 - ((int)GameplayManager.Instance.GetBlockingProbability() * 10);
            Debug.Log($"ConsecutiveHits are {value}");
            return value;

        }


        int PunchTimeRate()
        {
            // lets say the probability is 0.1% so
            // so integer of it is 1
            // 10 - 1 = 9 + (lets say an increment of 4 points)
            // so random value would be like min = 1 and max = 1 + 4

            // Directly use the difficulty level from GameplayManager
            int difficultyLevel = GameplayManager.Instance.Get_DifficultyLevel();

            // Calculate min based on the difficulty level directly
            //int min = 8 - difficultyLevel;
            int min = 0;
            int max = min + 2;  // Extend the range for randomization

            // Ensure min is always less than max
            if (min > max) Swap(ref min, ref max);

            // Generate a random value between min and max (inclusive)
            int value = Mathf.Abs(Random.Range(min, max + 1)); // Inclusive for max value
            Debug.Log($"PunchTimeRate is {value}");
            return value;


        }

        void Swap(ref int val1, ref int val2)
        {
            int temp = val1;
            val1 = val2;
            val2 = temp;
        }


        #region TriggerEvents

        public void OnPunchCollidedWithEnemy(Collider collider)
        {
            Debug.Log($"Punch collided with player");
        }

        public void OnTriggerEnter_Hit(Collider collider)
        {
            try
            {
                // Apply the damage as well
                Debug.Log($"Got hit by the enemy");
                //IsPunching = false;

                // check if the player is blocking or not
                if (GameplayManager.Instance.PlayerController.m_isBlocking && CurrentBlockPunches < PlayerBlockBreakerPunches)
                {
                    // technially the player is blocking
                    AudioController.Instance.PlaySound(PunchSound.Block);

                    // play the deflect animation as well and the return
                    GameplayManager.Instance.PlayerController.TotalBlocks++;
                    CurrentBlockPunches++;
                    if (M_PunchState == PunchState.Right) m_rightHandAnim.CrossFade("Deflect", .1f);
                    else if (M_PunchState == PunchState.Left) m_leftHandAnim.CrossFade("Deflect", .1f);
                    Debug.Log($"Deflected");

                    // play a blocking effect to make it more cooler
                    var effects = EffectsController.Instance.BlockEffects;
                    var randomEffect = effects[Random.Range(0, effects.Length)];

                    EffectsController.Instance.SpawnParticle(randomEffect, collider.transform.position, true);


                    return;
                }

                // make a simple conditioal for checking if the player blocks and enemy punches are equals it means the block will be broekn now
                if (CurrentBlockPunches == PlayerBlockBreakerPunches)
                {
                    // now since the block of the player just got broken, do some stun animation on the camera
                    // CinematicsController.Instance.StunPlayer();
                    Debug.Log("Blocks are equal to punches");
                    GameplayManager.Instance.PlayerController.IsStunned = true;//
                    // return;
                }

                // since the player has been hit, use the consecutive here 
                GameplayManager.Instance.IsConsecutive = false;
                GameplayManager.Instance.PlayerConsecutivePunches = 1;

                // here technically the enemy managed to hit the player
                AudioController.Instance.PlaySound(PunchSound.Normal);

                GameplayManager.Instance.PlayerController.TotalBlocks = 0;
                CurrentBlockPunches = 0;

                GameplayManager.Instance.PlayerController.OnBlockBroken();

                GameplayManager.Instance.RegisterHit(GameplayManager.HitFrom.Enemy);

                GameplayManager.Instance.PlayerController.DisablePunchForDuration(.5f);

                // ok so as we registered the hit successfully, make sure the player hands are at the 
                // starting position as well
                //GameplayManager.Instance.M_GameplayState = GameplayManager.GameplayState.Off;
                GameplayManager.Instance.PlayerController.RightHandAnim.CrossFade("Idle", .1f);
                GameplayManager.Instance.PlayerController.LeftHandAnim.CrossFade("Idle", .1f);

                // animate the main camera as well
                CinematicsController.Instance.MainCamera.GetComponent<Animator>().CrossFade("Hit", .1f);


                // get the main player and do the red image animation thing
                Gameplay_UI_Manager.Instance.HitImage.GetComponent<Animator>().CrossFade("Hit", .1f);

                CinematicsController.Instance.DoShake(CinematicsController.ShakeLevel.High);

                EffectsController.Instance.SpawnParticle
                                        (
                                        EffectsController.Instance.HitEffects_NormalPunch[Random.Range(0, EffectsController.Instance.HitEffects_NormalPunch.Length)],
                                        m_particleSpawnTransform.position
                                        );
            }
            catch (Exception e)
            {
                Debug.LogError($"Error on hitting from NPC {e.Message}");
            }
        }
        public void OnTriggerStay_Hit(Collider collider)
        {
            Debug.Log($"Trigger stay enemy");
        }
        public void OnTriggerExit_Hit(Collider collider)
        {
            Debug.Log($"Trigger exit enemy");
        }

        #endregion


    }
}
