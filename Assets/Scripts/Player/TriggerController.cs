using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;


namespace SimpleBoxing
{
    [RequireComponent(typeof(SphereCollider))]
    public class TriggerController : MonoBehaviour
    {
        [SerializeField] string EncounterTagName = "Enemy";
        public UnityEvent<Collider> _OnTriggerEnter;
        public UnityEvent<Collider> _OnTriggerStay;
        public UnityEvent<Collider> _OnTriggerExit;
        private SphereCollider m_sphereCollider;

        private void Awake()
        {
            try
            {
                m_sphereCollider = GetComponent<SphereCollider>();

                // turn the trigger on
                m_sphereCollider.isTrigger = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Got an error {e.Message}");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(EncounterTagName) == false) return;
            this._OnTriggerEnter?.Invoke(other);

        }
        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag(EncounterTagName) == false) return;
            this._OnTriggerStay?.Invoke(other);
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(EncounterTagName) == false) return;
            this._OnTriggerExit?.Invoke(other);
        }
    }
}