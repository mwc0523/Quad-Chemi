using UnityEngine;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager instance;

    [Header("강화 레벨 상태")]
    public int tier1Level = 1; // 하/중/상급
    public int tier2Level = 1; // 서사/전설급
    public int tier3Level = 1; // 신화급

    [Header("UI 패널 연결")]
    public GameObject upgradePanel;
    public TextMeshProUGUI T_tier1;
    public TextMeshProUGUI T_tier2;
    public TextMeshProUGUI T_tier3;
    public TextMeshProUGUI T_tier1Show;
    public TextMeshProUGUI T_tier2Show;
    public TextMeshProUGUI T_tier3Show;

    // 유닛들이 공유할 글로벌 스탯 버프 객체
    public StatModifier tier1Modifier;
    public StatModifier tier2Modifier;
    public StatModifier tier3Modifier;

    void Awake()
    {
        if (instance == null) instance = this;
        upgradePanel.SetActive(false);
        // 게임 시작 시, 배율 버프를 생성 (기본 0, 즉 1배율. 1업하면 1이 되어 총 2배율이 됨)
        tier1Modifier = new StatModifier(StatType.Attack, 0f, StatModifierType.UpgradeMult, "Tier1Upgrade");
        tier2Modifier = new StatModifier(StatType.Attack, 0f, StatModifierType.UpgradeMult, "Tier2Upgrade");
        tier3Modifier = new StatModifier(StatType.Attack, 0f, StatModifierType.UpgradeMult, "Tier3Upgrade");
    }

    // --- 1번 강화 (하/중/상급) ---
    public void UpgradeTier1()
    {
        int cost = tier1Level; // 레벨당 n개
        if (InGameManager.instance.currentElementStone >= cost)
        {
            InGameManager.instance.AddElementStone(-cost); // 원소석 소모 (차감 함수 필요)
            tier1Level++;
            tier1Modifier.value = (tier1Level - 1); // 2렙이면 value=1 (총 2배율)
            Debug.Log($"1번 강화 성공! Lv.{tier1Level}");
            UpdateUpgradeUI();
        }
    }

    // --- 2번 강화 (서사/전설급) ---
    public void UpgradeTier2()
    {
        int cost = 1 + (2 * tier2Level); // 1 + 2n개
        if (InGameManager.instance.currentElementStone >= cost)
        {
            InGameManager.instance.AddElementStone(-cost);
            tier2Level++;
            tier2Modifier.value = (tier2Level - 1);
            Debug.Log($"2번 강화 성공! Lv.{tier2Level}");
            UpdateUpgradeUI();
        }
    }

    // --- 3번 강화 (신화급) ---
    public void UpgradeTier3()
    {
        int cost = 3 + (3 * tier3Level); // 3 + 3n개
        if (InGameManager.instance.currentElementStone >= cost)
        {
            InGameManager.instance.AddElementStone(-cost);
            tier3Level++;
            tier3Modifier.value = (tier3Level - 1);
            Debug.Log($"3번 강화 성공! Lv.{tier3Level}");
            UpdateUpgradeUI();
        }
    }

    // 강화 창 열기 함수
    public void OpenUpgradePanel()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);

            UpdateUpgradeUI();
        }
    }
    public void ClickUPgrade()
    {
        tier3Level++;
        tier3Modifier.value = (tier3Level - 1);
        UpdateUpgradeUI();
    }

    void UpdateUpgradeUI()
    {
        T_tier1.text = "하/중/상급\n" + tier1Level + " E";
        T_tier2.text = "서사/전설급\n" + (1 + 2 * tier2Level) + " E";
        T_tier3.text = "신화급\n" + (3 + 3 * tier3Level) + " E";
        T_tier1Show.text = "LV." + tier1Level;
        T_tier2Show.text = "LV." + tier2Level;
        T_tier3Show.text = "LV." + tier3Level;
    }

    // 강화 창 닫기 (X 버튼용) 함수
    public void CloseUpgradePanel()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
    }
}