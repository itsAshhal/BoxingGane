using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimpleBoxing.UI
{
    public class MainMenu_UI : MonoBehaviour
    {
        public static MainMenu_UI Instance;
        private void Awake()
        {
            if (Instance != this && Instance != null) Destroy(this);
            else Instance = this;
        }

        public void LoadScene(int sceneIndex)
        {
            SceneManager.LoadScene(sceneIndex);
        }
        public void LoadSceneAsync(int sceneIndex)
        {
            StartCoroutine(LoadSceneCoroutine(sceneIndex));
        }
        IEnumerator LoadSceneCoroutine(int sceneIndex)
        {
            AsyncOperation loadSceneResult = SceneManager.LoadSceneAsync(sceneIndex);
            while (loadSceneResult.isDone == false) yield return null;

            // scene load done
        }

        public void OnClick_PlayGame()
        {
            // Set the playerHealth playerPref to 1 so it can be used again
            PlayerPrefs.SetFloat("PlayerHealth", 0.0f);
            PlayerPrefs.SetInt("PlayerScore", 0);
            PlayerPrefs.SetInt("DifficultyLevel", 4);
        }

    }

}