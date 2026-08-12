using UnityEngine;

namespace FishingSystem.Fish
{
    public class FishData
    {
        // 고유 원본 데이터 참조
        public FishDataSO Data { get; private set; }
        
        // 변경 데이터 (실시간)
        public float CurrentStamina { get; set; }
        public FishQuality Quality { get; set; } 
        public float Length { get; private set; }

        public FishData(FishDataSO data)
        {
            Data = data;
            CurrentStamina = data.maxStamina;
            Quality = FishQuality.GradeB;
            Length = Random.Range(data.minLength, data.maxLength);
        }
        
        // Dictionary의 Key 중복 조회를 위한 동등성 판정 기준 재정의
        public override bool Equals(object obj)
        {
            if (obj is FishData other)
            {
                if (this.Data == null || other.Data == null) return false;
                
                // 들고 있는 스크립터블 오브젝트 원본 참조가 같다면 동일 어종으로 판정합니다.
                return this.Data == other.Data;
            }
            return false;
        }

        // Equals 재정의 시 반드시 함께 짝을 맞추어야 하는 고유 해시코드 공식 지정
        public override int GetHashCode()
        {
            return Data != null ? Data.GetHashCode() : 0;
        }
    }
}