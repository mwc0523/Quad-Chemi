using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestSlot : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI t_Title;
    public Slider s_ProgressBar;
    public TextMeshProUGUI t_Amount;          // 진행도 (예: 798/1000)
    public TextMeshProUGUI t_RewardInfo;      // 보상 내용 (T_Amount (1)에 연결하세요)
    public Button b_Reward;
    public GameObject i_CompleteCover;        // 흐린 이미지 + 체크표시 (I_CompleteCover)

    private QuestSaveData mySaveData;
    private QuestTemplate myTemplate;
    private QuestPanelUI parentPanel;

    void Start()
    {
        // 수령 버튼에 클릭 이벤트 연결
        b_Reward.onClick.AddListener(OnClickClaim);
    }

    public void Setup(QuestSaveData saveData, QuestTemplate template, QuestPanelUI panel)
    {
        mySaveData = saveData;
        myTemplate = template;
        parentPanel = panel;

        // 1. 텍스트 및 슬라이더 세팅
        t_Title.text = template.questName;
        s_ProgressBar.maxValue = template.targetValue;
        s_ProgressBar.value = saveData.currentProgress;
        t_Amount.text = $"{saveData.currentProgress}/{template.targetValue}";

        // 2. 보상 텍스트 조합 (예: "정수 2000, 에테르 300")
        string rewardStr = "";
        for (int i = 0; i < template.rewards.Count; i++)
        {
            string rName = template.rewards[i].rewardType == CurrencyType.Essence ? "정수" :
                           template.rewards[i].rewardType == CurrencyType.Aether ? "에테르" : "티켓";

            rewardStr += $"{rName} {template.rewards[i].amount}";
            if (i < template.rewards.Count - 1) rewardStr += ", ";
        }
        t_RewardInfo.text = rewardStr;

        // 3. 상태에 따른 UI 활성화/비활성화 처리
        if (saveData.isClaimed)
        {
            // 수령 완료
            i_CompleteCover.SetActive(true);
            b_Reward.interactable = false;
            b_Reward.GetComponentInChildren<TextMeshProUGUI>().text = "수령완료";
        }
        else if (saveData.isCompleted)
        {
            // 달성했으나 수령 전
            i_CompleteCover.SetActive(false);
            b_Reward.interactable = true;
            b_Reward.GetComponentInChildren<TextMeshProUGUI>().text = "수령";
        }
        else
        {
            // 진행 중
            i_CompleteCover.SetActive(false);
            b_Reward.interactable = false; // 진행중이므로 클릭 불가
            b_Reward.GetComponentInChildren<TextMeshProUGUI>().text = "진행중";
        }
    }

    // 보상 수령 버튼 클릭 시
    private void OnClickClaim()
    {
        if (mySaveData.isCompleted && !mySaveData.isClaimed)
        {
            var user = DataManager.instance.currentUser;

            // 보상 지급 로직
            foreach (var reward in myTemplate.rewards)
            {
                if (reward.rewardType == CurrencyType.Essence) user.essence += reward.amount;
                else if (reward.rewardType == CurrencyType.Aether) user.aether += reward.amount;
                else if (reward.rewardType == CurrencyType.Ticket) user.ticket += reward.amount;
            }

            mySaveData.isClaimed = true;
            DataManager.instance.SaveData();

            // 탑바 재화 및 UI 갱신
            UIManager.instance.RefreshTopBar();
            UIManager.instance.RefreshQuestRedDot();
            parentPanel.RefreshUI(); // 전체 창 새로고침 (정렬이나 마일스톤 확인용)
        }
    }
}