using SimpleBoxing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;


namespace SimpleBoxing
{
    /// <summary>
    /// Contains all the UI stuff, that needs to be used across the main gameplay screen
    /// </summary>
    public class Gameplay_UI_Manager : Singleton<Gameplay_UI_Manager>
    {
        public Image HitImage;
        public Image PlayerHealthBar;
        public Image EnemyHealthBar;
        public TMP_Text LevelText;
        public Image FadeImage;

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
    }

}