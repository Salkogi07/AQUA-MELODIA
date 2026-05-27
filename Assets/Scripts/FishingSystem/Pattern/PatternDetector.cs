using UnityEngine;
using UnityEngine.Pool;
using R3;

namespace FishingSystem.Pattern
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class PatternDetector : MonoBehaviour
    {
        public IObjectPool<PatternDetector> PoolManager { set; private get; }
        public ReactiveProperty<bool> IsTriggered { get; } = new(false);

        private BoxCollider2D _collider;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            _collider.isTrigger = true;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }
        
        public void InitializeDetector(Vector3 targetScale, Color targetColor, bool isVisible)
        {
            transform.localScale = targetScale; 

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = isVisible; // 표시 여부 결정
                if (isVisible)
                {
                    spriteRenderer.color = targetColor;
                }
            }
        }

        public void ResetDetector()
        {
            IsTriggered.Value = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("MouseBrush"))
            {
                if (IsTriggered.Value) return;
                IsTriggered.Value = true;
            }
        }

        public void ReleaseToPool()
        {
            PoolManager?.Release(this);
        }
    }
}