using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleBoxing
{
    public class Destroyer : MonoBehaviour
    {
        public float destroyTime = 2f;
        private void Start()
        {
            Invoke(nameof(DestroyThis), destroyTime);
        }

        void DestroyThis()
        {
            Destroy(gameObject);
        }
    }

}