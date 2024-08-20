using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleBoxing.UI
{
    /// <summary>
    /// This script only handles the UI elements that need to be adjusted, especially when playing the game on iPad
    /// </summary>
    /// 
    [ExecuteAlways]
    public class UI_basedOnDeviceScreen : MonoBehaviour
    {
        public bool ShouldScaleUIWithCode = false;

        public Vector2 AppropriateScale;
        private Vector2 _defaultScale;

        private void OnEnable()
        {
            _defaultScale = GetComponent<RectTransform>().localScale;
            ChangeUIBasedOnIpad();
        }

        /// <summary>
        /// This method contains the code to change the sizing and positioning of some UI elements especially on iPad,
        /// since the UI on all other iOS and android devices tend to work fine, so being on iPad is only the major issue
        /// </summary>
        public void ChangeUIBasedOnIpad()
        {
            if (ShouldScaleUIWithCode == true)
            {
                if (IsCurrentDeviceIpad())
                {
                    GetComponent<RectTransform>().localScale = AppropriateScale;
                }
            }

            else GetComponent<RectTransform>().localScale = _defaultScale;

        }

        [ContextMenu("Change UI Forcefully")]
        public void ChangeUIBasedOnIpad_ForceFully()
        {
            GetComponent<RectTransform>().localScale = AppropriateScale;
        }

        [ContextMenu("UnChange UI Forcefully")]
        public void UnChangeUIBasedOnIpad_Unforcefully()
        {
            GetComponent<RectTransform>().localScale = _defaultScale;
        }

        /// <summary>
        /// Call this method to check if the current device is an iPad
        /// </summary>
        /// <returns></returns>
        public bool IsCurrentDeviceIpad()
        {
            var identifier = SystemInfo.deviceModel;
            Debug.Log($"Identifier is {identifier}");

            if (identifier.StartsWith("iPad", StringComparison.Ordinal))
            {
                // iPad logic
                Debug.Log($"iPad {true}");
                return true;
            }

            else
            {
                // iPhone logic
                Debug.Log($"iPad {false}");
                return false;
            }




        }
    }
}
