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
using UnityEngine.Rendering;
using UnityEngine.Events;
using Unity.Properties;

namespace SimpleBoxing
{
    public class GameplayManager : MonoBehaviour
    {
        #region ScoringAndUI

        // These 3 major callbacks are called on specific areas in this script and their bindings are used by different method

        [Header("ScoringAndUI")]
        [Tooltip("When your player successfully lands the punch on the enemy but its a normal punch, use these callbacks to implement score and other stuff")]
        public UnityEvent OnPlayerNormalPunchSucces;
        [Tooltip("When your player successfully lands the punch on the enemy but its a hard/special punch, use these callbacks to implement score and other stuff")]
        public UnityEvent OnPlayerHardPunchSuccess;
        [Tooltip("When the enemy somehow manages to land a punch on you and gets a hit point, use these callbacks to implement enemy scoring and other stuff")]
        public UnityEvent OnEnemyPunchSuccess;
        [Tooltip("Callbacks runs when the game is over, irrespective of whether the player wins or looses")]
        public UnityEvent<bool> OnGameOver;
        [SerializeField] int m_playerNormalHitPunchScore;
        [SerializeField] int m_playerHardHitPunchScore;
        [SerializeField] int m_enemyNormalHitPunchScore;
        [SerializeField] float RestartTimeWhenPlayerWins = 2f;
        [Tooltip("Right now the enemy Ai seems to easy as its starting from 1 stage, we can set it to 4-5 to make a little harder")]
        [SerializeField] int DifficultyLevelShouldStartFrom = 4;
        public int AtWhichLevelTheStatsShouldStopIncreasing = 20;
        [Tooltip("It has 2 main features, when the enemy takes a punch and gets hit, he suddenly punches to break the player's momentum also when he takes a block he can do an instant punch again as well")]
        public int AtWhichLevelTheInstantAttackShouldBeStarted = 10;

        [Header("Consecutive Punches")]
        public int PlayerConsecutivePunches = 0;
        public int ConsecutivePunchesLimit = 10;
        public bool IsConsecutive = false;

        // Callbacks

        public void OnPlayerNormalPunchSuccess_Method()
        {
            Debug.Log($"Callback, player has landed a normal punch");
            Debug.Log($"On normal hit the IsConsecutive is {IsConsecutive}");
            //Gameplay_UI_Manager.Instance.AnimateScoreText(m_playerNormalHitPunchScore, ScoreAnimation.Player);
            Gameplay_UI_Manager.Instance.AnimateScoreText(PlayerConsecutivePunches, ScoreAnimation.Player);
        }
        public void OnPlayerHardPunchSuccess_Method()
        {
            Debug.Log($"Callback, player has landed a hard punch");
            //Gameplay_UI_Manager.Instance.AnimateScoreText(m_playerHardHitPunchScore, ScoreAnimation.Player);
            Gameplay_UI_Manager.Instance.AnimateScoreText(PlayerConsecutivePunches * 2, ScoreAnimation.Player);
        }
        public void OnEnemyPunchSuccess_Method()
        {
            Debug.Log($"Callback, enemy has landed a normal punch");
            IsConsecutive = false;
            PlayerConsecutivePunches = 1;
            Gameplay_UI_Manager.Instance.AnimateScoreText(m_enemyNormalHitPunchScore, ScoreAnimation.Enemy);
        }

        public void OnGameOver_Method(bool isPlayerTheWinner)
        {
            string victimName = !isPlayerTheWinner ? "Player" : "Enemy";
            Set_DifficultyLevel(Get_DifficultyLevel() + 1);
            Debug.Log($"Someone just died and its {victimName}");
            StartCoroutine(GameOverCoroutine(isPlayerTheWinner));

            M_GameplayState = GameplayState.Off;
            m_playerController.HitArea().enabled = false;


        }
        IEnumerator GameOverCoroutine(bool isPlayerTheWinner)
        {
            // so if the enemy dies we need to restart the scene so the game keeps on being played
            if (isPlayerTheWinner)
            {
                yield return new WaitForSeconds(RestartTimeWhenPlayerWins);
                var _currentScore = int.Parse(Gameplay_UI_Manager.Instance.MainScoreText.text);
                PlayerPrefs.SetInt("PlayerScore", _currentScore);

                // Set the playerHealth playerPref to 1 so it can be used again
                float currentHealth = Gameplay_UI_Manager.Instance.PlayerHealthBar.fillAmount;
                PlayerPrefs.SetFloat("PlayerHealth", (currentHealth + 0.2f));

                RestartScene();
            }
            else
            {
                // since this is the place where the player dies, make default the difficulty level
                Set_DifficultyLevel(4);

                // so we get the exactly the same score
                yield return new WaitForSeconds(Gameplay_UI_Manager.Instance.ScoreAppearanceDuration + .05f);

                // as we've died save the score limit and then go back to the main menu
                var currentScore = int.Parse(Gameplay_UI_Manager.Instance.MainScoreText.text);
                PlayerPrefs.SetInt("PlayerScore", currentScore);
                Debug.Log($"Current score is set to {currentScore}");

                // Set the playerHealth playerPref to 1 so it can be used again
                PlayerPrefs.SetFloat("PlayerHealth", 0.0f);

                // set the difficulty level 
                Set_DifficultyLevel(Get_DifficultyLevel() + 1);

                // ok so since we have the player current score, we need to check for the highest score as well
                if (PlayerPrefs.HasKey("PlayerHighestScore"))
                {
                    int highestScore = PlayerPrefs.GetInt("PlayerHighestScore");
                    if (currentScore > highestScore)
                    {
                        // set the current score as the highest score
                        PlayerPrefs.SetInt("PlayerHighestScore", currentScore);
                    }
                }
                else
                {
                    // in this case since we don't have the highest score so we'll save the current score as the highest one
                    // set the current score as the highest score
                    PlayerPrefs.SetInt("PlayerHighestScore", currentScore);
                }

                // also since we lost the game, set the playerScore to 0 so next time he starts from 0
                PlayerPrefs.SetInt("PlayerScore", 0);

                // alright since the player has lost, we don't have to restart the scene instead show the gameOver menu so
                // the player can decide either to continue(restart) or go to the mainMenu
                Gameplay_UI_Manager.Instance.DisplayGameOverPanel();
            }
        }

