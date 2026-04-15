using UnityEngine;
using System.Collections.Generic;

public class QuestPanelUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentTransform; // ScrollView의 Content
    public GameObject questSlotPrefab; // 방금 만든 QuestSlot 프리팹

    private List<GameObject> spawnedSlots = new List<GameObject>();

    void Start() {
        gameObject.SetActive(false);
    }
    // UIManager에서 패널 켤 때 호출됨
    public void RefreshUI()
    {
        var user = DataManager.instance.currentUser;

        // 기존 슬롯 삭제
        foreach (var slot in spawnedSlots) Destroy(slot);
        spawnedSlots.Clear();

        // 퀘스트 슬롯 생성
        foreach (var log in user.questLogs)
        {
            var template = QuestManager.instance.allQuests.Find(q => q.questID == log.questID);
            if (template != null)
            {
                GameObject go = Instantiate(questSlotPrefab, contentTransform);
                QuestSlot slotScript = go.GetComponent<QuestSlot>();
                slotScript.Setup(log, template, this);
                spawnedSlots.Add(go);
            }
        }

        // 여기서 일일 퀘스트 누적 달성 마일스톤 UI를 갱신하는 로직을 추가하시면 됩니다.
    }

    // 일일 퀘스트 추가 보상 (일괄 지급 버튼)
    public void OnClickDailyMilestoneBonus()
    {
        var user = DataManager.instance.currentUser;
        int completedDailyCount = GetCompletedCount(QuestType.Daily);
        int[] milestones = { 1, 3, 5, 7 };
        bool gotAnyReward = false;

        for (int i = 0; i < milestones.Length; i++)
        {
            // 마일스톤 조건을 달성했고, 아직 수령하지 않았다면
            if (completedDailyCount >= milestones[i] && !user.dailyMilestoneClaimed[i])
            {
                // 예시: 1회=에테르10, 3회=에테르30... (기획에 맞게 수정)
                int bonusAether = milestones[i] * 10;
                user.aether += bonusAether;

                user.dailyMilestoneClaimed[i] = true;
                gotAnyReward = true;
            }
        }

        if (gotAnyReward)
        {
            Debug.Log("일일 추가 보상 일괄 수령 완료!");
            DataManager.instance.SaveData();
            UIManager.instance.RefreshTopBar();
            UIManager.instance.RefreshQuestRedDot();
            RefreshUI();
        }
    }

    private int GetCompletedCount(QuestType type)
    {
        int count = 0;
        var user = DataManager.instance.currentUser;
        foreach (var log in user.questLogs)
        {
            var template = QuestManager.instance.allQuests.Find(q => q.questID == log.questID);
            if (template != null && template.questType == type && log.isCompleted)
            {
                count++;
            }
        }
        return count;
    }

    public void OnClickCloseQuestPanelButton()
    {
        gameObject.SetActive(false);
    }
}