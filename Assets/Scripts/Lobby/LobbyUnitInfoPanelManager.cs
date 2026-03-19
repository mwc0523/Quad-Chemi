using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUnitInfoPanelManager : MonoBehaviour
{
    public static LobbyUnitInfoPanelManager Instance { get; private set; }

    [Header("Panel Root")]
    [SerializeField] private GameObject unitInfoPanel;

    [Header("Basic Info")]
    [SerializeField] private Image cardFrame;
    [SerializeField] private Image unitIcon;
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text cardCountText;
    [SerializeField] private Slider progressBar;
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

    private UnitData currentUnitData;
    

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
        HideUnitInfo();
    }

    public void ShowUnitInfo(UnitSaveData saveData)
    {
        if (saveData == null || DataManager.instance == null) return;

        // 1. 데이터 찾기 (ID 매칭)
        UnitData unitTemplate = DataManager.instance.allUnitTemplates.Find(u => u.unitName == saveData.unitID);
        if (unitTemplate == null)
        {
            Debug.LogError($"[오류] {saveData.unitID} 에 해당하는 데이터를 찾지 못했습니다.");
            return;
        }
        else
        {
            Debug.Log($"[성공] {unitTemplate.unitName} 데이터 로드 완료");
        }

        currentUnitData = unitTemplate;

        // 2. 패널 활성화 및 최상단 정렬
        unitInfoPanel.SetActive(true);

        // 3. 기본 정보 갱신
        unitIcon.sprite = currentUnitData.unitSprite;
        cardFrame.color = GetColorByGrade(currentUnitData.grade);
        unitNameText.text = currentUnitData.unitName;
        levelText.text = $"Lv.{saveData.level}";
        Debug.Log($"이름 텍스트에 들어간 값: {unitNameText.text}");

        // 4. 게이지 및 카드 수량
        int required = Mathf.Max(1, saveData.level * 10);
        cardCountText.text = $"{saveData.count}/{required}";
        progressBar.value = Mathf.Clamp01((float)saveData.count / required);

        // 5. 스탯
        attackPowerText.text = currentUnitData.damage.ToString("F1");
        attackSpeedText.text = currentUnitData.attackSpeed.ToString("F1");
        attackRangeText.text = currentUnitData.attackRange.ToString("F1");


        // 6. 스킬 버튼 생성
        RefreshSkillButtons();
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
        skillDescriptionText.text = skill.description;
    }

    public void HideUnitInfo() => unitInfoPanel.SetActive(false);

    private Color GetColorByGrade(UnitGrade grade)
    {
        return grade switch
        {
            UnitGrade.Low => Color.white,
            UnitGrade.Middle => Color.green,
            UnitGrade.High => Color.blue,
            UnitGrade.Epic => new Color(0.6f, 0f, 1f),
            UnitGrade.Legend => Color.yellow,
            UnitGrade.Myth => Color.red,
            _ => Color.white
        };
    }
}