        private void OnApplicationQuit()
        {
            // also since we lost the game, set the playerScore to 0 so next time he starts from 0
            PlayerPrefs.SetInt("PlayerScore", 0);
            PlayerPrefs.SetFloat("PlayerHealth", 0f);
        }


        #endregion

        #region MainImplementation

        public static GameplayManager Instance;
        [Header("MainImplementation")]

        private Vector3 m_enemySpawnPosition;
        private SlowMo m_slowMo;
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
        [Tooltip("As first we need to display the animation of the enemy falling down as spawning, wait for RigBuilder setup")]
        public float GameplayStartTime = 2f;


        private void Awake()
        {
            if (Instance != this && Instance != null) Destroy(this);
            else Instance = this;

            // being required from the client, as the starting level enemies are 2 easy to tackle, lets just start from level 4 to make it a 
            // little harder from the start
            //SetPlayerPrefManually();

            // since we're setting manually, set the original difficulty level as well so it doens't affect the gameplay system 
            // and the enemy Ai
        }

        private void Start()
        {
            m_enemySpawnPosition = m_npc.transform.position;
            m_slowMo = GetComponent<SlowMo>();
            Debug.Log($"Probability of the enemy blocking is {GetBlockingProbability()}%");
            SetUpDamageSystem();
            SetupEnemyAnimationSpeed();
            ManagePlayerHealth();

            Gameplay_UI_Manager.Instance.DoFadeAnimation(false);
            Gameplay_UI_Manager.Instance.LevelText.text = Get_DifficultyLevel().ToString();
            Invoke(nameof(StartGameplay), GameplayStartTime);

            // Binding callbacks
            OnPlayerNormalPunchSucces.AddListener(OnPlayerNormalPunchSuccess_Method);
            OnPlayerHardPunchSuccess.AddListener(OnPlayerHardPunchSuccess_Method);
            OnEnemyPunchSuccess.AddListener(OnEnemyPunchSuccess_Method);
            OnGameOver.AddListener(OnGameOver_Method);

            // ok here when the scene reloads, we need to set the proper score for the player
            SetScoreProperly();
        }

        /// <summary>
        /// This method allows to dynamically increase the player heatlh level by 20% when the player wins, but if he looses, then obviously the game's gonna restart
        /// </summary>
        void ManagePlayerHealth()
        {
            // check if the key exists

            if (PlayerPrefs.HasKey("PlayerHealth") == false) return;
            Debug.Log($"Found Player Saved Health {PlayerPrefs.GetFloat("PlayerHealth")}");
            if (PlayerPrefs.GetFloat("PlayerHealth") == 0.0f) return;

            // we don't need to get the value, just increase it by 20% i.e 0.2
            // increase the player health
            Debug.Log($"Reaching down below");
            Gameplay_UI_Manager.Instance.PlayerHealthBar.fillAmount = PlayerPrefs.GetFloat("PlayerHealth"); // 20% of 1 of course

            /*
             * REMEMBER TO SET THIS KEY WHEN THE GAME IS OVER, USE THAT CALLBACK => OnGameOver
             */
        }
        void SetScoreProperly()
        {
            if (PlayerPrefs.HasKey("PlayerScore"))
            {
                var score = PlayerPrefs.GetInt("PlayerScore");
                Gameplay_UI_Manager.Instance.MainScoreText.text = score < 10 ? $"0{score}" : $"{score}";
            }
        }

