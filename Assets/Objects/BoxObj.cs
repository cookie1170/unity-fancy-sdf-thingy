using Rendering;
using UnityEngine;

namespace Objects
{
    public class BoxObj : MonoBehaviour
    {
        [Min(0)] public Vector2 dimensions;
        [Min(0)] public float blendingFactor;
        public Color color;
    
        private SpriteRenderer _sprite;

        private void OnValidate()
        {
            if (!_sprite) _sprite = GetComponent<SpriteRenderer>();
            transform.localScale = new Vector3(dimensions.x, dimensions.y, 1.0f);
            _sprite.color = color;
        }

        private void Awake()
        {
            if (!_sprite) _sprite = GetComponent<SpriteRenderer>();
            _sprite.enabled = false;
        }

        private void Start()
        {
            ComputeRenderer.Boxes.Add(this);
            ComputeRenderer.OnShapesChanged?.Invoke();
        }

        private void OnEnable()
        {
            if (didStart)
            {
                ComputeRenderer.Boxes.Add(this);
                ComputeRenderer.OnShapesChanged?.Invoke();
            }
        }

        private void OnDisable()
        {
            ComputeRenderer.Boxes.Remove(this);
            ComputeRenderer.OnShapesChanged?.Invoke();
        }

        private void OnDestroy()
        {
            ComputeRenderer.Boxes.Remove(this);
            ComputeRenderer.OnShapesChanged?.Invoke();
        }
    }
}
