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
    // 실시간 수치 비교를 위한 변수들
    private float lastDmg, lastSpd, lastRng;

    void Awake()
    {
        if (instance == null) instance = this;
        unitInfoPanel.SetActive(false);
    }

    void Update()
    {
        // 1. 실시간 수치 반영: 창이 열려있을 때만 스탯 변화 감시
        if (unitInfoPanel.activeSelf && currentUnit != null)
        {
            float curDmg = currentUnit.combatStats.Get(StatType.Attack);
            float curSpd = currentUnit.combatStats.Get(StatType.AttackSpeed);
            float curRng = currentUnit.combatStats.Get(StatType.Range);

            // 하나라도 변했다면 텍스트 갱신
            if (Mathf.Abs(lastDmg - curDmg) > 0.01f ||
                Mathf.Abs(lastSpd - curSpd) > 0.01f ||
                Mathf.Abs(lastRng - curRng) > 0.01f)
            {
                UpdateStatTexts();
            }
        }
    }

    public void ShowUnitInfo(Unit unit)
    {
        if (unit == null || unit.data == null) return;

        bool isNewUnit = (currentUnit != unit);
        currentUnit = unit;

        unitInfoPanel.SetActive(true);

        // 기본 정보 셋팅
        unitIcon.sprite = unit.data.unitSprite;
        nameText.text = unit.data.unitName;
        gradeText.text = unit.data.grade.ToString();

        // 스탯 초기화 및 즉시 반영
        UpdateStatTexts();

        // 2. 유닛이 바뀔 때만 스킬 버튼들 새로 생성
        if (isNewUnit)
        {
            RefreshSkillButtons();
        }
    }

    private void UpdateStatTexts()
    {
        if (currentUnit == null) return;

        lastDmg = currentUnit.combatStats.Get(StatType.Attack);
        lastSpd = currentUnit.combatStats.Get(StatType.AttackSpeed);
        lastRng = currentUnit.combatStats.Get(StatType.Range);

        damageText.text = FormatStat(currentUnit.data.damage, lastDmg);
        attackSpeedText.text = FormatStat(currentUnit.data.attackSpeed, lastSpd);
        attackRangeText.text = FormatStat(currentUnit.data.attackRange, lastRng);
    }

    private void RefreshSkillButtons()
    {
        foreach (Transform child in skillButtonParent) Destroy(child.gameObject);

        // 에러 해결 포인트: SkillData 대신 SkillInfo 사용
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
            DisplaySkillByIndex(0); // 첫 번째 스킬 기본 표시
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

        ClearSkillContent();

        GameObject skillObj = Instantiate(skillPrefab, skillContentParent);
        var skill = currentUnit.data.skills[index];

        // 프리팹 내부의 텍스트 오브젝트 이름 찾기
        TMP_Text tName = skillObj.transform.Find("T_SkillName")?.GetComponent<TMP_Text>();
        TMP_Text tDesc = skillObj.transform.Find("T_SkillDesc")?.GetComponent<TMP_Text>();

        if (tName != null) tName.text = skill.skillName;
        if (tDesc != null) tDesc.text = skill.description;
    }

    private void ClearSkillContent()
    {
        foreach (Transform child in skillContentParent) Destroy(child.gameObject);
    }

    string FormatStat(float baseValue, float finalValue)
    {
        float diff = finalValue - baseValue;
        if (Mathf.Abs(diff) < 0.01f) return baseValue.ToString("0.##");

        string color = diff > 0 ? "#0f861f" : "#ad1818";
        return $"{baseValue:0.##} (<color={color}>{(diff > 0 ? "+" : "")}{diff:0.##}</color>)";
    }

    public void HideUnitInfo()
    {
        currentUnit = null;
        unitInfoPanel.SetActive(false);
    }
}