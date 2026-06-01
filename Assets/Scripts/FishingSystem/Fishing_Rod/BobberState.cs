namespace FishingSystem.Fishing_Rod
{
    public enum BobberState
    {
        Ready,       // 던지기 전 대기 상태
        Flying,      // 공중을 날아가는 상태
        Settled,     // 물에 안착한 상태
        Biting,      // 물고기가 물어서 입질 중인 상태 (클릭 시 챔질 성공)
        Retrieving   // 찌를 자연스럽게 낚싯대 끝으로 회수 중인 상태
    }
}