using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleBoxing.UI
{
    public class MainMenu_UI_Manager : MonoBehaviour
    {
        public static MainMenu_UI_Manager Instance;
        private void Awake()
        {
            if (Instance != this && Instance != null) Destroy(this);
            else Instance = this;
        }
    }

}