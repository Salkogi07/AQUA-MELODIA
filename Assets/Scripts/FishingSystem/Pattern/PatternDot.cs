using UnityEngine;
using UnityEngine.Pool;
using R3;

namespace FishingSystem.Pattern
{
    public class PatternDot : MonoBehaviour
    {
        public IObjectPool<PatternDot> PoolManager { set; private get; }
        public ReactiveProperty<bool> IsErased { get; } = new(false);
        
        [SerializeField] private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        public void Reset()
        {
            IsErased.Value = false;
            
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
        }
        
        public void SetColor(Color color)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
        }

        public void Erase()
        {
            if (IsErased.Value) return;
            IsErased.Value = true;

            ReleaseToPool();
        }

        public void ReleaseToPool()
        {
            PoolManager?.Release(this);
        }
    }
}