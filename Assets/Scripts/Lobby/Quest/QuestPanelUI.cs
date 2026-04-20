using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;


public class QuestPanelUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI title;
    public Transform contentTransform; // ScrollView의 Content
    public GameObject questSlotPrefab; // 방금 만든 QuestSlot 프리팹
    public UnityEngine.UI.Slider milestoneSlider;
    public GameObject[] milestoneIcons;

    [Header("Milestone Box Sprites")] //보상 상자 이미지들
    public Sprite lockedBoxSprite;  // 달성 전 (회색 상자)
    public Sprite readyBoxSprite;   // 수령 가능 (빛나는 상자)
    public Sprite claimedBoxSprite; // 수령 완료 (열린 상자 또는 V표시)

    [Header("Reward Popup")]
    public RewardPopupUI rewardPopup;

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


        if (currentTab != QuestType.Permanent) //업적 탭이 아닐 경우 마일스톤 표시용
        {
            milestoneSlider.gameObject.SetActive(true);
            int completedCount = GetCompletedCount(currentTab);

            if (milestoneSlider != null)
            {
                milestoneSlider.maxValue = 7;
                milestoneSlider.value = completedCount;
            }

            // 현재 탭에 맞는 수령 여부 리스트 가져오기
            List<bool> claimedList = (currentTab == QuestType.Daily) ? user.dailyMilestoneClaimed : user.weeklyMilestoneClaimed;
            int[] milestones = { 1, 3, 5, 7 };

            for (int i = 0; i < milestones.Length; i++)
            {
                if (i < milestoneIcons.Length && milestoneIcons[i] != null)
                {
                    // 아이콘 오브젝트는 항상 켜둡니다. (숨기는 게 아니라 이미지를 바꿀 거니까요)
                    milestoneIcons[i].SetActive(true);

                    // Image 컴포넌트를 가져옵니다.
                    Image iconImg = milestoneIcons[i].GetComponent<Image>();
                    if (iconImg != null)
                    {
                        bool isReached = completedCount >= milestones[i];
                        bool isClaimed = claimedList[i];

                        if (isClaimed)
                        {
                            // 1. 이미 받음 -> 열린 상자
                            iconImg.sprite = claimedBoxSprite;
                        }
                        else if (isReached)
                        {
                            // 2. 달성했는데 아직 안 받음 -> 수령 가능 상자
                            iconImg.sprite = readyBoxSprite;
                        }
                        else
                        {
                            // 3. 아직 달성 못함 -> 잠긴 상자
                            iconImg.sprite = lockedBoxSprite;
                        }
                    }
                }
            }
        }
        else
        {
            // 업적 탭일 경우: 슬라이더 끄기
            milestoneSlider.gameObject.SetActive(false);

            // 업적 탭일 경우: 아이콘들도 모두 끄기
            foreach (var icon in milestoneIcons)
            {
                if (icon != null) icon.SetActive(false);
            }
        }

    }

    // 기존 OnClickDailyMilestoneBonus 수정
    public void OnClickDailyMilestoneBonus()
    {
        var user = DataManager.instance.currentUser;
        int completedCount = GetCompletedCount(currentTab);
        List<bool> claimedList;

        if (currentTab == QuestType.Daily) claimedList = user.dailyMilestoneClaimed;
        else if (currentTab == QuestType.Weekly) claimedList = user.weeklyMilestoneClaimed;
        else return;

        int[] milestones = { 1, 3, 5, 7 };

        // 합산 보상 변수
        int totalE = 0, totalA = 0, totalT = 0;
        bool gotAnyReward = false;

        for (int i = 0; i < milestones.Length; i++)
        {
            if (completedCount >= milestones[i] && !claimedList[i])
            {
                // 보상 계산 및 합산
                var rewards = CalculateMilestoneReward(currentTab, i);
                user.essence += rewards.essence;
                user.aether += rewards.aether;

                totalE += rewards.essence;
                totalA += rewards.aether;

                claimedList[i] = true;
                gotAnyReward = true;
            }
        }

        if (gotAnyReward)
        {
            // 팝업 띄우기
            rewardPopup.Show(totalE, totalA, totalT);

            DataManager.instance.SaveData();
            UIManager.instance.RefreshTopBar();
            UIManager.instance.RefreshQuestRedDot();
            RefreshUI();
        }
    }

    // GiveMilestoneReward를 수치 반환형으로 수정
    private (int essence, int aether) CalculateMilestoneReward(QuestType tab, int index)
    {
        int e = 0, a = 0;
        if (tab == QuestType.Daily)
        {
            int[] essenceRewards = { 500, 1000, 1500, 2000 };
            e = essenceRewards[index];
            if (index == 3) a = 100;
        }
        else if (tab == QuestType.Weekly)
        {
            int[] essenceRewards = { 2000, 3000, 5000, 10000 };
            int[] aetherRewards = { 0, 100, 200, 500 };
            e = essenceRewards[index];
            a = aetherRewards[index];
        }
        return (e, a);
    }

    // QuestSlot에서 개별 보상 수령 시 호출할 Public 함수
    public void ShowRewardPopup(int e, int a, int t)
    {
        rewardPopup.Show(e, a, t);
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