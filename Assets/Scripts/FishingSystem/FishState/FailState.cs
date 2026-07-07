using UnityEngine;
using FishingSystem.Fishing_Rod;

namespace FishingSystem.FishState
{
    public class FailedState : FishingState
    {
        public FailedState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName) : base(fishingRod, stateMachine, animBoolName) { }

        public override void Enter()
        {
            base.Enter(); 
            
            // 줄 상태를 "Snapped(끊어짐)"으로 변경하여 튕겨나가는 연출 시작
            fishingRod.SetLineState(FishingLineState.Snapped);
            
            // 찌의 물리를 켜서 물 속이나 바닥으로 툭 떨어지게 만듦 (줄이 끊어졌으므로)
            fishingRod.BobberRb.bodyType = RigidbodyType2D.Dynamic;
            fishingRod.BobberRb.gravityScale = 1f;

            Debug.Log("<color=red>💥 텐션 오버! 낚싯줄이 끊어졌습니다!</color>");

            // 줄의 중간 지점에 끊어짐(Snap) 파티클 생성
            if (fishingRod.snapParticlePrefab != null)
            {
                Vector3 midPoint = Vector3.Lerp(fishingRod.rodTip.position, fishingRod.bobber.position, 0.5f);
                GameObject particle = Object.Instantiate(fishingRod.snapParticlePrefab, midPoint, Quaternion.identity);
                Object.Destroy(particle, 1.5f); // 1.5초 후 파티클 메모리 정리
            }
        }

        public override void Exit()
        {
            base.Exit(); 
            // 찌 중력스케일 원상복구
            fishingRod.BobberRb.gravityScale = fishingRod.DefaultGravity; 
        }
    }
}