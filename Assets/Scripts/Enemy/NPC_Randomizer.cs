using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SimpleBoxing.Enemy
{
    /// <summary>
    /// This monoBehaviour mainly handles the randomization of different NPC assets
    /// like hats, gloves, glasses and other things which make him different.
    /// </summary>
    public class NPC_Randomizer : MonoBehaviour
    {
        public GameObject[] Collection;

        private void Start()
        {
            SetupCollection();
        }

        [ContextMenu("SetupCollection")]
        public void SetupCollection()
        {
            // if we have only 1 item in the collection, make sure its active
            if (Collection.Length == 1) Collection[0].SetActive(true);
            else
            {
                int index = Random.Range(0, Collection.Length);
                Array.ForEach(Collection, item => item.SetActive(false));
                Collection[index].SetActive(true);
            }
        }

        // so there will be multiple collections attached to different gameObjects, so we have
        // a better control over what to show and what to hide

        // REMEMBER
        // each collection can active one object, other elements of the collection will remain inactive
    }
}