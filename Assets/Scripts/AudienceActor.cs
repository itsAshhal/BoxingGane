using UnityEngine;
using Random = UnityEngine.Random;

namespace SimpleBoxing
{
    public class AudienceActor : MonoBehaviour
    {
        public Color[] SpriteColors;
        public float MinAnimationSpeed = 1.0f;
        public float MaxAnimationSpeed = 1.5f;

        public SpriteRenderer spriteRendererComponent;

        private void Start()
        {

            spriteRendererComponent.color = SpriteColors[Random.Range(0, SpriteColors.Length)];

            GetComponent<Animator>().speed = Random.Range(MinAnimationSpeed, MaxAnimationSpeed);
        }
    }

}