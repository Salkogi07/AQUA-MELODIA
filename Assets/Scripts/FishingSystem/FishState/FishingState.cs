using UnityEngine;

namespace FishingSystem.FishState
{
    public abstract class FishingState
    {
        protected FishingRod fishingRod;
        protected FishingStateMachine stateMachine;
        protected string animBoolName;

        protected Animator anim;

        public FishingState(FishingRod fishingRod, FishingStateMachine stateMachine, string animBoolName)
        {
            this.fishingRod = fishingRod;
            this.stateMachine = stateMachine;
            this.animBoolName = animBoolName;
            this.anim = fishingRod.GetComponentInChildren<Animator>(); // 애니메이터 참조
        }

        public virtual void Enter()
        {
            anim.SetBool(animBoolName, true);
        }

        public virtual void Update() { }

        public virtual void FixedUpdate() { }

        public virtual void Exit()
        {
            anim.SetBool(animBoolName, false);
        }
    }
}