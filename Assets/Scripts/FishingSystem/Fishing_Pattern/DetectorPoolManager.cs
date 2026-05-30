using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace FishingSystem.Fishing_Pattern
{
    public class DetectorPoolManager : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private PatternDetector _detectorPrefab;
        [SerializeField] private Transform _poolParent;
        
        [SerializeField] private int _initialPoolSize = 100; 
        [SerializeField] private int _maxPoolSize = 500;

        private IObjectPool<PatternDetector> _detectorPool;

        private void Awake()
        {
            _detectorPool = new ObjectPool<PatternDetector>(
                createFunc: () =>
                {
                    PatternDetector detector = Instantiate(_detectorPrefab, _poolParent);
                    detector.PoolManager = _detectorPool; // 풀 매니저 참조 주입
                    return detector;
                },
                actionOnGet: item =>
                {
                    item.gameObject.SetActive(true);
                    item.ResetDetector();
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
            List<PatternDetector> tempStorage = new List<PatternDetector>(_initialPoolSize);
            for (int i = 0; i < _initialPoolSize; i++)
            {
                tempStorage.Add(_detectorPool.Get());
            }
            foreach (var detector in tempStorage)
            {
                _detectorPool.Release(detector);
            }
        }

        public PatternDetector GetDetector() => _detectorPool.Get();
        public void ReleaseDetector(PatternDetector detector) => _detectorPool.Release(detector);
    }
}