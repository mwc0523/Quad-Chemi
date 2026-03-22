using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnitCardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Refs")]
    public Image unitIcon;
    public Image cardFrame;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI countText;
    public Slider progressBar;

    private UnitSaveData myData;

    public void Setup(UnitSaveData saveData)
    {
        myData = saveData;

        UnitData baseData = DataManager.instance.allUnitTemplates.Find(u => u.unitName == saveData.unitID);
        if (baseData == null) return;

        nameText.text = baseData.unitName;
        unitIcon.sprite = baseData.unitSprite;
        cardFrame.color = GetColorByGrade(baseData.grade);

        levelText.text = $"Lv.{saveData.level}";

        int required = saveData.GetRequiredCount();
        countText.text = $"{saveData.count}/{required}";
        progressBar.value = (float)saveData.count / required;
        if (saveData.level >= 50)
        {
            countText.text = "MAX";
            progressBar.value = 1f;
        }
    }

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

    public void OnCardClicked()
    {
        if (myData == null) return;

        if (LobbyUnitInfoPanelManager.Instance != null)
        {
            LobbyUnitInfoPanelManager.Instance.ShowUnitInfo(myData);
        }
        else
        {
            Debug.LogWarning("LobbyUnitInfoPanelManager 인스턴스를 찾을 수 없습니다.");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        OnCardClicked();
    }
}
