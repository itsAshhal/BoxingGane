using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using SimpleBoxing;
using SimpleBoxing.Enemy;
using SimpleBoxing.Player;
using UnityEngine.Animations.Rigging;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace SimpleBoxing
{
    public class GameplayManager : Singleton<GameplayManager>
    {
        private Vector3 m_enemySpawnPosition;
        [SerializeField] NPC_Ai m_npc;
        [SerializeField] PlayerBoxingController m_playerController;
        [Range(0.01f, 0.4f)]
        [SerializeField] float PlayerDamageAmount = 1f;
        [Range(0.01f, 0.4f)]
        [SerializeField] float EnemyDamageAmount = 1f;
        [SerializeField] float DeathAnimationWaitTime = 1f;

        [Header("PlayerDeathScenario")]
        [SerializeField] GameObject[] PlayerPartsToSetActive;
        [SerializeField] Transform CameraConfigurerOnDeath;
        [SerializeField] float HitSmoothDuration = .25f;
        [SerializeField] float PlayerRecoveryTimeFromEnemyPunch = 2f;  // 2 seconds lets say
        [Tooltip("So when the combo system reaches this level, combo system will be activated")]
        public int ComboSystemStartsAfterDifficulty = 5;
        [Tooltip("Hit damage registered for special punch")]
        [SerializeField] float SpecialPunchMultiplier = 1.5f;


        private void Start()
        {
            m_enemySpawnPosition = m_npc.transform.position;
            Debug.Log($"Probability of the enemy blocking is {GetBlockingProbability()}%");
            SetUpDamageSystem();
            SetupEnemyAnimationSpeed();

            Gameplay_UI_Manager.Instance.DoFadeAnimation(false);
            Gameplay_UI_Manager.Instance.LevelText.text = Get_DifficultyLevel().ToString();


        }


        public enum HitFrom
        {
            Player, Enemy
        }
        public HitFrom M_HitFrom;

        public enum GameplayState
        {
            On, Off
        }
        public GameplayState M_GameplayState;



        /// <summary>
        /// This decideds in which state the player is right now
        /// either is attacking, so enemy usually blocks
        /// </summary>
        public enum MainPlayerCurrentState
        {
            Attacking, Blocking, DoingNothing
        }
        public enum EnemyCurrentState
        {
            Attacking, Blocking, DoingNothing
        }
        public EnemyCurrentState M_EnemyCurrentState;
        public MainPlayerCurrentState M_MainPlayerCurrentState;

        public float Timer = 0f;

        [Tooltip("So if the main player hasn't done anything in this time, NPC decides to make a move")]
        public float MaxTimeout = 2f;


        // getters
        public NPC_Ai NPC => this.m_npc;
        public PlayerBoxingController PlayerController => this.m_playerController;

        /// <summary>
        /// This method acts as a registrar as whenever he is about to attack, he registers his attack
        /// and the NPC gets to know in advance and he makes moves accordingly
        /// </summary>
        /// <param name="playerController"></param>
        public void I_Am_About_To_Attack(PlayerBoxingController playerController)
        {
            // since this method is called when the player has just registered an attack
            NPC.PlayerIsAttacking(playerController);
            Timer = 0f;
        }


        /// <summary>
        /// Using this method, manage the increased difficulty of the NPC over time and over each win
        /// </summary>
        public int Get_DifficultyLevel()
        {
            if (PlayerPrefs.HasKey("DifficultyLevel"))
            {
                Debug.Log($"DifficultyLevel is {PlayerPrefs.GetInt("DifficultyLevel")}");
                return PlayerPrefs.GetInt("DifficultyLevel");
            }
            else return 0;
        }

        public void Set_DifficultyLevel(int level) => PlayerPrefs.SetInt("DifficultyLevel", level);

        public bool DifficultyLevelExists(string keyName) => PlayerPrefs.HasKey(keyName);



        /// <summary>
        /// Registering hit is important to make the match progress in a linear way
        /// </summary>
        /// <param name="hitFrom">Is the hit coming from the player or the enemy</param>
        public void RegisterHit(HitFrom hitFrom)
        {
            switch (hitFrom)
            {
                case HitFrom.Enemy:

                    // since in this case enemy has punched the player
                    //Gameplay_UI_Manager.Instance.PlayerHealthBar.fillAmount -= EnemyDamageAmount;
                    ReduceHealth(EnemyDamageAmount, Gameplay_UI_Manager.Instance.PlayerHealthBar);

                    // ok since the player is hit, we need a small amount of time until the player can
                    // start punching again
                    M_GameplayState = GameplayState.Off;
                    Invoke(nameof(RecoverPlayer), PlayerRecoveryTimeFromEnemyPunch);

                    // ok so we need to check if the player has blacked out and lost the game or not
                    if (Gameplay_UI_Manager.Instance.PlayerHealthBar.fillAmount <= 0f
                        ||
                        Gameplay_UI_Manager.Instance.PlayerHealthBar.fillAmount - EnemyDamageAmount <= 0f
                        )
                    {
                        // player has died 
                        M_GameplayState = GameplayState.Off;

                        // turn the rigBuilder off as well
                        PlayerController.GetComponent<RigBuilder>().enabled = false;
                        PlayerController.GetComponent<Animator>().CrossFade("Death", .1f);

                        // we need some additional steps to make sure the death animation looks cool
                        foreach (var item in PlayerPartsToSetActive) item.SetActive(true);

                        CinematicsController.Instance.MainCamera.Follow = null;
                        CinematicsController.Instance.MainCamera.LookAt = null;

                        CinematicsController.Instance.MainCamera.transform.position = CameraConfigurerOnDeath.position;
                        CinematicsController.Instance.MainCamera.transform.forward = CameraConfigurerOnDeath.forward;

                        Invoke(nameof(NPC_DeathAnimation), DeathAnimationWaitTime);
                    }

                    break;
                case HitFrom.Player:
                    // since in this case player has punched the enemy
                    //Gameplay_UI_Manager.Instance.EnemyHealthBar.fillAmount -= PlayerDamageAmount;

                    // check if player has done a special punch
                    bool m_isSpecialPunch = PlayerController.M_PunchState == PlayerBoxingController.PunchState.SpecialPunchRight ||
                        PlayerController.M_PunchState == PlayerBoxingController.PunchState.SpecialPunchLeft;

                    Debug.Log($"IsSpecialPunch {m_isSpecialPunch}");


                    if (m_isSpecialPunch) ReduceHealth(PlayerDamageAmount * SpecialPunchMultiplier, Gameplay_UI_Manager.Instance.EnemyHealthBar);
                    else ReduceHealth(PlayerDamageAmount, Gameplay_UI_Manager.Instance.EnemyHealthBar);



                    NPC.HitsTaken++;

                    NPC.SetupCombo();

                    // ok so we need to check if the enemy has blacked out and lost the game or not
                    if (Gameplay_UI_Manager.Instance.EnemyHealthBar.fillAmount <= 0f
                        ||
                        Gameplay_UI_Manager.Instance.EnemyHealthBar.fillAmount - PlayerDamageAmount <= 0f
                        )
                    {
                        // player has died 
                        M_GameplayState = GameplayState.Off;

                        // turn the rigBuilder off as well
                        NPC.GetComponent<RigBuilder>().enabled = false;
                        NPC.GetComponent<Animator>().CrossFade("Death", .1f);


                        // since the enemy is dead, set the difficulty to next increment
                        Set_DifficultyLevel(Get_DifficultyLevel() + 1);

                        Invoke(nameof(RestartScene), 3f);


                        //Invoke(nameof(PlayerDeathAnimation), DeathAnimationWaitTime);
                    }

                    break;

            }

        }

        public void RecoverPlayer()
        {
            M_GameplayState = GameplayState.On;
        }

        public void ReduceHealth(float playerDamageAmount, Image HealthImage)
        {
            StartCoroutine(SmoothReduceHealth(playerDamageAmount, HealthImage));
        }

        private IEnumerator SmoothReduceHealth(float playerDamageAmount, Image HealthImage)
        {
            float startFillAmount = HealthImage.fillAmount;
            float targetFillAmount = startFillAmount - playerDamageAmount; // Assuming playerDamageAmount is a percentage
            float elapsedTime = 0f;

            while (elapsedTime < HitSmoothDuration)
            {
                elapsedTime += Time.deltaTime;
                HealthImage.fillAmount = Mathf.Lerp(startFillAmount, targetFillAmount, elapsedTime / HitSmoothDuration);
                yield return null;
            }

            HealthImage.fillAmount = targetFillAmount; // Ensure it ends at the exact target
        }

        void NPC_DeathAnimation()
        {
            EffectsController.Instance.SpawnParticle(
                EffectsController.Instance.DeathEffects[Random.Range(0, EffectsController.Instance.DeathEffects.Length)],
                NPC.transform.position
                );

            // do the fade animation as well
            Gameplay_UI_Manager.Instance.DoFadeAnimation(true, true);


        }

        void PlayerDeathAnimation()
        {
            EffectsController.Instance.SpawnParticle(
                EffectsController.Instance.DeathEffects[Random.Range(0, EffectsController.Instance.DeathEffects.Length)],
                PlayerController.transform.position
                );

            Gameplay_UI_Manager.Instance.DoFadeAnimation(true, true);
        }


        void RestartScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }



        #region Probability And Difficulty

        public float GetBlockingProbability()
        {
            int difficultyLevel = Get_DifficultyLevel();
            if (difficultyLevel == 0)
            {
                // the NPC won't block at all
                return 0.0f;
            }
            else
            {
                // we need a probability for calculating how much the enemy can block
                float probability = (difficultyLevel * 1f) / 10;
                return probability;
            }

        }

        void SetUpDamageSystem()
        {
            // based on the current level state
            // we need to set the damage
            // remember that, higher the difficulty level, Player damage is low and enemy damage is higher
            var level = Get_DifficultyLevel();  // Ensure this returns an int
            float prob = level / 10.0f;  // Use 10.0f to ensure floating-point division

            float ExtractedValue = prob / 3f;  // No need to cast again, it's already float

            // now add this value to EnemyDamageAmount and subtract it from PlayerDamageAmount
            Debug.Log($"ExtractedValue {ExtractedValue}");
            EnemyDamageAmount += ExtractedValue;
            PlayerDamageAmount -= ExtractedValue;
        }

        void SetupEnemyAnimationSpeed()
        {
            var level = Get_DifficultyLevel();
            float prob = level / 10.0f;

            float ExtractedValue = prob / 3;
            NPC.RightHandAnim.speed += ExtractedValue;
            NPC.LeftHandAnim.speed += ExtractedValue;
        }



        #endregion


    }
}

