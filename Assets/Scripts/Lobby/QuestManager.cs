using UnityEngine;
using System;
using System.Collections.Generic;

// 퀘스트 보상 구조체
[Serializable]
public class QuestReward
{
    public CurrencyType rewardType;
    public int amount;
}

// 인스펙터에서 설정할 퀘스트 원본 데이터 (ScriptableObject로 빼도 무방합니다)
[Serializable]
public class QuestTemplate
{
    public string questID;       // 예: "daily_kill_1000"
    public string questName;     // 예: "일일 몬스터 처치"
    public QuestType questType;
    public float targetValue;      // 예: 1000
    public List<QuestReward> rewards;
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    [Header("모든 퀘스트 목록 (인스펙터에서 세팅)")]
    public List<QuestTemplate> allQuests = new List<QuestTemplate>();

    void Awake() => instance = this;

    void Start()
    {
        InitializeQuests(); // 퀘스트 목록 동기화
        CheckAndResetQuests();
    }

    // 유저 데이터에 없는 신규 퀘스트가 있다면 추가해주는 동기화 작업
    private void InitializeQuests()
    {
        var user = DataManager.instance.currentUser;
        foreach (var template in allQuests)
        {
            if (!user.questLogs.Exists(q => q.questID == template.questID))
            {
                user.questLogs.Add(new QuestSaveData
                {
                    questID = template.questID,
                    currentProgress = 0,
                    isCompleted = false,
                    isClaimed = false
                });
            }
        }
    }

    public void CheckAndResetQuests()
    {
        if (DataManager.instance == null) return;
        var user = DataManager.instance.currentUser;
        DateTime now = DateTime.Now;
        bool isFirstLoginToday = false; // 오늘 첫 로그인인지 판별용

        // 1. 일일 퀘스트 초기화 (매일 00:00)
        DateTime lastDaily = new DateTime(user.lastDailyResetTick);
        if (now.Date > lastDaily.Date)
        {
            ResetQuestGroup(QuestType.Daily);
            user.lastDailyResetTick = now.Ticks;
            user.dailyMilestoneClaimed = new List<bool> { false, false, false, false };

            user.lastLoginDate = now.ToString("yyyy-MM-dd");
            user.totalLoginCount++;
            isFirstLoginToday = true;
        }
        

        // 2. 주간 퀘스트 초기화 (월요일 00:00)
        DateTime lastWeekly = new DateTime(user.lastWeeklyResetTick);
        int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
        DateTime currentMonday = now.AddDays(-diff).Date;

        if (lastWeekly < currentMonday)
        {
            ResetQuestGroup(QuestType.Weekly);
            user.lastWeeklyResetTick = now.Ticks;
            user.weeklyMilestoneClaimed = new List<bool> { false, false, false, false };
        }

        if (isFirstLoginToday)
        {
            OnQuestProgress("daily_login", 1);
            OnQuestProgress("weekly_login", 1);
        }

        DataManager.instance.SaveData();
        UIManager.instance.RefreshQuestRedDot();
    }

    private void ResetQuestGroup(QuestType type)
    {
        var user = DataManager.instance.currentUser;
        foreach (var log in user.questLogs)
        {
            var template = allQuests.Find(q => q.questID == log.questID);
            if (template != null && template.questType == type)
            {
                log.currentProgress = 0;
                log.isCompleted = false;
                log.isClaimed = false;
            }
        }
    }

    // 몬스터 처치 등 이벤트 발생 시 호출할 함수
    // 사용 예: QuestManager.instance.OnQuestProgress("daily_kill", 1);
    public void OnQuestProgress(string targetQuestID, float amount) // 더하는 용도
    {
        var user = DataManager.instance.currentUser;
        var log = user.questLogs.Find(q => q.questID == targetQuestID);
        var template = allQuests.Find(q => q.questID == targetQuestID);

        if (log != null && template != null && !log.isCompleted)
        {
            log.currentProgress += amount;

            if (log.currentProgress >= template.targetValue)
            {
                log.currentProgress = template.targetValue;
                log.isCompleted = true;
                UIManager.instance.RefreshQuestRedDot(); // 달성 시 레드닷 켜기
            }
        }
    }

    public void UpdateQuestHighest(string targetQuestID, float value) //갱신하는 용도
    {
        var user = DataManager.instance.currentUser;
        var log = user.questLogs.Find(q => q.questID == targetQuestID);
        var template = allQuests.Find(q => q.questID == targetQuestID);

        if (log != null && template != null && !log.isCompleted)
        {
            // 새로 달성한 수치가 기존 기록보다 높을 때만 갱신
            if (value > log.currentProgress)
            {
                log.currentProgress = value;

                // 목표치 도달 여부 체크
                if (log.currentProgress >= template.targetValue)
                {
                    log.currentProgress = template.targetValue;
                    log.isCompleted = true;
                    UIManager.instance.RefreshQuestRedDot();
                }
            }
        }
    }
}