using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace ShadowShift.UI
{
    public class DotweenButton_2 : MonoBehaviour
    {
        public UnityEvent ClickActions;
        private Button m_button;

        // Animation parameters
        public float AnimationDuration = 0.5f;
        public Vector3 TargetScale = new Vector3(1.2f, 1.2f, 1.2f);
        public Ease EaseType = Ease.OutQuad; // Ease type for the scale animation

        private void Start()
        {
            m_button = GetComponent<Button>();
            m_button.onClick.AddListener(OnButtonClick);

        }



        public void SetObjectActiveState(GameObject go)
        {
            go.SetActive(!go.activeSelf);
        }

        private void OnButtonClick()
        {
            AnimateButton(() =>
            {
                // Callback function: Execute any logic you want after the animation ends
                Debug.Log("Button animation completed!");
                PerformButtonAction();
            });
        }

        private void AnimateButton(TweenCallback onComplete)
        {
            // Perform a scale animation with easing
            transform.DOScale(TargetScale, AnimationDuration).SetEase(EaseType).OnComplete(onComplete)
                     .OnComplete(() =>
                     {
                         // Return to original scale after the animation;
                         PerformButtonAction();
                     });
        }

        private void PerformButtonAction()
        {
            // Your custom action here
            ClickActions?.Invoke();
            // For example, load a new scene, open a panel, etc.
        }
    }
}

