using Rendering;
using UnityEngine;

namespace Objects
{
    public class CircleObj : MonoBehaviour
    {
        [Min(0)] public float radius;
        [Min(0)] public float blendingFactor;
        public Color color;
    
        private SpriteRenderer _sprite;

        private void OnValidate()
        {
            if (!_sprite) _sprite = GetComponent<SpriteRenderer>();
            transform.localScale = Vector3.one * radius * 2;
            _sprite.color = color;
        }

        private void Awake()
        {
            if (!_sprite) _sprite = GetComponent<SpriteRenderer>();
            _sprite.enabled = false;
        }

        private void Start()
        {
            ComputeRenderer.Circles.Add(this);
        }

        private void OnEnable()
        {
            if (didStart)
                ComputeRenderer.Circles.Add(this);
        }

        private void OnDisable()
        {
            ComputeRenderer.Circles.Remove(this);
        }

        private void OnDestroy()
        {
            ComputeRenderer.Circles.Remove(this);
        }
    }
}
