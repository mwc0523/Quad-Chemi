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
    public Transform skillContentParent; // 스킬 아이템 프리팹이 생성될 부모
    public GameObject skillPrefab;       // T_SkillName, T_SkillDesc가 포함된 프리팹
    public Transform skillButtonParent;  // 숫자 버튼(1, 2, 3)들이 들어갈 부모
    public GameObject skillButtonPrefab; // 숫자만 적힌 작은 버튼 프리팹

    private UnitData currentData; // 버튼 클릭 시 참조할 현재 유닛 데이터

    void Awake()
    {
        if (instance == null) instance = this;
        unitInfoPanel.SetActive(false);
    }

    public void ShowUnitInfo(UnitData data)
    {
        unitInfoPanel.SetActive(true);
        currentData = data; // 데이터 저장

        // 기본 정보 채우기
        if (unitIcon != null) unitIcon.sprite = data.unitSprite;
        nameText.text = data.unitName;
        gradeText.text = data.grade.ToString();
        damageText.text = data.damage.ToString();
        attackSpeedText.text = data.attackSpeed.ToString();

        // 1. 기존 버튼들 싹 지우기
        foreach (Transform child in skillButtonParent) Destroy(child.gameObject);

        // 2. 스킬 개수만큼 '선택 버튼' 생성
        if (data.skills != null && data.skills.Count > 0)
        {
            for (int i = 0; i < data.skills.Count; i++)
            {
                int index = i; // 클로저 방지
                GameObject btnObj = Instantiate(skillButtonPrefab, skillButtonParent);
                btnObj.GetComponentInChildren<TMP_Text>().text = "Skill " + (i + 1).ToString();

                // 버튼을 누르면 해당 인덱스의 스킬 프리팹을 띄움
                btnObj.GetComponent<Button>().onClick.AddListener(() => DisplaySkillByIndex(index));
            }

            // 기본적으로 1번 스킬(0번 인덱스)을 먼저 보여줌
            DisplaySkillByIndex(0);
        }
    }

    // 핵심: 프리팹을 하나만 생성해서 내용을 채우는 함수
    private void DisplaySkillByIndex(int index)
    {
        if (currentData == null || currentData.skills == null) return;

        // 기존에 떠있던 스킬 프리팹 지우기
        foreach (Transform child in skillContentParent) Destroy(child.gameObject);

        // 새 스킬 프리팹 하나 생성
        GameObject skillObj = Instantiate(skillPrefab, skillContentParent);
        var skill = currentData.skills[index];

        // 님 프리팹의 실제 자식 이름(T_SkillName, T_SkillDesc)으로 찾기
        skillObj.transform.Find("T_SkillName").GetComponent<TMP_Text>().text = skill.skillName;
        skillObj.transform.Find("T_SkillDesc").GetComponent<TMP_Text>().text = skill.description;
    }

    public void HideUnitInfo() => unitInfoPanel.SetActive(false);
}