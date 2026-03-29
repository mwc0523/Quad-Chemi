using UnityEngine;
using TMPro;
using UnityEngine.UI;

public enum CardEffectID // 카드 종류 모음
{
    // 하급
    Low_FireSpeed,        // 발화점
    Low_WaterDuration,    // 습기 보존
    Low_EarthStun,        // 단단한 지반
    Low_RangeUp,          // 가벼운 산들바람
    Low_MoreIsBetter,     // 다다익선
    Low_RecycleBasic,     // 재활용(하급)
    Low_FireballBoost,    // 강화된 불꽃
    Low_BubbleSlow,       // 비눗방울
    Low_QuakeChance,      // 지진파
    Low_Tailwind,         // 순풍

    // 중급
    Mid_NutrientSupply,       // 영양분 공급
    Mid_HighPressureSteam,   // 고압 증기
    Mid_BladeIce,            // 칼날 얼음
    Mid_LavaEruption,        // 열기 분출
    Mid_QuickSand,           // 유사
    Mid_StaticShock,         // 정전기
    Mid_ElementReverse,      // 원소 역전
    Mid_FastAttack,          // 빠른 중급 공격
    Mid_ElementBalance,      // 원소 평형
    Mid_RecycleAdvanced,     // 재활용(중급)
    Mid_EmergencySupply,    // 긴급 수급 1

    // 상급
    High_SuperTyphoon,       // 초대형 태풍
    High_MeteorShower,       // 혜성 낙하
    High_WorldTreeSprout,    // 세계수의 싹
    High_Overload,           // 과부하
    High_FrozenLand,         // 동토의 땅
    High_RockBreak,          // 암석 파쇄
    High_CriticalChance,     // 치명적 확률
    High_ElementBalance2,    // 원소 평형 2
    High_BonusReward,        // 보상 증가
    High_EmergencySupply2,  // 긴급 수급 2
    High_FateCard,          // 운명의 카드 

    // 서사
    Epic_DeadlyToxin,            // 치명적인 독소
    Epic_IronDefense,            // 철벽 방어
    Epic_SolarSystem,            // 태양계 형성
    Epic_PermanentFrost,         // 영구 동토
    Epic_TeslaCoil,              // 테슬라 코일
    Epic_Tsunami,                // 대해일
    Epic_CollectiveIntelligence, // 집단 지성
    Epic_Alchemy,                // 재화의 연금술
    Epic_ElementBalance3,        // 원소 평형 3
    Epic_EmergencySupply3,        // 긴급 수급 3
    Epic_FateCard2,             // 운명의 카드 2

    // 전설
    Legendary_WorldTreeBlessing, // 세계수의 가호
    Legendary_FinalJudgement,    // 최후의 심판
    Legendary_EventHorizon,      // 이벤트 호라이슨
    Legendary_AbsoluteZero,      // 절대 영역
    Legendary_DivinePunishment,  // 천벌
    Legendary_GiantsShoulder,    // 거인의 어깨
    Legendary_FateCard3,          // 운명의 카드 3

    // 신화
    Myth_BeginningOfEnd,     // 종말의 시작
    Myth_AbyssCall,          // 심연의 부름
    Myth_SpearOfRagnarok,    // 라그나로크의 창
    Myth_InfiniteLoop,       // 무한의 굴레
    Myth_PrimordialLight,    // 태초의 빛
    Myth_GoldMine,           // 황금 광산
    Myth_MythRebirth,        // 신화의 재림
    Myth_AscensionTrigger    // 동일 16마리 → 신화 2 소환
}

[System.Serializable]
public enum CardGrade { Low, Mid, High, Epic, Legendary, Myth }

[System.Serializable]
public class CardData
{
    public CardEffectID id;
    public string name;
    public string desc;
    public CardGrade grade;

    public CardData(CardEffectID id, string name, string desc, CardGrade grade)
    {
        this.id = id; this.name = name; this.desc = desc; this.grade = grade;
    }
}

public class CardSlotUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public Button selectButton;
    public Button rerollButton;
    public Image backgroundImage;

    private CardEffectID currentEffectID;
    private CardUIManager uiManager; // 전체를 관리하는 매니저

    public void Setup(CardUIManager manager, CardData data)
    {
        uiManager = manager;
        currentEffectID = data.id;

        nameText.text = data.name;
        descText.text = data.desc;

        // 이제 data가 매개변수에 있으므로 data.grade를 찾을 수 있습니다.
        backgroundImage.color = GetColorByGrade(data.grade);

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnClickSelect);

        rerollButton.onClick.RemoveAllListeners();
        rerollButton.onClick.AddListener(OnClickReroll);
    }

    void OnClickSelect() => uiManager.OnCardSelected(currentEffectID);
    void OnClickReroll() => uiManager.OnCardRerolled(this);

    private Color GetColorByGrade(CardGrade grade)
    {
        return grade switch
        {
            CardGrade.Low => new Color(0.85f, 0.85f, 0.85f),
            CardGrade.Mid => new Color(0.55f, 0.85f, 0.55f),
            CardGrade.High => new Color(0.5f, 0.75f, 1f),
            CardGrade.Epic => new Color(0.75f, 0.5f, 0.95f),
            CardGrade.Legendary => new Color(1f, 0.85f, 0.3f),
            CardGrade.Myth => new Color(1f, 0.5f, 0.5f),
            _ => Color.white
        };
    }
}