        void StartGameplay()
        {
            NPC.GetComponent<RigBuilder>().enabled = true;
            NPC.HitArea().enabled = true;
            M_GameplayState = GameplayState.On;
        }


        [ContextMenu("Set difficulty level back to 5")]
        public void SetPlayerPrefManually()
        {
            Set_DifficultyLevel(4);
        }

        [ContextMenu("Set time scale low")]
        public void SetTimeScaleLow()
        {
            GetComponent<SlowMo>().DoSlowMotion(0.05f);
        }

        [ContextMenu("Set time scale high")]
        public void SetTimeScaleHigh()
        {
            GetComponent<SlowMo>().UndoSlowMotion();
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
            else return DifficultyLevelShouldStartFrom;
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

                    // Invoke the scoring callbacks
                    OnEnemyPunchSuccess?.Invoke();

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

                        OnGameOver?.Invoke(false);  // false because here, enemy is the winner
                    }

                    break;
                case HitFrom.Player:
                    // since in this case player has punched the enemy
                    //Gameplay_UI_Manager.Instance.EnemyHealthBar.fillAmount -= PlayerDamageAmount;



                    // Enemy should recover its punches
                    NPC.RecoverEnemyPunches();

                    // so whenever the player gets damage, the hand anims need to be idle
                    NPC.LeftHandAnim.CrossFade("Idle", .1f);
                    NPC.RightHandAnim.CrossFade("Idle", .1f);

                    // check if player has done a special punch
                    bool m_isSpecialPunch = PlayerController.M_PunchState == PlayerBoxingController.PunchState.SpecialPunchRight ||
                        PlayerController.M_PunchState == PlayerBoxingController.PunchState.SpecialPunchLeft;

                    Debug.Log($"IsSpecialPunch {m_isSpecialPunch}");


                    if (m_isSpecialPunch)
                    {
                        // Reduce the health
                        ReduceHealth(PlayerDamageAmount * SpecialPunchMultiplier, Gameplay_UI_Manager.Instance.EnemyHealthBar);

                        // Invoke the scoring callbacks
                        OnPlayerHardPunchSuccess?.Invoke();
                    }
                    else
                    {
                        // Reduce the health
                        ReduceHealth(PlayerDamageAmount, Gameplay_UI_Manager.Instance.EnemyHealthBar);
                        OnPlayerNormalPunchSucces?.Invoke();
                    }



                    NPC.HitsTaken++;

                    NPC.SetupCombo();

                    // ok so we need to check if the enemy has blacked out and lost the game or not
                    if (Gameplay_UI_Manager.Instance.EnemyHealthBar.fillAmount <= 0f
                        ||
                        Gameplay_UI_Manager.Instance.EnemyHealthBar.fillAmount - PlayerDamageAmount <= 0f
                        )
                    {
                        // player has died 

                        // turn off the enemy Hit area as well
                        NPC.HitArea().enabled = false;

                        M_GameplayState = GameplayState.Off;

                        // turn the rigBuilder off as well
                        NPC.GetComponent<RigBuilder>().enabled = false;
                        NPC.GetComponent<Animator>().CrossFade("Death", .1f);


                        OnGameOver?.Invoke(true);  // true because here, player is the winner

                        // since the enemy is dead, set the difficulty to next increment
                        //Set_DifficultyLevel(Get_DifficultyLevel() + 1);

                        //Invoke(nameof(RestartScene), 3f);


                        Invoke(nameof(PlayerDeathAnimation), DeathAnimationWaitTime);
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
            //Gameplay_UI_Manager.Instance.DoFadeAnimation(true, true);


        }

        void PlayerDeathAnimation()
        {
            EffectsController.Instance.SpawnParticle(
                EffectsController.Instance.DeathEffects[Random.Range(0, EffectsController.Instance.DeathEffects.Length)],
                PlayerController.transform.position
                );

            //Gameplay_UI_Manager.Instance.DoFadeAnimation(true, true);
        }

        public void DoSlowMotion(float duration)
        {
            m_slowMo.DoSlowMotion(m_slowMo.slowDownFactor);
            Invoke(nameof(UndoSlowMotion), duration);
        }
        void UndoSlowMotion()
        {
            m_slowMo.UndoSlowMotion();
        }

        public void RestartScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }



        #endregion

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
            if (level == AtWhichLevelTheStatsShouldStopIncreasing) level = AtWhichLevelTheStatsShouldStopIncreasing;
            float prob = level / 10.0f;  // Use 10.0f to ensure floating-point division

            float ExtractedValue = prob / 3.5f;  // No need to cast again, it's already float

            // now add this value to EnemyDamageAmount and subtract it from PlayerDamageAmount
            Debug.Log($"ExtractedValue {ExtractedValue}");
            EnemyDamageAmount += ExtractedValue;
            //PlayerDamageAmount -= ExtractedValue;  // for right now we're not increasing the player damage amount
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

