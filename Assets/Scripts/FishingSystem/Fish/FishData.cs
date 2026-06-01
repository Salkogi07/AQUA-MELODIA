namespace FishingSystem.Fish
{
    public class FishData
    {
        // 고유 원본 데이터 참조
        public FishDataSO Data { get; private set; }
        
        // 변경 데이터 (실시간)
        public float CurrentStamina { get; set; }
        public float Quality { get; set; } 

        public FishData(FishDataSO data)
        {
            Data = data;
            CurrentStamina = data.maxStamina; // 시작 시 최대 기력으로 초기화
            Quality = 0f;                     // 미니게임 시작 전 품질 초기화
        }
    }
}