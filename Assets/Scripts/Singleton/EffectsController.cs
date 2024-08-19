using ETFXPEL;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace SimpleBoxing
{
    public enum VFX_Type
    {
        ALl, Stun, HitEffectsNormal, HitEffectsSpecial, BlockEffects, DeathEffects, EnemyRespawnEffects, None
    }
    public class EffectsController : Singleton<EffectsController>
    {
        public ParticleSystem[] HitEffects_NormalPunch;
        public ParticleSystem[] HitEffects_SpecialPunch;
        public ParticleSystem[] BlockEffects;
        public ParticleSystem[] DeathEffects;
        public ParticleSystem[] EnemyRespawnEffect;
        public Transform StunTransform;
        public ParticleSystem[] StunParticles;

        public VFX_Type M_VfxType;


        public ParticleSystem SpawnParticle(ParticleSystem particle, Vector3 spawnPosition, bool EnableSpawn = false)
        {
            if (EnableSpawn == false) return null;

            var effect = Instantiate(particle, spawnPosition, Quaternion.identity);
            effect.AddComponent<Destroyer>().destroyTime = 2f;

            return effect;
        }
    }

}