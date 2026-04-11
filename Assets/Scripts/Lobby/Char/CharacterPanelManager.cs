using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public enum SortType
{
    GradeAsc,           // 등급 오름차순 (낮은 등급 우선)
    GradeDesc,          // 등급 내림차순 (높은 등급 우선)
    LevelAsc,           // 레벨 오름차순
    LevelDesc,          // 레벨 내림차순
    GradeLevelAsc,      // 등급별 레벨 오름차순
    GradeLevelDesc      // 등급별 레벨 내림차순
}

public class CharacterPanelManager : MonoBehaviour
{
    public static CharacterPanelManager instance;

    [Header("Config")]
    public GameObject unitCardPrefab;
    public Transform contentTransform;
    public TMP_Dropdown sortDropdown; // Inspector에서 드롭다운 연결

    private SortType currentSortType = SortType.GradeAsc; // 기본값 레벨 내림차순

    void Awake() => instance = this;
    void Start()
    {
        if (sortDropdown != null)
        {
            // 옵션 목록을 싹 비우고 Enum 순서대로 다시 확실히 채워줍니다. (인스펙터 실수 방지)
            sortDropdown.ClearOptions();
            List<string> options = new List<string> {
            "등급 낮은 순", "등급 높은 순", "레벨 낮은 순",
            "레벨 높은 순", "등급별 레벨 낮은 순", "등급별 레벨 높은 순"
        };
            sortDropdown.AddOptions(options);

            // 초기값 설정 및 화면 갱신
            sortDropdown.value = (int)currentSortType;
            sortDropdown.RefreshShownValue(); // 이걸 호출해야 현재 선택된 텍스트가 표시됩니다!

            RefreshPanel();
        }
    }

    // 드롭다운의 OnValueChanged에 이 함수를 연결하세요
    public void OnSortDropdownChanged(int index)
    {
        //Debug.Log($"정렬 변경됨! 선택된 인덱스: {index}");
        currentSortType = (SortType)index;
        RefreshPanel();
    }

    public void RefreshPanel()
    {
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        if (DataManager.instance == null) return;

        List<UnitSaveData> myUnits = DataManager.instance.currentUser.unitList;
        List<UnitSaveData> sortedList = SortUnits(myUnits, currentSortType);

        foreach (UnitSaveData unitData in sortedList)
        {
            GameObject cardObj = Instantiate(unitCardPrefab, contentTransform);
            UnitCardUI cardUI = cardObj.GetComponent<UnitCardUI>();
            if (cardUI != null) cardUI.Setup(unitData);
        }
    }

    private List<UnitSaveData> SortUnits(List<UnitSaveData> list, SortType sortType)
    {
        // 등급 정보를 참조하기 위해 헬퍼 함수 활용
        System.Func<UnitSaveData, int> getGrade = (u) =>
        {
            var template = DataManager.instance.allUnitTemplates.Find(t => t.unitName == u.unitID);
            return (template != null) ? (int)template.grade : -1;
        };

        switch (sortType)
        {
            case SortType.GradeAsc: // 등급 오름차순
                return list.OrderBy(getGrade).ThenByDescending(u => u.level).ToList();

            case SortType.GradeDesc: // 등급 내림차순
                return list.OrderByDescending(getGrade).ThenByDescending(u => u.level).ToList();

            case SortType.LevelAsc: // 레벨 오름차순
                return list.OrderBy(u => u.level).ThenByDescending(u => u.count).ToList();

            case SortType.LevelDesc: // 레벨 내림차순
                return list.OrderByDescending(u => u.level).ThenByDescending(u => u.count).ToList();

            case SortType.GradeLevelAsc: // 등급별 레벨 오름차순
                return list.OrderByDescending(getGrade)
                           .ThenBy(u => u.level)
                           .ThenByDescending(u => u.count).ToList();

            case SortType.GradeLevelDesc: // 등급별 레벨 내림차순
                return list.OrderByDescending(getGrade)
                           .ThenByDescending(u => u.level)
                           .ThenByDescending(u => u.count).ToList();

            default:
                return list;
        }
    }
}