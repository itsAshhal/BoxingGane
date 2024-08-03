using SimpleBoxing;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleBoxing.Input
{
    /// <summary>
    /// Provides a central callable class instance for all major controls.
    /// Starting from touch, hold to other screen inputs
    /// </summary>
    public class InputController : Singleton<InputController>
    {
        #region Events_Actions

        // Subscribe to these events from your PlayerController class
        public UnityEvent<Touch> OnLeftScreenPressed;
        public UnityEvent<Touch> OnRightScreenPressed;
        public UnityEvent<Touch> OnMiddleScreenPressed;
        public UnityEvent<Touch> OnLeftScreenHold;
        public UnityEvent<Touch> OnRightScreenHold;
        public UnityEvent<Touch> OnFingerReleased;

        #endregion

        #region Properties

        float touchTimer = 0f;

        [Tooltip("If player touch hold exceeds this limit, boom! we're doing a heavy punch")]
        public float MaxHoldTime = 1f;

        #endregion

        #region Enums

        /// <summary>
        /// Custom player touch state
        /// </summary>
        public enum PlayerTouchState
        {
            Started, Ended
        }
        public enum ScreenPosition
        {
            Left,
            Middle,
            Right
        }
        public ScreenPosition M_ScreenPosition;

        public PlayerTouchState M_PlayerTouchState;
        private Touch m_touch;
        public bool canTouchScreen = true;

        #endregion

        #region Unity

        void Update()
        {
            // for input touches we need to 
            // first divide the screen into 3 blocks
            // left screen using width of the screen
            // right screen using width of the screen
            // middle screen using width of the screen

            // adjust the middle screen according to ur needs
            // after that the right and the left screens will be calculated automatically
            // otherwise they will interfere with the middle screen touch
            // Touch touch = Input.GetTouch(0);

            if (UnityEngine.Input.touchCount == 0) return;

            Touch touch = m_touch = UnityEngine.Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                canTouchScreen = true;
            }
            if (touch.phase == TouchPhase.Ended)
            {
                touchTimer = 0f;
                OnFingerReleased?.Invoke(touch);
                //canTouchScreen = false;
            }

            /*if (touch.phase.Equals(TouchPhase.Began))
            {
                // Turn the custom state on
                canTouchScreen = true;
                M_PlayerTouchState = PlayerTouchState.Started;
                M_ScreenPosition = DetermineScreenPosition(touch.position);
            }
            else if (touch.phase.Equals(TouchPhase.Ended))
            {
                // Turn the state off as well
                canTouchScreen = false;
                M_PlayerTouchState = PlayerTouchState.Ended;
                //ScreenPosition screenPos = DetermineScreenPosition(touch.position);
                //Debug.Log("Touch Ended at " + screenPos);
            }
            else if (touch.phase == TouchPhase.Stationary)
            {
                // ok so if the player has touched the screen for the max time limit, automatically punch
                M_PlayerTouchState = PlayerTouchState.Ended;
            }*/




            HandlePlayerTouchState_2();

        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks the current screen touch position
        /// </summary>
        /// <param name="touchPosition">The screen position, user has interacted to....</param>
        /// <returns></returns>
        private ScreenPosition DetermineScreenPosition(Vector2 touchPosition)
        {
            float screenWidth = Screen.width;

            if (touchPosition.x < screenWidth / 3)
            {
                return ScreenPosition.Left;
            }
            else if (touchPosition.x > 2 * screenWidth / 3)
            {
                return ScreenPosition.Right;
            }
            else
            {
                return ScreenPosition.Middle;
            }
        }

        /// <summary>
        /// Handling player touch states in a custom way so we can call events and callbacks
        /// </summary>
        void HandlePlayerTouchState_2()
        {
            // we need to check the gameplayState. so if its off, we can't let the player do anything
            if (GameplayManager.Instance.M_GameplayState == GameplayManager.GameplayState.Off) return;

            if (DetermineScreenPosition(m_touch.position) == ScreenPosition.Middle)
            {
                OnMiddleScreenPressed?.Invoke(m_touch);
            }
            if (m_touch.phase == TouchPhase.Ended) OnFingerReleased?.Invoke(m_touch);

            if (canTouchScreen == false) return;

            touchTimer += Time.deltaTime;
            if (touchTimer >= MaxHoldTime && m_touch.phase != TouchPhase.Ended)
            {
                // we need to do the special punch
                M_ScreenPosition = DetermineScreenPosition(m_touch.position);
                if (M_ScreenPosition == ScreenPosition.Left)
                {
                    OnLeftScreenHold?.Invoke(m_touch);
                }
                if (M_ScreenPosition == ScreenPosition.Right)
                {
                    OnRightScreenHold?.Invoke(m_touch);
                }

                canTouchScreen = false;
            }
            if (m_touch.phase == TouchPhase.Ended && touchTimer < MaxHoldTime)
            {
                M_ScreenPosition = DetermineScreenPosition(m_touch.position);
                if (M_ScreenPosition == ScreenPosition.Left)
                {
                    OnLeftScreenPressed?.Invoke(m_touch);
                }
                if (M_ScreenPosition == ScreenPosition.Right)
                {
                    OnRightScreenPressed?.Invoke(m_touch);
                }
                canTouchScreen = false;
            }

        }

        /// <summary>
        /// Handling player touch states in a custom way so we can call events and callbacks
        /// </summary>
        void HandlePlayerTouchState()
        {
            // we need to check the gameplayState. so if its off, we can't let the player do anything
            if (GameplayManager.Instance.M_GameplayState == GameplayManager.GameplayState.Off) return;

            if (canTouchScreen == false) return;

            return;

            if (M_PlayerTouchState == PlayerTouchState.Started)
            {
                // it means the user is still interacting with the screen right now
                // start the timer
                touchTimer += Time.deltaTime;
            }
            else
            {
                // here the user has left the touch interaction
                // check the time to decide whether this was a hold or a mere touch
                if (touchTimer >= MaxHoldTime && m_touch.phase != TouchPhase.Ended)
                {
                    // Hold
                    //Debug.Log($"Player is holding, screenPosition is {M_ScreenPosition.ToString()}");

                    // Calling events
                    switch (M_ScreenPosition)
                    {
                        case ScreenPosition.Left:
                            OnLeftScreenHold?.Invoke(this.m_touch);
                            break;
                        case ScreenPosition.Right:
                            OnRightScreenHold?.Invoke(this.m_touch);
                            break;
                    }
                }
                else
                {
                    // Touch
                    Debug.Log($"Player is touching, screenPosition is {M_ScreenPosition.ToString()}");

                    // Calling events
                    switch (M_ScreenPosition)
                    {
                        case ScreenPosition.Left:
                            OnLeftScreenPressed?.Invoke(this.m_touch);
                            break;
                        case ScreenPosition.Right:
                            OnRightScreenPressed?.Invoke(this.m_touch);
                            break;
                        case ScreenPosition.Middle:
                            OnMiddleScreenPressed?.Invoke(this.m_touch);
                            break;
                    }
                }

                touchTimer = 0f;
            }
        }

        #endregion
    }
}
