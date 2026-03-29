using System;
using System.Collections.Generic;
using UnityEngine;

// 1. 재화 종류를 Enum으로 관리 (추가될 때마다 여기에 이름만 넣으면 됩니다)
public enum CurrencyType
{
    Ticket,     // 입장 티켓
    Essence,    // 정수 (기본 재화)
    Aether      // 에테르 (유료 재화)
} 

// 2. 유닛 개별 정보 (확장성을 고려한 구조)
[Serializable]
public class UnitSaveData
{
    public string unitID;     // 유닛 고유 ID (예: "FireNemo", "WaterNemo")
    public int level;         // 유닛 레벨
    public int count;         // 보유 개수 (합성/진화 등에 사용)
    public int totalCount;         
    public string weaponID;   // 나중에 전용 무기가 생길 경우를 대비한 확장 변수

    private UnitGrade GetGrade()
    {
        // DataManager에서 unitID로 원본 템플릿을 찾아 등급을 반환합니다.
        var data = DataManager.instance.allUnitTemplates.Find(u => u.unitName == unitID);
        return (data != null) ? data.grade : UnitGrade.Low;
    }

    public int GetRequiredCount()
    {
        // 1. 기본 조각 공식 (현재 유지)
        float baseCount = 5 + (level * 3) + (level * level * 0.5f);

        // 2. 등급별 조각 가중치 (고급일수록 적게 필요)
        float gradeModifier = 1f;
        switch (GetGrade())
        {
            case UnitGrade.Low: gradeModifier = 1.0f; break; // 현상 유지
            case UnitGrade.Middle: gradeModifier = 0.9f; break;
            case UnitGrade.High: gradeModifier = 0.75f; break;
            case UnitGrade.Epic: gradeModifier = 0.6f; break;
            case UnitGrade.Legend: gradeModifier = 0.45f; break;
            case UnitGrade.Myth: gradeModifier = 0.3f; break; // 신화는 조각이 매우 귀하므로 적게 소모
        }

        return Mathf.RoundToInt(baseCount * gradeModifier);
    }

    public long GetRequiredEssence()
    {
        // 1. 기본 정수 공식
        float baseEssence = 50 + (level * 20) + (level * level * 5);

        // 2. 등급별 정수 가중치 (고급일수록 기하급수적으로 비싸짐)
        float priceModifier = 1f;
        switch (GetGrade())
        {
            case UnitGrade.Low: priceModifier = 0.5f; break;  // 하급은 매우 저렴하게
            case UnitGrade.Middle: priceModifier = 1.0f; break;
            case UnitGrade.High: priceModifier = 2.5f; break;
            case UnitGrade.Epic: priceModifier = 8.0f; break;
            case UnitGrade.Legend: priceModifier = 25.0f; break;
            case UnitGrade.Myth: priceModifier = 100.0f; break; // 신화는 '돈(정수)'으로 바르는 수준
        }

        return (long)Mathf.Round(baseEssence * priceModifier);
    }
    public float GetDamageMultiplier()
    {
        if (level <= 1) return 1f;

        // 1. 등급별 성장 가중치 (신화가 1.0 기준, 하급으로 갈수록 낮아짐)
        float growthWeight = 1.0f;
        switch (GetGrade())
        {
            case UnitGrade.Low: growthWeight = 0.71f; break; // 하급: 성장세가 매우 완만함
            case UnitGrade.Middle: growthWeight = 0.78f; break;
            case UnitGrade.High: growthWeight = 0.85f; break;
            case UnitGrade.Epic: growthWeight = 0.92f; break;
            case UnitGrade.Legend: growthWeight = 0.96f; break;
            case UnitGrade.Myth: growthWeight = 1.00f; break; // 신화: 기존 공식 그대로 (100% 효율)
        }

        // 2. 기존 공식을 가중치에 따라 변형
        // 선형 성장 부분(0.1)과 지수 성장 부분(1.115) 모두에 가중치를 적용합니다.
        float linearPart = 1f + (level - 1) * (0.1f * growthWeight);
        float exponentialPart = Mathf.Pow(1f + (0.115f * growthWeight), level - 1);

        return linearPart * exponentialPart;
    }

    public UnitSaveData(string id)
    {
        unitID = id;
        level = 1;
        count = 0;
        totalCount = 0;
        weaponID = "";
    }
}

// 3. 설정 정보
[Serializable]
public class SettingsData
{
    public float masterVolume = 1f;
    public bool isPushNotificationOn = true;
}

// 4. 최상위 유저 데이터 (서버나 파일로 저장될 '본체')
[Serializable]
public class UserProfile
{
    public string nickname = "Player";
    public int playerLevel = 1;
    public int currentExp = 0;
    public int totalExp = 0;

    // 재화 관리는 Dictionary가 편하지만, Unity 기본 JsonUtility는 Dictionary를 
    // 기본 지원하지 않으므로 List나 별도의 직렬화 패키지(Newtonsoft.Json)를 권장합니다.
    // 여기서는 확장성을 위해 List 형태의 보관함을 사용하거나 직접 명시합니다.
    public int ticket = 10;
    public const int MAX_TICKET = 10;
    public const int CHARGE_INTERVAL_MINUTES = 30;
    public long essence = 0; // 골드류는 나중에 수치가 커질 수 있어 long 권장
    public int aether = 0;

    [NonSerialized] // 이 변수는 JsonUtility로 저장하지 않습니다.
    public int currentRunReachedWave;
    [NonSerialized]
    public bool isCurrentRunClear;


    [Header("Stage Progress")]
    // -1: 아무 테마도 클리어 못함 / 0: 바위산 클리어 / ... / 4: 공허 클리어
    public int highestClearedTheme = -1;
    public int highestReachedStage = 1; // 최고 도달 단계 (1~5)
    public int highestReachedRound = 0; // 최고 도달 라운드 (1~100)

    public int selectedTheme = 0; // 현재 선택된 테마 (0:바위산 ~ 4:공허)
    public int selectedStage = 1;








    // 티켓 회복을 위한 마지막 접속 시간 기록
    public string lastTicketChargeTime = "";
    public string lastShopRefreshDate = "";  // 마지막 상점 갱신 날짜
    public int dailyShopRefreshCount = 0;    // 오늘 수동 새로고침 한 횟수
    public List<ShopItemData> savedDailyShop = new List<ShopItemData>();

    // 보유 유닛 목록
    public List<UnitSaveData> unitList = new List<UnitSaveData>();

    // 설정 데이터
    public SettingsData settings = new SettingsData();

    public int GetRequiredExp(int level)
    {
        return 100 + ((level - 1) * 50) + ((level - 1) * (level - 1) * 3);
    }
    public void AddExp(int amount)
    {
        totalExp += amount;
        currentExp += amount;
        while (currentExp >= GetRequiredExp(playerLevel))
        {
            currentExp -= GetRequiredExp(playerLevel); // 요구치만큼 깎고
            playerLevel++;                             // 레벨업!

            // TODO: 나중에 레벨업 보상(티켓 충전, 재화 지급 등)이 있다면 여기에 추가
            Debug.Log($"레벨업! 현재 레벨: {playerLevel}");
        }
    }
}