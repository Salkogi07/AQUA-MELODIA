namespace FishingSystem.Fish
{
    public class FishData
    {
        // 고유 원본 데이터 참조
        public FishDataSO Data { get; private set; }
        
        // 변경 데이터 (실시간)
        public float CurrentStamina { get; set; }
        public FishQuality Quality { get; set; } 

        public FishData(FishDataSO data)
        {
            Data = data;
            CurrentStamina = data.maxStamina;
            Quality = FishQuality.GradeB;    
        }
    }
}