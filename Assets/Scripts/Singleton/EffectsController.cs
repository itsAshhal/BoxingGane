using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace SimpleBoxing
{
    public class EffectsController : Singleton<EffectsController>
    {
        public ParticleSystem[] HitEffects_NormalPunch;
        public ParticleSystem[] HitEffects_SpecialPunch;
        public ParticleSystem[] BlockEffects;
        public ParticleSystem[] DeathEffects;
        public ParticleSystem[] EnemyRespawnEffect;
        public Transform StunTransform;
        public ParticleSystem[] StunParticles;
        public bool UseVFX = true;

        public void SpawnParticle(ParticleSystem particle, Vector3 spawnPosition)
        {
            if (!UseVFX) return;
            var effect = Instantiate(particle, spawnPosition, Quaternion.identity);
            effect.AddComponent<Destroyer>().destroyTime = 2f;
        }
    }

}