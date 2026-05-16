using UnityEngine;
using UnityEngine.Pool;
using R3;

namespace FishingSystem.Pattern
{
    public class PatternDot : MonoBehaviour
    {
        // 유저가 제공한 풀 매니저 구조 적용
        public IObjectPool<PatternDot> PoolManager { set; private get; }

        // R3를 위한 ReactiveProperty
        public ReactiveProperty<bool> IsErased { get; } = new(false);

        public void Reset()
        {
            IsErased.Value = false;
            // 혹시 모를 기존 물리나 트위닝 연출이 있다면 여기서 초기화합니다.
        }

        public void Erase()
        {
            if (IsErased.Value) return;
            IsErased.Value = true;

            // 마우스로 지워지면 풀로 반환
            ReleaseToPool();
        }

        public void ReleaseToPool()
        {
            // 풀 매니저를 통해 안전하게 오브젝트 풀로 반환
            PoolManager?.Release(this);
        }
    }
}