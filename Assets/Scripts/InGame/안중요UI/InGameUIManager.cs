using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager instance;

    [Header("UI 패널")]
    public GameObject unitInfoPanel;

    [Header("유닛 기본 정보")]
    public Image unitIcon;
    public TMP_Text nameText;
    public TMP_Text gradeText;
    public TMP_Text damageText;
    public TMP_Text attackSpeedText;
    public TMP_Text attackRangeText;

    [Header("스킬 시스템 (버튼 방식)")]
    public Transform skillContentParent;
    public GameObject skillPrefab;
    public Transform skillButtonParent;
    public GameObject skillButtonPrefab;

    private Unit currentUnit;
    private float lastDmg, lastSpd, lastRng;

    // [실시간 갱신을 위한 변수]
    private int currentSkillIndex = 0;    // 현재 보고 있는 스킬 번호
    private float lastBonusChance;       // 마지막으로 확인한 보너스 확률
    private TMP_Text currentSkillDescText; // 현재 생성된 설명창의 텍스트 컴포넌트 캐싱

    void Awake()
    {
        if (instance == null) instance = this;
        unitInfoPanel.SetActive(false);
    }

    void Update()
    {
        if (unitInfoPanel.activeSelf && currentUnit != null)
        {
            // 1. 기존 스탯 실시간 감시
            float curDmg = currentUnit.combatStats.Get(StatType.Attack);
            float curSpd = currentUnit.combatStats.Get(StatType.AttackSpeed);
            float curRng = currentUnit.combatStats.Get(StatType.Range);

            if (Mathf.Abs(lastDmg - curDmg) > 0.01f ||
                Mathf.Abs(lastSpd - curSpd) > 0.01f ||
                Mathf.Abs(lastRng - curRng) > 0.01f)
            {
                UpdateStatTexts();
            }

            // 2. 스킬 확률 보너스 실시간 감시
            float curBonusChance = currentUnit.TotalSkillChanceBonus;
            if (Mathf.Abs(lastBonusChance - curBonusChance) > 0.001f)
            {
                lastBonusChance = curBonusChance;
                // 설명 텍스트만 즉시 업데이트
                RefreshCurrentSkillDescription();
            }
        }
    }

    public void ShowUnitInfo(Unit unit)
    {
        if (unit == null || unit.data == null) return;

        bool isNewUnit = (currentUnit != unit);
        currentUnit = unit;

        unitInfoPanel.SetActive(true);

        unitIcon.sprite = unit.data.unitSprite;
        nameText.text = unit.data.unitName;
        gradeText.text = unit.data.grade.ToString();

        UpdateStatTexts();

        if (isNewUnit)
        {
            currentSkillIndex = 0;
            RefreshSkillButtons();
        }
    }

    private void UpdateStatTexts()
    {
        if (currentUnit == null) return;
        string lobbySource = "LobbyLevelBonus";

        float finalDmg = currentUnit.combatStats.Get(StatType.Attack);
        float baseDmg = currentUnit.combatStats.GetBaseWithSources(StatType.Attack, lobbySource);
        float finalSpd = currentUnit.combatStats.Get(StatType.AttackSpeed);
        float baseSpd = currentUnit.combatStats.GetBaseWithSources(StatType.AttackSpeed, lobbySource);
        float finalRng = currentUnit.combatStats.Get(StatType.Range);
        float baseRng = currentUnit.combatStats.GetBaseWithSources(StatType.Range, lobbySource);

        lastDmg = finalDmg; lastSpd = finalSpd; lastRng = finalRng;

        damageText.text = FormatStat(baseDmg, finalDmg);
        attackSpeedText.text = FormatStat(baseSpd, finalSpd);
        attackRangeText.text = FormatStat(baseRng, finalRng);
    }

    private void RefreshSkillButtons()
    {
        foreach (Transform child in skillButtonParent) Destroy(child.gameObject);
        List<SkillInfo> skills = currentUnit.data.skills;

        if (skills != null && skills.Count > 0)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                int index = i;
                GameObject btnObj = Instantiate(skillButtonPrefab, skillButtonParent);
                TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = (i + 1).ToString();
                btnObj.GetComponent<Button>().onClick.AddListener(() => DisplaySkillByIndex(index));
            }
            DisplaySkillByIndex(0);
        }
        else
        {
            ClearSkillContent();
        }
    }

    private void DisplaySkillByIndex(int index)
    {
        if (currentUnit == null || currentUnit.data.skills == null) return;
        if (index < 0 || index >= currentUnit.data.skills.Count) return;

        currentSkillIndex = index;
        lastBonusChance = currentUnit.TotalSkillChanceBonus;

        ClearSkillContent();

        // 1. 스킬 프리팹 생성
        GameObject skillObj = Instantiate(skillPrefab, skillContentParent);
        var skill = currentUnit.data.skills[index];

        // 2. 이름 설정
        TMP_Text tName = skillObj.transform.Find("T_SkillName")?.GetComponent<TMP_Text>();
        if (tName != null) tName.text = skill.skillName;

        // 3. 설명 텍스트 컴포넌트 찾아서 '변수'에 저장 (캐싱)
        Transform descTransform = skillObj.transform.Find("T_SkillDesc");
        if (descTransform != null)
        {
            currentSkillDescText = descTransform.GetComponent<TMP_Text>();
        }

        // 4. 초기 설명 표시
        RefreshCurrentSkillDescription();
    }

    // 텍스트 내용만 업데이트하는 전용 함수
    private void RefreshCurrentSkillDescription()
    {
        if (currentSkillDescText == null || currentUnit == null) return;

        var skill = currentUnit.data.skills[currentSkillIndex];
        string finalDescription = skill.description;

        if (finalDescription.Contains("{chance}"))
        {
            float bonusChance = currentUnit.TotalSkillChanceBonus * 100f;
            string chanceAddText = "";

            if (bonusChance > 0.01f)
                chanceAddText = $"(<color=#0f861f>+{bonusChance:0.##}%</color>)";
            else if (bonusChance < -0.01f)
                chanceAddText = $"(<color=#ad1818>{bonusChance:0.##}%</color>)";

            finalDescription = finalDescription.Replace("{chance}", chanceAddText);
        }

        currentSkillDescText.text = finalDescription;
    }

    private void ClearSkillContent()
    {
        currentSkillDescText = null; // 참조 해제
        foreach (Transform child in skillContentParent) Destroy(child.gameObject);
    }

    string FormatStat(float uiBase, float finalValue)
    {
        float diff = finalValue - uiBase;
        if (Mathf.Abs(diff) < 0.01f) return uiBase.ToString("0.##");
        string color = diff > 0 ? "#0f861f" : "#ad1818";
        return $"{uiBase:0.##} (<color={color}>{(diff > 0 ? "+" : "")}{diff:0.##}</color>)";
    }

    public void HideUnitInfo()
    {
        currentUnit = null;
        unitInfoPanel.SetActive(false);
    }
}