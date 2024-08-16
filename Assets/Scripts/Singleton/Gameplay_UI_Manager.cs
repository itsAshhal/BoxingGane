using SimpleBoxing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace SimpleBoxing
{
    public enum ScoreAnimation
    {
        Player, Enemy
    }

    /// <summary>
    /// Contains all the UI stuff, that needs to be used across the main gameplay screen
    /// </summary>
    public class Gameplay_UI_Manager : MonoBehaviour
    {
        public static Gameplay_UI_Manager Instance;
        public Image HitImage;
        public Image PlayerHealthBar;
        public Image EnemyHealthBar;
        public TMP_Text LevelText;
        public Image FadeImage;
        public TMP_Text PlayerScoreAnimatedText;
        public TMP_Text EnemyScoreAnimatedText;
        [Tooltip("How long the score thing should be on display after anyone's punch")]
        public float ScoreAppearanceDuration = .5f;
        public TMP_Text MainScoreText;
        int m_totalScore = 0;
        public GameObject GameOverMenu;
        public TMP_Text CurrentScoreText;
        public TMP_Text HighestScoreText;


        private void Awake()
        {
            if (Instance != this && Instance != null) Destroy(this);
            else Instance = this;
        }

        public void DoFadeAnimation(bool fadeIn = true, bool startSceneAsWell = false)
        {
            if (fadeIn) FadeImage.GetComponent<Animator>().CrossFade("FadeIn", .1f);
            else FadeImage.GetComponent<Animator>().CrossFade("FadeOut", .1f);

            if (startSceneAsWell) Invoke(nameof(StartScene), 4f);
        }
        void StartScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// The score texts are animated when any character (player/enemy) lands a successful punch on the opposition
        /// </summary>
        /// <param name="punchScore">Current punch score to display in the animation, also incremented in the main score as well</param>
        /// <param name="scoreAnimation">Who's punch is landed, is it the player or the enemy!?</param>

        public void AnimateScoreText(int punchScore, ScoreAnimation scoreAnimation)
        {
            StartCoroutine(ScoreAnimationCoroutine(punchScore, scoreAnimation));
        }
        public IEnumerator ScoreAnimationCoroutine(int punchScore, ScoreAnimation scoreAnimation)
        {

            Animator anim = scoreAnimation == ScoreAnimation.Enemy ? EnemyScoreAnimatedText.GetComponent<Animator>() : PlayerScoreAnimatedText.GetComponent<Animator>();

            // make a different condition for writing animation texts(scores) for the player and for the enemy as well
            if (scoreAnimation == ScoreAnimation.Player)
            {
                //punchScore = GameplayManager.Instance.PlayerConsecutivePunches;
                PlayerScoreAnimatedText.text = "+" + punchScore.ToString();
            }
            else anim.GetComponent<TMP_Text>().text = $"-{punchScore}";

            anim.CrossFade("Appear", .1f);
            yield return new WaitForSeconds(ScoreAppearanceDuration);
            anim.CrossFade("Disappear", .1f);
            m_totalScore = int.Parse(MainScoreText.text);
            m_totalScore += punchScore;
            PlayerPrefs.SetInt("PlayerScore", m_totalScore);

            // checking overall score
            // increase the main score as well
            if (scoreAnimation == ScoreAnimation.Player)
            {
                string scoreString = string.Empty;
                if (m_totalScore < 10)
                {
                    scoreString = $"0{m_totalScore}";
                    MainScoreText.text = $"0{m_totalScore}";
                }
                else
                {
                    scoreString = $"0{m_totalScore}";
                    MainScoreText.text = $"{m_totalScore}";
                }
                PlayerPrefs.SetInt("PlayerScore", m_totalScore);

            }
            else
            {
                // we also want the score to be decreased by the opponent as well
                string scoreString = MainScoreText.text;

                // decrease it by opponent score, good thing we have the param punchScore
                var result = int.Parse(scoreString) - punchScore;

                // check if the score is going below 0
                if (result > 0)
                {

                    scoreString = result.ToString();

                    if (int.Parse(scoreString) < 10) scoreString = "0" + scoreString;
                    MainScoreText.text = scoreString;
                }


            }
        }


        /// <summary>
        /// Call this method when the game is over and the player lost or has been knocked down
        /// </summary>
        public void DisplayGameOverPanel()
        {
            Animator anim = GameOverMenu.GetComponent<Animator>();
            anim.CrossFade("Appear", .1f);

            // also we need to set the current and the highest score as well
            //CurrentScoreText.text = m_totalScore < 10 ? $"0{m_totalScore}" : $"{m_totalScore}";
            CurrentScoreText.text = MainScoreText.text;

            // check for the highest score
            if (PlayerPrefs.HasKey("PlayerHighestScore"))
            {
                int highestScore = PlayerPrefs.GetInt("PlayerHighestScore");
                HighestScoreText.text = highestScore < 10 ? $"0{highestScore}" : $"{highestScore}";
            }
        }


        public void LoadScene(int sceneIndex)
        {
            StartCoroutine(LoadSceneCoroutine(sceneIndex));
        }
        IEnumerator LoadSceneCoroutine(int sceneIndex)
        {
            AsyncOperation loadSceneResult = SceneManager.LoadSceneAsync(sceneIndex);
            while (loadSceneResult.isDone == false) yield return null;

            // scene load done
        }

        public void SetPlayerCurrentScorePlayerPref(int score)
        {
            PlayerPrefs.SetInt("PlayerScore", score);
        }



    }

}