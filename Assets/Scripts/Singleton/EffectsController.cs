using System.Collections;
using System.Collections.Generic;
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

        public void SpawnParticle(ParticleSystem particle, Vector3 spawnPosition)
        {
            Destroy(Instantiate(particle, spawnPosition, Quaternion.identity), 1f);
        }
    }

}