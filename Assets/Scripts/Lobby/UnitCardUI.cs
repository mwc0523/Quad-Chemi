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
    public Image progressImage;
    public Image UpgradeArrow;

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
        float progressRatio = (float)saveData.count / required;

        // 기본 텍스트 및 슬라이더 값 설정
        countText.text = $"{saveData.count}/{required}";
        progressBar.value = Mathf.Clamp01(progressRatio);

        // ★ 레벨 50 이상(MAX) 예외 처리
        if (saveData.level >= 50)
        {
            countText.text = "MAX";
            progressBar.value = 1f;
            progressRatio = 1f; // 색상 판정을 위해 1로 고정
        }

        if(progressRatio >= 1f && saveData.level < 50) UpgradeArrow.gameObject.SetActive(true);
        else UpgradeArrow.gameObject.SetActive(false);

        // 슬라이더 색상 변경
        if (progressImage != null)
        {
            // 1 이상(또는 MAX)이면 초록색, 아니면 주황색(245, 113, 0)
            progressImage.color = (progressRatio >= 1f) ?
                Color.green : new Color(245f / 255f, 113f / 255f, 0f / 255f);
        }
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
