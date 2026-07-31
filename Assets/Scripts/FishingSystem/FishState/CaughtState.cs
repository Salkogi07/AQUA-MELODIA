using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using FishingSystem.Fish;

namespace FishingSystem.FishState
{
    public class CaughtState : FishingState
    {
        private CancellationTokenSource cts;
        private bool isPulled = false;
        private GameObject spawnedFish;
        private GameObject fishPivot; 

        // 물리 시뮬레이션용 변수
        private Vector3 lastBobberPos;
        private Vector3 bobberVelocity;
        private Vector3 bobberAcceleration;
        
        private float currentAngle = 0f;
        private float angleVelocity = 0f;

        private const float SpringStrength = 220f;  
        private const float SwingDamping = 0.82f;   
        private const float InertiaInfluence = 0.35f; 

        // 물고기가 손으로 날아오는 시간의 속도 비율입니다.
        private const float FlyDurationMultiplier = 2f; 

        public CaughtState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public override void Enter()
        {
            base.Enter();
            isPulled = false;
            cts = new CancellationTokenSource();

            currentAngle = 0f;
            angleVelocity = 0f;
            lastBobberPos = fishingRod.bobber.position;
            bobberVelocity = Vector3.zero;

            // 1. 물고기 프리팹 결정
            GameObject prefabToSpawn = fishingRod.fishVisualPrefab;
            if (fishingRod.CurrentHookedFish != null && fishingRod.CurrentHookedFish.Data != null)
            {
                var data = fishingRod.CurrentHookedFish.Data;
                var field = data.GetType().GetField("fishPrefab");
                if (field != null)
                {
                    var customPrefab = field.GetValue(data) as GameObject;
                    if (customPrefab != null) prefabToSpawn = customPrefab;
                }
            }

            if (prefabToSpawn != null)
            {
                // 2. 바늘 위치에 회전용 피벗 오브젝트 생성
                fishPivot = new GameObject("FishPivot");
                fishPivot.transform.SetParent(fishingRod.fishHookPoint);
                fishPivot.transform.localPosition = Vector3.zero;
                fishPivot.transform.localRotation = Quaternion.identity;

                // 3. 월드 좌표 기준 정렬을 위해 피벗 외부에 임시로 먼저 생성
                spawnedFish = Object.Instantiate(prefabToSpawn);
                spawnedFish.transform.rotation = Quaternion.identity;

                // 4. 입(Mouth) 트랜스폼 찾기
                Transform mouthTransform = null;
                FishBitingPoint bitePointComp = spawnedFish.GetComponentInChildren<FishBitingPoint>();
                
                if (bitePointComp != null)
                {
                    mouthTransform = bitePointComp.transform;
                }
                else
                {
                    mouthTransform = FindMouthByName(spawnedFish.transform);
                }

                // 5. 입 위치를 피벗에 100% 일치시킴
                if (mouthTransform != null)
                {
                    Vector3 worldMouthPos = mouthTransform.position;
                    Vector3 worldRootPos = spawnedFish.transform.position;
                    Vector3 mouthOffset = worldMouthPos - worldRootPos;

                    spawnedFish.transform.SetParent(fishPivot.transform);
                    spawnedFish.transform.position = fishPivot.transform.position - mouthOffset;
                }
                else
                {
                    spawnedFish.transform.SetParent(fishPivot.transform);
                    spawnedFish.transform.localPosition = Vector3.zero;
                    Debug.LogWarning($"⚠️ 물고기 프리팹에서 'Mouth' 자식 또는 'FishBitingPoint'를 찾을 수 없어 중심점으로 정렬했습니다.");
                }
            }

            fishingRod.SetLineState(FishingSystem.Fishing_Rod.FishingLineState.Taut);
            Debug.Log("<color=lime>🎣 [성공] 물고기를 낚아 올립니다!</color>");
        }

