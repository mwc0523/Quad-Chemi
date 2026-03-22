using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("UI Grids")]
    public Transform dailyShopGrid;   // 일일 랜덤 (9칸)
    public Transform essenceShopGrid; // 정수 상점 (3칸)
    public Transform aetherShopGrid;  // 에테르 상점 (3칸)

    public GameObject shopSlotPrefab;

    // 등급별 등장 확률 (합계 1.0 = 100%)
    private Dictionary<UnitGrade, float> gradeProbabilities = new Dictionary<UnitGrade, float>()
    {
        { UnitGrade.Low, 0.45f },
        { UnitGrade.Middle, 0.30f },
        { UnitGrade.High, 0.15f },
        { UnitGrade.Epic, 0.07f },
        { UnitGrade.Legend, 0.025f },
        { UnitGrade.Myth, 0.005f }
    };

    void Start() { RefreshAllShops(); }

    public void RefreshAllShops()
    {
        GenerateDailyShop();
        GenerateFixedShop(essenceShopGrid, ShopItemType.Currency, "정수", 3);
        GenerateFixedShop(aetherShopGrid, ShopItemType.Currency, "에테르", 3);
    }

    // 1. 일일 랜덤 상점 (확률 적용)
    public void GenerateDailyShop()
    {
        ClearGrid(dailyShopGrid);

        for (int i = 0; i < 9; i++)
        {
            UnitGrade selectedGrade = GetRandomGrade();
            UnitData randomUnit = GetRandomUnitByGrade(selectedGrade);

            if (randomUnit == null) continue;

            ShopItemData newItem = new ShopItemData
            {
                itemType = ShopItemType.Unit,
                itemID = randomUnit.unitName,
                unitGrade = selectedGrade,
                amount = 10,
                costType = CostType.Essence,
                costAmount = CalculatePrice(selectedGrade), // 등급별 가격 산정
                isSoldOut = false
            };
            CreateSlot(dailyShopGrid, newItem);
        }
    }

    // 2. 고정 재화 상점 (정수, 에테르 등)
    private void GenerateFixedShop(Transform grid, ShopItemType type, string id, int count)
    {
        ClearGrid(grid);
        for (int i = 1; i <= count; i++)
        {
            ShopItemData newItem = new ShopItemData
            {
                itemType = type,
                itemID = id,
                amount = 100 * i, // 예: 100개, 200개, 300개
                costType = CostType.Aether, // 에테르로 정수를 사는 식
                costAmount = 50 * i,
                isSoldOut = false
            };
            CreateSlot(grid, newItem);
        }
    }

    // --- 헬퍼 함수들 ---

    private UnitGrade GetRandomGrade()
    {
        float roll = Random.value; // 0.0 ~ 1.0
        float cumulative = 0f;
        foreach (var pair in gradeProbabilities)
        {
            cumulative += pair.Value;
            if (roll <= cumulative) return pair.Key;
        }
        return UnitGrade.Low;
    }

    private UnitData GetRandomUnitByGrade(UnitGrade grade)
    {
        // DataManager의 전체 리스트에서 해당 등급만 필터링
        var candidateUnits = DataManager.instance.allUnitTemplates.FindAll(u => u.grade == grade);
        if (candidateUnits.Count == 0) return null;
        return candidateUnits[Random.Range(0, candidateUnits.Count)];
    }

    private int CalculatePrice(UnitGrade grade)
    {
        // 등급별 기본 가격 가중치
        switch (grade)
        {
            case UnitGrade.Low: return 100;
            case UnitGrade.High: return 500;
            case UnitGrade.Myth: return 5000;
            default: return 200;
        }
    }

    private void CreateSlot(Transform parent, ShopItemData data)
    {
        GameObject slotObj = Instantiate(shopSlotPrefab, parent);
        slotObj.GetComponent<ShopItemSlot>().SetupSlot(data);
    }

    private void ClearGrid(Transform grid)
    {
        foreach (Transform child in grid) Destroy(child.gameObject);
    }

    public void AttemptPurchase(ShopItemSlot slot)
    {
        ShopItemData item = slot.myData;

        // 재화 확인 및 차감 로직
        if (item.costType == CostType.Essence)
        {
            if (DataManager.instance.currentUser.essence >= item.costAmount)
            {
                DataManager.instance.currentUser.essence -= item.costAmount;

                // 상품 지급 (유닛일 경우)
                if (item.itemType == ShopItemType.Unit)
                {
                    AddUnit(item.itemID, item.amount);
                }

                item.isSoldOut = true;
                slot.SetupSlot(item); // UI 갱신
                // 서버 저장 및 상단 바 UI 갱신 (선택 사항)
                DataManager.instance.SaveData();
                UIManager ui = FindObjectOfType<UIManager>();
                if (ui != null)
                {
                    ui.RefreshTopBar();
                }
                Debug.Log($"{item.itemID} 구매 성공!");
            }
            else
            {
                Debug.Log("잔액이 부족합니다.");
            }
        }
    }

    private void AddUnit(string id, int amount)
    {
        var unitList = DataManager.instance.currentUser.unitList;
        var existingUnit = unitList.Find(u => u.unitID == id);

        if (existingUnit != null) {
            existingUnit.count += amount;
            existingUnit.totalCount += amount;
        }
    }
}