using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public enum SortType { Level, Count, ID }

public class CharacterPanelManager : MonoBehaviour
{
    [Header("Config")]
    public GameObject unitCardPrefab;
    public Transform contentTransform;
    public TextMeshProUGUI sortingWith;

    private SortType currentSortType = SortType.Level;

    public void RefreshPanel()
    {
        if (LobbyUnitInfoPanelManager.Instance != null)
        {
            LobbyUnitInfoPanelManager.Instance.HideUnitInfo();
        }

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

            if (cardUI != null)
            {
                cardUI.Setup(unitData);
            }
        }
    }

    public void ToggleSort()
    {
        currentSortType = (SortType)(((int)currentSortType + 1) % 3);

        if (currentSortType == SortType.Level) sortingWith.text = "\uB808\uBCA8 \uC21C";
        else if (currentSortType == SortType.Count) sortingWith.text = "\uAC1C\uC218 \uC21C";
        else sortingWith.text = "\uC774\uB984 \uC21C";

        RefreshPanel();
    }

    private List<UnitSaveData> SortUnits(List<UnitSaveData> list, SortType sortType)
    {
        switch (sortType)
        {
            case SortType.Level:
                return list.OrderByDescending(u => u.level).ThenByDescending(u => u.count).ToList();
            case SortType.Count:
                return list.OrderByDescending(u => u.count).ToList();
            case SortType.ID:
                return list.OrderBy(u => u.unitID).ToList();
            default:
                return list;
        }
    }
}
