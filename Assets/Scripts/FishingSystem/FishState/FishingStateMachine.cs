using FishingSystem.Fishing_Rod;

namespace FishingSystem.FishState
{
    public class FishingStateMachine
    {
        public FishingState CurrentState { get; private set; }

        public void Initialize(FishingState startingState)
        {
            CurrentState = startingState;
            CurrentState.Enter();
        }

        public void ChangeState(FishingState newState)
        {
            CurrentState.Exit();
            CurrentState = newState;
            CurrentState.Enter();
        }
    }
}