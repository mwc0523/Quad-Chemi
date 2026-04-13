using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    // 퀘스트 원본 데이터 (ScriptableObject 등으로 관리 권장)
    // 여기서는 예시로 리스트로 처리
    public List<QuestTemplate> allQuests = new List<QuestTemplate>();

    void Awake() => instance = this;

    void Start()
    {
        CheckAndResetQuests();
    }

    public void CheckAndResetQuests()
    {
        var user = DataManager.instance.currentUser;
        DateTime now = DateTime.Now;

        // 1. 일일 퀘스트 초기화 (매일 00:00)
        DateTime lastDaily = new DateTime(user.lastDailyResetTick);
        if (now.Date > lastDaily.Date)
        {
            ResetQuestGroup(QuestType.Daily);
            user.lastDailyResetTick = now.Ticks;
            user.dailyMilestoneClaimed = new List<bool> { false, false, false, false };
        }

        // 2. 주간 퀘스트 초기화 (월요일 00:00)
        // 현재 주의 월요일 구하기
        DateTime lastWeekly = new DateTime(user.lastWeeklyResetTick);
        int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
        DateTime currentMonday = now.AddDays(-diff).Date;

        if (lastWeekly < currentMonday)
        {
            ResetQuestGroup(QuestType.Weekly);
            user.lastWeeklyResetTick = now.Ticks;
            user.weeklyMilestoneClaimed = new List<bool> { false, false, false, false };
        }

        DataManager.instance.SaveData();
    }

    private void ResetQuestGroup(QuestType type)
    {
        var user = DataManager.instance.currentUser;
        // 해당 타입의 퀘스트 로그만 찾아 초기화
        // 실제로는 원본 리스트에서 해당 타입의 ID들을 가져와서 덮어씌웁니다.
    }

    // 퀘스트 진행도 상승 호출 함수 (예: 몬스터 처치 시 호출)
    public void OnQuestProgress(string actionID, int amount)
    {
        // 로직: user.questLogs에서 actionID가 일치하는 퀘스트의 progress를 올림
        // 완료 시 IsCompleted 체크 및 UI 레드닷 갱신 호출
        UIManager.instance.RefreshQuestRedDot();
    }
}