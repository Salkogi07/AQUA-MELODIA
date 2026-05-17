using UnityEngine;
using UnityEngine.Pool;
using R3;

namespace FishingSystem.Pattern
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class PatternDetector : MonoBehaviour
    {
        // 오브젝트 풀 반환을 위한 프로퍼티 추가
        public IObjectPool<PatternDetector> PoolManager { set; private get; }
        public ReactiveProperty<bool> IsTriggered { get; } = new(false);

        private BoxCollider2D _collider;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            _collider.isTrigger = true;
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