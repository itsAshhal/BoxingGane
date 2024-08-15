using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleBoxing.Player
{
    public class BoxingGlove : MonoBehaviour
    {
        public Quaternion BlockingRotation;
        public Quaternion NonBlockingRotation;
        public bool IsEnabled = false;

        private void Update()
        {
            Debug.Log($"Transform.Rotation {transform.rotation.eulerAngles}");
            NonBlockingRotation = transform.rotation;
        }

        public void SetBlockingState()
        {
            if (IsEnabled == false) return;
            transform.rotation = BlockingRotation;
        }
        public void SetUnblockingState()
        {
            if (IsEnabled == false) return;
            transform.rotation = NonBlockingRotation;
        }
    }

}