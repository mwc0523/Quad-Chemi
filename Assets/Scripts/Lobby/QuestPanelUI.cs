using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class QuestPanelUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI title;
    public Transform contentTransform; // ScrollView의 Content
    public GameObject questSlotPrefab; // 방금 만든 QuestSlot 프리팹
    public UnityEngine.UI.Slider milestoneSlider;
    public GameObject[] milestoneIcons;
    

    private QuestType currentTab = QuestType.Daily; //현재 탭(일일/주간/영구)
    private List<GameObject> spawnedSlots = new List<GameObject>();


    public void OnClickDailyTab() { currentTab = QuestType.Daily; RefreshUI(); }
    public void OnClickWeeklyTab() { currentTab = QuestType.Weekly; RefreshUI(); }
    public void OnClickPermTab() { currentTab = QuestType.Permanent; RefreshUI(); }

    void Start() {
        gameObject.SetActive(false);
    }
    // UIManager에서 패널 켤 때 호출됨
    public void RefreshUI()
    {
        var user = DataManager.instance.currentUser;

        if (title != null)
        {
            switch (currentTab)
            {
                case QuestType.Daily: // QuestType은 프로젝트의 Enum 타입에 맞춰 수정하세요
                    title.gameObject.SetActive(true);
                    title.text = "일일 퀘스트 달성 수";
                    break;

                case QuestType.Weekly:
                    title.gameObject.SetActive(true);
                    title.text = "주간 퀘스트 달성 수";
                    break;

                default:
                    title.gameObject.SetActive(false);
                    break;
            }
        }

        // 기존 슬롯 삭제
        foreach (var slot in spawnedSlots) Destroy(slot);
        spawnedSlots.Clear();

        // 퀘스트 슬롯 생성
        foreach (var log in user.questLogs)
        {
            var template = QuestManager.instance.allQuests.Find(q => q.questID == log.questID);
            if (template != null && template.questType == currentTab) //비어있지 않고, 현재 탭에 해당하는 퀘스트만
            {
                GameObject go = Instantiate(questSlotPrefab, contentTransform);
                QuestSlot slotScript = go.GetComponent<QuestSlot>();
                slotScript.Setup(log, template, this);
                spawnedSlots.Add(go);
            }
        }
        if(currentTab != QuestType.Permanent) { //업적 탭이 아닐 경우
            milestoneSlider.gameObject.SetActive(true);
            int completedCount = GetCompletedCount(currentTab);
            if (milestoneSlider != null)
            {
                milestoneSlider.maxValue = 7; // 마일스톤 최대치 (기획에 따라 변경 가능)
                milestoneSlider.value = completedCount;
            }

            // 달성 개수에 따라 보상 아이콘(체크 표시 등) 활성화
            int[] milestones = { 1, 3, 5, 7 };
            for (int i = 0; i < milestones.Length; i++)
            {
                if (i < milestoneIcons.Length && milestoneIcons[i] != null)
                {
                    // 퀘스트 달성 수가 마일스톤 요구치 이상이면 해당 아이콘 켜기
                    milestoneIcons[i].SetActive(completedCount >= milestones[i]);
                }
            }
        }
        else {
            milestoneSlider.gameObject.SetActive(false);
        }

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