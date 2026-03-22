using UnityEngine;

// 1. 무엇을 파는가? (확장성을 위해 Enum 사용)
public enum ShopItemType
{
    Unit,       // 유닛 조각
    Currency,   // 정수, 에테르, 티켓 등
    Material    // 나중에 추가될 재료(광산 열쇠 등)
}

// 2. 무엇으로 사는가?
public enum CostType
{
    Essence,
    Aether,
    Ad        // 광고 보고 무료 획득
}

// 3. 상품 정보 설계도
[System.Serializable]
public class ShopItemData
{
    public ShopItemType itemType;
    public string itemID;      // 유닛이면 "FireNemo", 재화면 "Essence" 등
    public int amount;         // 지급 개수 (유닛 10장, 정수 500개 등)

    public CostType costType;
    public int costAmount;     // 가격

    public bool isSoldOut;     // 구매 완료 여부

    public UnitGrade unitGrade;
}