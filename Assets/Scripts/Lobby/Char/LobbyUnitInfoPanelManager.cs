using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUnitInfoPanelManager : MonoBehaviour
{
    public static LobbyUnitInfoPanelManager Instance { get; private set; }
    [SerializeField] private CharacterPanelManager characterPanelManager;

    [Header("Panel Root")]
    [SerializeField] private GameObject unitInfoPanel;

    [Header("Basic Info")]
    [SerializeField] private Image cardFrame;
    [SerializeField] private Image unitIcon;
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text cardCountText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private Button closeButton;

    [Header("Stats")]
    [SerializeField] private TMP_Text attackPowerText;
    [SerializeField] private TMP_Text attackSpeedText;
    [SerializeField] private TMP_Text attackRangeText;

    [Header("Skills")]
    [SerializeField] private Transform skillButtonParent;
    [SerializeField] private GameObject skillButtonPrefab;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text skillDescriptionText;

    [Header("Recipe Settings")]
    [SerializeField] private Button openRecipeButton; // 인스펙터에서 버튼 연결
    [SerializeField] private RecipeManager recipeManager;

    [Header("Upgrade")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private Image upgradeIcon;

    private UnitData currentUnitData;
    private UnitSaveData currentSaveData;


    private void Awake()
    {
        Instance = this;
        closeButton.onClick.AddListener(HideUnitInfo);
        if (openRecipeButton != null)
        {
            openRecipeButton.onClick.AddListener(() =>
            {
                if (currentUnitData != null)
                    recipeManager.ShowRecipeDetail(currentUnitData);
            });
        }
        unitInfoPanel.SetActive(false);
    }

    public void ShowUnitInfo(UnitSaveData saveData)
    {
        if (saveData == null || DataManager.instance == null) return;

        currentSaveData = saveData;

        UnitData unitTemplate = DataManager.instance.allUnitTemplates.Find(u => u.unitName == saveData.unitID);
        if (unitTemplate == null) return;

        currentUnitData = unitTemplate;
        unitInfoPanel.SetActive(true);

        // 기본 정보 갱신
        unitIcon.sprite = currentUnitData.unitSprite;
        cardFrame.color = GetColorByGrade(currentUnitData.grade);
        unitNameText.text = currentUnitData.unitName;
        levelText.text = $"Lv.{saveData.level}";

        // ★ 수정된 부분: 만렙(50) 체크 및 UI 처리
        bool isMaxLevel = saveData.level >= 50;

        if (isMaxLevel)
        {
            // 50레벨일 경우 개수 숨기고(혹은 MAX 표시) 슬라이더 풀로 채움
            cardCountText.text = "MAX";
            progressBar.value = 1f;
            if (progressFillImage != null) progressFillImage.color = Color.green;
        }
        else
        {
            // 기존 로직
            int requiredCount = saveData.GetRequiredCount();
            float progressRatio = (float)saveData.count / requiredCount;
            cardCountText.text = $"{saveData.count}/{requiredCount}";
            progressBar.value = Mathf.Clamp01(progressRatio);

            if (progressFillImage != null)
            {
                progressFillImage.color = (progressRatio >= 1f) ? Color.green : new Color(245f / 255f, 113f / 255f, 0f / 255f);
            }
        }

        // 스탯
        float damageMult = saveData.GetDamageMultiplier();
        attackPowerText.text = (currentUnitData.damage * damageMult).ToString("F1");
        attackSpeedText.text = currentUnitData.attackSpeed.ToString("F1");
        attackRangeText.text = currentUnitData.attackRange.ToString("F1");

        RefreshSkillButtons();

        // 레벨업 버튼 상태 업데이트
        UpdateUpgradeButtonState();
    }

    private void UpdateUpgradeButtonState()
    {
        if (currentSaveData == null || DataManager.instance == null) return;

        bool isMaxLevel = currentSaveData.level >= 50;

        int reqCount = currentSaveData.GetRequiredCount();
        long reqEssence = currentSaveData.GetRequiredEssence();
        long myEssence = DataManager.instance.currentUser.essence;

        bool canUpgrade = !isMaxLevel && (currentSaveData.count >= reqCount) && (myEssence >= reqEssence);

        upgradeButton.gameObject.SetActive(canUpgrade);
        upgradeIcon.gameObject.SetActive(canUpgrade);

        // ★ 원인 해결: canUpgrade 조건과 상관없이 텍스트는 무조건 갱신합니다.
        if (upgradeCostText != null)
        {
            if (isMaxLevel)
            {
                // 만렙이면 비용을 보여줄 필요가 없으니 가려줍니다.
                upgradeCostText.text = "-";
            }
            else
            {
                upgradeCostText.text = reqEssence.ToString("N0");
                upgradeCostText.color = (myEssence >= reqEssence) ? Color.black : Color.red;
            }
        }
    }
    private void RefreshSkillButtons()
    {
        // 기존 버튼 제거
        foreach (Transform child in skillButtonParent) Destroy(child.gameObject);

        var skills = currentUnitData.skills;
        if (skills == null || skills.Count == 0) return;

        for (int i = 0; i < skills.Count; i++)
        {
            int index = i;
            GameObject btnObj = Instantiate(skillButtonPrefab, skillButtonParent);
            btnObj.GetComponentInChildren<TMP_Text>().text = (i + 1).ToString();
            btnObj.GetComponent<Button>().onClick.AddListener(() => DisplaySkill(index));
        }

        // 기본으로 첫 번째 스킬 표시
        DisplaySkill(0);
    }

    private void DisplaySkill(int index)
    {
        if (currentUnitData.skills == null || index >= currentUnitData.skills.Count) return;

        var skill = currentUnitData.skills[index];
        skillNameText.text = skill.skillName;
        string finalDescription = skill.description;
        if (finalDescription.Contains("{chance}")) {
            finalDescription = finalDescription.Replace("{chance}", "");
        }
        skillDescriptionText.text = finalDescription;
    }

    public void HideUnitInfo() {
        unitInfoPanel.SetActive(false);
    }

    private Color GetColorByGrade(UnitGrade grade)
    {
        return grade switch
        {
            // 하급 (Low): 깨끗한 느낌의 밝은 회색
            UnitGrade.Low => new Color(0.85f, 0.85f, 0.85f),

            // 중급 (Middle): 생기 있는 연두색 (라임 계열)
            UnitGrade.Middle => new Color(0.55f, 0.85f, 0.55f),

            // 상급 (High): 시원한 느낌의 하늘색
            UnitGrade.High => new Color(0.5f, 0.75f, 1f),

            // 서사급 (Epic): 신비로운 보라색 (진해지면 눈 아픈 보라를 살짝 중화)
            UnitGrade.Epic => new Color(0.75f, 0.5f, 0.95f),

            // 전설급 (Legend): 금색에 가까운 진한 노란색
            UnitGrade.Legend => new Color(1f, 0.85f, 0.3f),

            // 신화급 (Myth): 강렬하지만 부드러운 다홍색
            UnitGrade.Myth => new Color(1f, 0.5f, 0.5f),

            _ => Color.white
        };
    }

    public void TryUpgradeUnit()
    {
        if (currentSaveData == null || DataManager.instance == null) return;

        int reqCount = currentSaveData.GetRequiredCount(); //
        long reqEssence = currentSaveData.GetRequiredEssence(); //
        var user = DataManager.instance.currentUser; //

        // 1. 재차 조건 확인 (보안 및 안전성)
        if (currentSaveData.count >= reqCount && user.essence >= reqEssence)
        {
            QuestManager.instance.OnQuestProgress("daily_enhance", 1);
            QuestManager.instance.OnQuestProgress("weekly_enhance", 1);
            // 2. 재화 차감
            currentSaveData.count -= reqCount;
            user.essence -= reqEssence;

            // 3. 레벨업!
            currentSaveData.level++; //
            if (currentSaveData.level >= 30) QuestManager.instance.OnQuestProgress("perm_enhance1", 1);
            if (currentSaveData.level >= 40) QuestManager.instance.OnQuestProgress("perm_enhance2", 1);
            if (currentSaveData.level >= 50) QuestManager.instance.OnQuestProgress("perm_enhance3", 1);
            // 4. UI 갱신 (현재 창을 다시 그려서 레벨과 스탯 변화 확인)
            ShowUnitInfo(currentSaveData);
            UIManager.instance.RefreshTopBar();

            // 5. 로비 유닛 리스트 UI도 갱신이 필요하다면 이벤트 발생 (선택 사항)
            characterPanelManager.RefreshPanel();

            Debug.Log($"{currentSaveData.unitID} 레벨업 성공! 현재 Lv.{currentSaveData.level}");

            DataManager.instance.SaveData();
        }
    }
}