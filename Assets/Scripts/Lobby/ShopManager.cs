using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using Random = UnityEngine.Random;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("UI Grids")]
    public Transform dailyShopGrid;   // 일일 랜덤 (9칸)
    public Transform essenceShopGrid; // 정수 상점 (3칸)
    public Transform aetherShopGrid;  // 에테르 상점 (3칸)

    public GameObject shopSlotPrefab;
    public TMP_Text refreshBtnText;

    // 등급별 등장 확률 (합계 1.0 = 100%)
    private Dictionary<UnitGrade, float> gradeProbabilities = new Dictionary<UnitGrade, float>()
    {
        { UnitGrade.Low, 0.40f },
        { UnitGrade.Middle, 0.30f },
        { UnitGrade.High, 0.16f },
        { UnitGrade.Epic, 0.08f },
        { UnitGrade.Legend, 0.04f },
        { UnitGrade.Myth, 0.02f }
    };

    void Start()
    {
        // 기존 RefreshAllShops() 대신 서버 시간을 가져오는 코루틴 실행
        StartCoroutine(InitShopWithServerTime());
    }

    private IEnumerator InitShopWithServerTime()
    {
        // 한국 표준시(KST)를 반환하는 무료 공용 API 호출
        UnityWebRequest req = UnityWebRequest.Get("https://worldtimeapi.org/api/timezone/Asia/Seoul");
        yield return req.SendWebRequest();

        DateTime currentTime = DateTime.Now; // 통신 실패 시 기본값으로 기기 시간 사용

        if (req.result == UnityWebRequest.Result.Success)
        {
            // JSON 응답에서 날짜("YYYY-MM-DD") 부분만 간단히 파싱
            string json = req.downloadHandler.text;
            int startIndex = json.IndexOf("datetime\":\"") + 11;
            if (startIndex > 10)
            {
                string dateStr = json.Substring(startIndex, 10);
                DateTime.TryParse(dateStr, out currentTime);
            }
        }

        string todayStr = currentTime.ToString("yyyy-MM-dd");
        var user = DataManager.instance.currentUser;

        // 날짜가 바뀌었거나, 오늘 저장된 상점 목록이 아예 없으면 새로 갱신
        if (user.lastShopRefreshDate != todayStr || user.savedDailyShop.Count == 0)
        {
            user.lastShopRefreshDate = todayStr;
            user.dailyShopRefreshCount = 0; // 새로고침 횟수 초기화
            GenerateDailyShop();
        }
        else
        {
            // 날짜가 안 바뀌었다면 유저 데이터에 저장된 어제(오늘) 상점 불러오기
            LoadSavedDailyShop();
        }
        refreshBtnText.text = $"새로고침 {10 - user.dailyShopRefreshCount}/10";
        // 정수와 에테르 상점은 고정이므로 그대로 생성
        GenerateFixedShop(essenceShopGrid, ShopItemType.Currency, "정수", 3);
        GenerateFixedShop(aetherShopGrid, ShopItemType.Currency, "에테르", 3);
    }
    // 1. 일일 랜덤 상점 (확률 적용)
    public void GenerateDailyShop()
    {
        ClearGrid(dailyShopGrid);
        var user = DataManager.instance.currentUser;
        user.savedDailyShop.Clear(); // 기존 목록 비우기

        for (int i = 0; i < 9; i++)
        {
            UnitGrade selectedGrade = GetRandomGrade();
            UnitData randomUnit = GetRandomUnitByGrade(selectedGrade);
            if (randomUnit == null) continue;

            int randomAmount = GetRandomAmountByGrade(selectedGrade);
            int unitPrice = CalculateUnitPrice(selectedGrade);

            ShopItemData newItem = new ShopItemData
            {
                itemType = ShopItemType.Unit,
                itemID = randomUnit.unitName,
                unitGrade = selectedGrade,
                amount = randomAmount,
                costType = CostType.Essence,
                costAmount = randomAmount * unitPrice,
                isSoldOut = false
            };

            user.savedDailyShop.Add(newItem); // 유저 데이터에 저장
            CreateSlot(dailyShopGrid, newItem); // UI 생성
        }

        DataManager.instance.SaveData(); // 갱신된 내역 서버(또는 로컬)에 저장
    }

    private void LoadSavedDailyShop()
    {
        ClearGrid(dailyShopGrid);
        var user = DataManager.instance.currentUser;
        foreach (var item in user.savedDailyShop)
        {
            CreateSlot(dailyShopGrid, item);
        }
    }

    public void OnClickManualRefresh()
    {
        var user = DataManager.instance.currentUser;

        if (user.dailyShopRefreshCount >= 10)
        {
            Debug.LogWarning("오늘 새로고침 횟수를 모두 소진했습니다. (10/10)");
            return;
        }

        if (user.aether < 10)
        {
            Debug.LogWarning("에테르가 부족합니다.");
            return;
        }

        // 재화 차감 및 횟수 증가
        user.aether -= 10;
        user.dailyShopRefreshCount++;

        // 상점 새로 돌리기 및 저장
        GenerateDailyShop();

        // 상단 UI 재화 갱신 (선택 사항)
        UIManager ui = FindObjectOfType<UIManager>();
        if (ui != null) ui.RefreshTopBar();
        refreshBtnText.text = $"새로고침 {10 - user.dailyShopRefreshCount}/10";

        Debug.Log($"상점 새로고침 완료! 남은 횟수: {10 - user.dailyShopRefreshCount}/10");
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
    private int GetRandomAmountByGrade(UnitGrade grade)
    {
        switch (grade)
        {
            // 하급: 40% 확률로 등장. 조각이 너무 흔해지지 않게 조정 (필요량 2.3만)
            case UnitGrade.Low: return UnityEngine.Random.Range(30, 61);

            // 중급: 30% 확률. (필요량 2만)
            case UnitGrade.Middle: return UnityEngine.Random.Range(20, 41);

            // 상급: 16% 확률. (필요량 1.7만)
            case UnitGrade.High: return UnityEngine.Random.Range(15, 31);

            // 서사: 8% 확률. 여기서부터는 '득템' 느낌이 나야 함 (필요량 1.3만)
            case UnitGrade.Epic: return UnityEngine.Random.Range(10, 21);

            // 전설: 4% 확률. (필요량 1만)
            case UnitGrade.Legend: return UnityEngine.Random.Range(8, 16);

            // 신화: 2% 확률. 귀한 만큼 한 번 떴을 때 확실한 체감이 필요 (필요량 7천)
            case UnitGrade.Myth: return UnityEngine.Random.Range(5, 11);

            default: return 10;
        }
    }

    private int CalculateUnitPrice(UnitGrade grade)
    {
        // 등급별 '1개당' 가격 (단가)
        switch (grade)
        {
            case UnitGrade.Low: return 10;   // 개당 10 (50개면 500)
            case UnitGrade.Middle: return 30;   // 개당 30
            case UnitGrade.High: return 100;  // 개당 100
            case UnitGrade.Epic: return 500;  // 개당 500
            case UnitGrade.Legend: return 2000; // 개당 2000
            case UnitGrade.Myth: return 5000; // 개당 5000
            default: return 100;
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