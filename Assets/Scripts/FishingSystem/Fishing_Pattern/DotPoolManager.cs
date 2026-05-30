using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace FishingSystem.Fishing_Pattern
{
    public class DotPoolManager : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private PatternDot _dotPrefab;
        [SerializeField] private Transform _poolParent;
        
        [SerializeField] private int _initialPoolSize = 60; 
        [SerializeField] private int _maxPoolSize = 200;

        private IObjectPool<PatternDot> _dotPool;

        private void Awake()
        {
            _dotPool = new ObjectPool<PatternDot>(
                createFunc: () =>
                {
                    PatternDot dot = Instantiate(_dotPrefab, _poolParent);
                    dot.PoolManager = _dotPool; 
                    return dot;
                },
                actionOnGet: item =>
                {
                    item.gameObject.SetActive(true);
                    item.Reset();
                },
                actionOnRelease: item => item.gameObject.SetActive(false),
                actionOnDestroy: item => Destroy(item.gameObject),
                defaultCapacity: _initialPoolSize,
                maxSize: _maxPoolSize
            );

            PreWarmPool();
        }

        private void PreWarmPool()
        {
            List<PatternDot> tempStorage = new List<PatternDot>(_initialPoolSize);
            for (int i = 0; i < _initialPoolSize; i++)
            {
                tempStorage.Add(_dotPool.Get());
            }
            foreach (var dot in tempStorage)
            {
                _dotPool.Release(dot);
            }
        }

        public PatternDot GetDot() => _dotPool.Get();
        public void ReleaseDot(PatternDot dot) => _dotPool.Release(dot);
    }
}