        private Transform FindMouthByName(Transform parent)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                string nameLower = child.name.ToLower();
                if (nameLower == "mouth" || nameLower == "mouthpoint" || nameLower == "bitepoint" || nameLower == "bitingpoint")
                {
                    return child;
                }
            }
            return null;
        }

        public override void Update()
        {
            CalculateBobberPhysics();

            if (!isPulled)
            {
                Vector2 dir = (fishingRod.rodTip.position - fishingRod.bobber.position).normalized;
                fishingRod.BobberRb.linearVelocity = Vector2.Lerp(fishingRod.BobberRb.linearVelocity, dir * 5f, Time.deltaTime * 5f);

                UpdatePendulumSwing(Time.deltaTime, true);
            }
        }

        private void CalculateBobberPhysics()
        {
            Vector3 currentPos = fishingRod.bobber.position;
            if (Time.deltaTime > 0)
            {
                Vector3 newVelocity = (currentPos - lastBobberPos) / Time.deltaTime;
                bobberAcceleration = (newVelocity - bobberVelocity) / Time.deltaTime;
                bobberVelocity = newVelocity;
            }
            lastBobberPos = currentPos;
        }

        private void UpdatePendulumSwing(float dt, bool isUnderWater)
        {
            if (fishPivot == null) return;

            Vector3 gravity = Vector3.down * 9.81f;
            Vector3 netForce = gravity - bobberAcceleration * InertiaInfluence;

            float targetAngle = Mathf.Atan2(netForce.y, netForce.x) * Mathf.Rad2Deg + 90f;
            targetAngle = Mathf.Clamp(targetAngle, -75f, 75f); 

            float angleDiff = targetAngle - currentAngle;
            float springForce = angleDiff * SpringStrength;
            
            angleVelocity += springForce * dt;
            angleVelocity *= SwingDamping; 
            currentAngle += angleVelocity * dt;

            float struggleSpeed = isUnderWater ? 45f : 30f;
            float struggleIntensity = isUnderWater ? 20f : 10f;
            float fastWriggle = Mathf.Sin(Time.time * struggleSpeed) * struggleIntensity;

            fishPivot.transform.localRotation = Quaternion.Euler(0, 0, currentAngle + fastWriggle);
        }

        public override void Exit()
        {
            base.Exit();
            cts?.Cancel();
            cts?.Dispose();
            
            if (fishPivot != null) Object.Destroy(fishPivot);
        }

        public void ExecutePull()
        {
            if (isPulled) return;
            isPulled = true;
            fishingRod.ResetBobberPhysics();
            FlyToPlayerRoutine(cts.Token).Forget();
        }

        private async UniTaskVoid FlyToPlayerRoutine(CancellationToken token)
        {
            Transform bobber = fishingRod.bobber;
            Transform targetHand = fishingRod.catchHandPosition;
            Vector3 startPos = bobber.position;
            float elapsed = 0f;

            // 낚싯줄 회수 기준 시간에 조율 배율을 곱해 비행 시간을 계산합니다.
            float flyDuration = fishingRod.retrieveDuration * FlyDurationMultiplier;

            try
            {
                while (elapsed < flyDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / flyDuration;

                    Vector3 p1 = (startPos + targetHand.position) / 2f + Vector3.up * fishingRod.retrieveArcHeight;
                    float u = 1 - t;
                    Vector3 currentPos = (u * u * startPos) + (2 * u * t * p1) + (t * t * targetHand.position);
                    bobber.position = currentPos;

                    CalculateBobberPhysics();
                    UpdatePendulumSwing(Time.deltaTime, false);

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                if (spawnedFish != null)
                {
                    spawnedFish.transform.SetParent(null);
                    spawnedFish.transform.rotation = Quaternion.identity;
                }
                
                if (fishPivot != null) Object.Destroy(fishPivot);

                fishingRod.ShowcaseState.SetFishObject(spawnedFish);
                stateMachine.ChangeState(fishingRod.ShowcaseState);
            }
            catch (System.OperationCanceledException) { }
        }
    }
}