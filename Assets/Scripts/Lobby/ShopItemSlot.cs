using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemSlot : MonoBehaviour
{
    public ShopItemData myData;

    [Header("UI 연결")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI amountText; // "X5" 같은 수량 표시용
    public TextMeshProUGUI costText;
    public Image costIcon;             // 재화 아이콘 (정수/에테르)

    [Header("진행도(유닛 전용)")]
    public GameObject progressContainer;
    public Slider progressSlider;        // 추가: 슬라이더 제어용
    public Image progressImage2;
    public TextMeshProUGUI progressText;

    [Header("상태 표시")]
    public GameObject soldOutDim;

    public void SetupSlot(ShopItemData data)
    {
        myData = data;

        // 1. 유닛 데이터 찾기 (DataManager 활용)
        UnitData unitTemplate = null;
        if (data.itemType == ShopItemType.Unit)
        {
            unitTemplate = DataManager.instance.allUnitTemplates.Find(u => u.unitName == data.itemID);
        }

        // 2. 기본 정보 세팅
        // 유닛이면 템플릿의 이름을, 아니면 ID를 그대로 사용
        nameText.text = (unitTemplate != null) ? unitTemplate.unitName : data.itemID;
        amountText.text = $"X{data.amount}";
        costText.text = data.costAmount.ToString("#,###");
        soldOutDim.SetActive(data.isSoldOut);

        // 3. 아이콘 세팅 (Resources.Load 대신 직접 참조)
        if (unitTemplate != null)
            iconImage.sprite = unitTemplate.unitSprite;
        else
            LoadNonUnitIcon(data.itemID); // 재화 아이콘은 따로 로드

        SetCostIcon(data.costType);

        // 4. 유닛 전용 UI (슬라이더 등)
        if (data.itemType == ShopItemType.Unit)
        {
            progressContainer.SetActive(true);
            UpdateUnitProgress();
        }
        else
        {
            progressContainer.SetActive(false);
        }
    }

    private void UpdateUnitProgress()
    {
        UnitSaveData myUnit = DataManager.instance.currentUser.unitList.Find(u => u.unitID == myData.itemID);

        //Debug.Log($"[Shop] {myData.itemID} 진행도 업데이트 시도. 찾음: {myUnit != null}");
        int required = myUnit.GetRequiredCount();
        float progressRatio = (float)myUnit.count / required;
        int currentLevel = myUnit != null ? myUnit.level : 1;

        // 텍스트 갱신
        progressText.text = $"{myUnit.count} / {required}";

        // 슬라이더 갱신 (0~1 사이 값)
        progressSlider.value = Mathf.Clamp01(progressRatio);

        progressImage2.color = (progressRatio >= 1f) ? Color.green : new Color(245f / 255f, 113f / 255f, 0f / 255f);
    }

    private void LoadNonUnitIcon(string id)
    {
        // 예: "Essence", "Aether" 아이콘 로드
        Sprite s = Resources.Load<Sprite>($"Icons/{id}");
        if (s != null) iconImage.sprite = s;
    }

    private void SetCostIcon(CostType type)
    {
        // 재화 타입에 맞는 아이콘도 Resources에서 로드하거나 인스펙터 배열에서 할당
        string iconName = (type == CostType.Essence) ? "정수" : "에테르";
        costIcon.sprite = Resources.Load<Sprite>($"Icons/{iconName}");
    }

    private string GetItemName(string id)
    {
        // 나중에 데이터 시트(CSV/JSON)를 만들면 여기서 ID를 이름으로 치환합니다.
        if (id == "FireNemo") return "불네모";
        if (id == "WaterNemo") return "물네모";
        return id;
    }

    public void OnClickBuy()
    {
        if (myData.isSoldOut) return;
        FindObjectOfType<ShopManager>().AttemptPurchase(this);
    }
}