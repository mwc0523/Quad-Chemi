using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class CardUIManager : MonoBehaviour
{
    public static CardUIManager instance;
    public List<Unit> activeUnits = new List<Unit>(); // 현재 필드 유닛 리스트

    public GameObject cardPanel; // 카드 뽑기 전체 화면
    public CardSlotUI[] cardSlots; // 기본 3개의 슬롯 (운명의 카드를 대비해 동적 생성으로 바꿔도 됨)
    public TextMeshProUGUI cardRerollcount; //리롤 몇번 남았는지

    private int rerollCount = 2; // 남은 새로고침 횟수
    private int destinyCard1Used = 1;
    private int destinyCard2Used = 1;
    private int destinyCard3Used = 1;

    // 등급별 카드 리스트 저장소
    private Dictionary<CardGrade, List<CardData>> allCards = new Dictionary<CardGrade, List<CardData>>();

    private HashSet<CardEffectID> appliedCards = new HashSet<CardEffectID>(); // 이미 획득한 카드
    private HashSet<CardEffectID> appearedInSession = new HashSet<CardEffectID>(); // 이번 뽑기(리롤 포함)에서 등장한 카드

    // 등급별 확률 (합이 100이 되도록 설정)
    private Dictionary<CardGrade, float> gradeProbabilities = new Dictionary<CardGrade, float>()
    {
        ///*
        { CardGrade.Low, 40f },
        { CardGrade.Mid, 30f },
        { CardGrade.High, 16f },
        { CardGrade.Epic, 8f },
        { CardGrade.Legendary, 4f },
        { CardGrade.Myth, 2f }
        //*/
        /*
        { CardGrade.Low, 0f },
        { CardGrade.Mid, 100f },
        { CardGrade.High, 0f },
        { CardGrade.Epic, 0f },
        { CardGrade.Legendary, 0f },
        { CardGrade.Myth, 0f }
        */
    };

    void Awake()
    {
        if (instance == null) instance = this;
        InitializeCardDatabase(); // 카드 데이터 초기화
        cardPanel.SetActive(false);
    }

    void InitializeCardDatabase()
    {
        foreach (CardGrade grade in System.Enum.GetValues(typeof(CardGrade)))
            allCards[grade] = new List<CardData>();

        // 하급
        allCards[CardGrade.Low].Add(new CardData(CardEffectID.Low_FireSpeed, "발화점", "불네모의 공격 속도 +100%", CardGrade.Low));
        allCards[CardGrade.Low].Add(new CardData(CardEffectID.Low_WaterDuration, "습기 보존", "물네모의 스킬 지속 시간 +5초", CardGrade.Low));
        allCards[CardGrade.Low].Add(new CardData(CardEffectID.Low_EarthStun, "단단한 지반", "땅네모의 스킬 기절시간 +1.5초", CardGrade.Low));
        allCards[CardGrade.Low].Add(new CardData(CardEffectID.Low_RangeUp, "가벼운 산들바람", "공기네모의 사거리 +1", CardGrade.Low));
        allCards[CardGrade.Low].Add(new CardData(CardEffectID.Low_MoreIsBetter, "다다익선", "필드에 하급네모가 8마리 이상일 때, 모든 네모의 공격력 +20%", CardGrade.Low));
        allCards[CardGrade.Low].Add(new CardData(CardEffectID.Low_RecycleBasic, "재활용(하급)", "하급 네모의 판매 비용 4배", CardGrade.Low));
        allCards[CardGrade.Low].Add(new CardData(CardEffectID.Low_FireballBoost, "강화된 불꽃", "불네모의 스킬 사용 확률 +10% ", CardGrade.Low));
        allCards[CardGrade.Low].Add(new CardData(CardEffectID.Low_BubbleSlow, "비눗방울", "물네모의 이동속도 감소 수치 +30% 추가", CardGrade.Low));
        allCards[CardGrade.Low].Add(new CardData(CardEffectID.Low_QuakeChance, "지진파", "땅네모의 스킬 사용 확률 +5% ", CardGrade.Low));
        allCards[CardGrade.Low].Add(new CardData(CardEffectID.Low_Tailwind, "순풍", "공기네모의 스킬 사용 확률 +5% ", CardGrade.Low));

        // 중급
        allCards[CardGrade.Mid].Add(new CardData(CardEffectID.Mid_NutrientSupply, "영양분 공급", "새싹네모의 공격력 +50%", CardGrade.Mid));
        allCards[CardGrade.Mid].Add(new CardData(CardEffectID.Mid_HighPressureSteam, "고압 증기", "증기네모 스킬 발동 시 주변 적에게도 300% 스플래시 데미지", CardGrade.Mid));
        allCards[CardGrade.Mid].Add(new CardData(CardEffectID.Mid_BladeIce, "칼날 얼음", "얼음네모 스킬에 맞은 적은 3초간 받는 피해 10% 증가", CardGrade.Mid));
        allCards[CardGrade.Mid].Add(new CardData(CardEffectID.Mid_LavaEruption, "열기 분출", "용암네모의 원형 화염 크기 20% 증가, 스킬 사용 확률 10% 증가", CardGrade.Mid));
        allCards[CardGrade.Mid].Add(new CardData(CardEffectID.Mid_QuickSand, "유사", "모래네모의 모래폭풍 내부의 적 이동속도 30% 추가 감소", CardGrade.Mid));
        allCards[CardGrade.Mid].Add(new CardData(CardEffectID.Mid_StaticShock, "정전기", "전기네모 스킬이 튕기는 횟수 +2회", CardGrade.Mid));
        allCards[CardGrade.Mid].Add(new CardData(CardEffectID.Mid_ElementReverse, "원소 역전", "조합 시마다 +5C", CardGrade.Mid));
        allCards[CardGrade.Mid].Add(new CardData(CardEffectID.Mid_FastAttack, "빠른 중급 공격", "중급네모들의 공격속도 +20%", CardGrade.Mid));
        allCards[CardGrade.Mid].Add(new CardData(CardEffectID.Mid_ElementBalance, "원소 평형", "필드에 중급네모 6종류가 모두 존재하면 모든 유닛 공격력 +30%", CardGrade.Mid));
        allCards[CardGrade.Mid].Add(new CardData(CardEffectID.Mid_RecycleAdvanced, "재활용(중급)", "중급 네모의 판매 비용 8배", CardGrade.Mid));
        allCards[CardGrade.Mid].Add(new CardData(CardEffectID.Mid_EmergencySupply, "긴급 수급", "앞으로 카드 선택 시 즉시 원소석 2개 획득", CardGrade.Mid));

        // 상급
        allCards[CardGrade.High].Add(new CardData(CardEffectID.High_SuperTyphoon, "초대형 태풍", "태풍네모의 스킬 데미지 +2000%", CardGrade.High));
        allCards[CardGrade.High].Add(new CardData(CardEffectID.High_MeteorShower, "혜성 낙하", "메테오네모의 메테오 개수 +2발", CardGrade.High));
        allCards[CardGrade.High].Add(new CardData(CardEffectID.High_WorldTreeSprout, "세계수의 싹", "나무네모의 기절 지속시간 +2초", CardGrade.High));
        allCards[CardGrade.High].Add(new CardData(CardEffectID.High_Overload, "과부하", "번개네모가 튕길 때마다 데미지 10%씩 증폭", CardGrade.High));
        allCards[CardGrade.High].Add(new CardData(CardEffectID.High_FrozenLand, "동토의 땅", "눈보라네모의 눈폭풍 범위 내 적이 초당 800%의 데미지를 받음", CardGrade.High));
        allCards[CardGrade.High].Add(new CardData(CardEffectID.High_RockBreak, "암석 파쇄", "바위네모의 디버프 수치 +5%", CardGrade.High));
        allCards[CardGrade.High].Add(new CardData(CardEffectID.High_CriticalChance, "치명적 확률", "모든 상급네모의 스킬 발동 확률 +5%", CardGrade.High));
        allCards[CardGrade.High].Add(new CardData(CardEffectID.High_ElementBalance2, "원소 평형 2", "필드에 상급네모 6종류가 모두 존재하면 모든 유닛 공격력 +50%", CardGrade.High));
        allCards[CardGrade.High].Add(new CardData(CardEffectID.High_BonusReward, "보상 증가", "중간보스 처치 시 획득 원소석 +1, 보스 처치 시 획득 원소석 +2 ", CardGrade.High));
        allCards[CardGrade.High].Add(new CardData(CardEffectID.High_EmergencySupply2, "긴급 수급2", "앞으로 카드 선택 시 즉시 원소석 5개 획득", CardGrade.High));
        allCards[CardGrade.High].Add(new CardData(CardEffectID.High_FateCard, "운명의 카드", "다음 카드 선택 시 새로고침 횟수 4회", CardGrade.High));

        // 서사
        allCards[CardGrade.Epic].Add(new CardData(CardEffectID.Epic_DeadlyToxin, "치명적인 독소", "맹독네모 독장판 위의 적 방어력 20% 감소", CardGrade.Epic));
        allCards[CardGrade.Epic].Add(new CardData(CardEffectID.Epic_IronDefense, "철벽 방어", "강철네모의 강철벽 지속 시간 +3초", CardGrade.Epic));
        allCards[CardGrade.Epic].Add(new CardData(CardEffectID.Epic_SolarSystem, "태양계 형성", "태양네모의 지구 소환 개수 +2", CardGrade.Epic));
        allCards[CardGrade.Epic].Add(new CardData(CardEffectID.Epic_PermanentFrost, "영구 동토", "블리자드네모 스킬에 맞은 적 1초간 빙결", CardGrade.Epic));
        allCards[CardGrade.Epic].Add(new CardData(CardEffectID.Epic_TeslaCoil, "테슬라 코일", "코일네모 버프 범위 내 아군 공격력 +10%", CardGrade.Epic));
        allCards[CardGrade.Epic].Add(new CardData(CardEffectID.Epic_Tsunami, "대해일", "해일네모 스킬의 이동거리 1.5배 증가 (총 기본거리의 2.5배 이동)", CardGrade.Epic));
        allCards[CardGrade.Epic].Add(new CardData(CardEffectID.Epic_CollectiveIntelligence, "집단 지성", "서사급 네모들의 스킬2 카운트를 2배로 적용", CardGrade.Epic));
        allCards[CardGrade.Epic].Add(new CardData(CardEffectID.Epic_Alchemy, "재화의 연금술", "서사/전설급 강화가 15강에 도달하면 모든 서사급 네모의 기본 공격력이 20배로 적용", CardGrade.Epic));
        allCards[CardGrade.Epic].Add(new CardData(CardEffectID.Epic_ElementBalance3, "원소 평형 3", "필드에 서사급네모 6종류가 모두 존재하면 모든 유닛 공격력 +100%", CardGrade.Epic));
        allCards[CardGrade.Epic].Add(new CardData(CardEffectID.Epic_EmergencySupply3, "긴급 수급3", "앞으로 카드 선택 시 즉시 원소석 10개 획득", CardGrade.Epic));
        allCards[CardGrade.Epic].Add(new CardData(CardEffectID.Epic_FateCard2, "운명의 카드2", "다음 카드 선택 시 새로고침 횟수 9회", CardGrade.Epic));

        // 전설
        allCards[CardGrade.Legendary].Add(new CardData(CardEffectID.Legendary_WorldTreeBlessing, "세계수의 가호", "세계수네모의 공속 버프가 맵 전체 아군에게 적용", CardGrade.Legendary));
        allCards[CardGrade.Legendary].Add(new CardData(CardEffectID.Legendary_FinalJudgement, "최후의 심판", "심판네모의 공격 속도 2배", CardGrade.Legendary));
        allCards[CardGrade.Legendary].Add(new CardData(CardEffectID.Legendary_EventHorizon, "이벤트 호라이슨", "블랙홀 네모의 처형 체력 기준선 +10% (총 15% 이하 처형)", CardGrade.Legendary));
        allCards[CardGrade.Legendary].Add(new CardData(CardEffectID.Legendary_AbsoluteZero, "절대 영역", "절대영도네모의 맵 전체 서리 데미지 틱당 +500%", CardGrade.Legendary));
        allCards[CardGrade.Legendary].Add(new CardData(CardEffectID.Legendary_DivinePunishment, "천벌", "뇌전네모의 심판의 벼락 데미지가 적 현재 체력의 3% 추가 피해", CardGrade.Legendary));
        allCards[CardGrade.Legendary].Add(new CardData(CardEffectID.Legendary_GiantsShoulder, "거인의 어깨", "아틀라스네모의 스킬 사용 확률 +10%", CardGrade.Legendary));
        allCards[CardGrade.Legendary].Add(new CardData(CardEffectID.Legendary_FateCard3, "운명의 카드3", "다음 카드 선택 시 새로고침 횟수 15회", CardGrade.Legendary));

        // 신화
        allCards[CardGrade.Myth].Add(new CardData(CardEffectID.Myth_BeginningOfEnd, "종말의 시작", "이후로 종말네모의 처치당 공격력 증가치가 +20으로 상승", CardGrade.Myth));
        allCards[CardGrade.Myth].Add(new CardData(CardEffectID.Myth_AbyssCall, "심연의 부름", "심연네모 오라 범위 내의 적은 1초마다 1% 확률로 즉사 (보스 제외)", CardGrade.Myth));
        allCards[CardGrade.Myth].Add(new CardData(CardEffectID.Myth_SpearOfRagnarok, "라그나로크의 창", "라그나로크네모의 전기벽 지속 시간 동안 적이 받는 피해 50% 증가", CardGrade.Myth));
        allCards[CardGrade.Myth].Add(new CardData(CardEffectID.Myth_InfiniteLoop, "무한의 굴레", "모든 유닛의 스킬 사용 확률 +15%", CardGrade.Myth));
        allCards[CardGrade.Myth].Add(new CardData(CardEffectID.Myth_PrimordialLight, "태초의 빛", "모든 유닛이 적 방어력을 무시함", CardGrade.Myth));
        allCards[CardGrade.Myth].Add(new CardData(CardEffectID.Myth_GoldMine, "황금 광산", "10초마다 원소석 1개 획득", CardGrade.Myth));
        allCards[CardGrade.Myth].Add(new CardData(CardEffectID.Myth_MythRebirth, "신화의 재림", "필드에 신화급 유닛이 등장할 때마다 맵 전체 적 체력 99% 감소 (보스 제외)", CardGrade.Myth));
        allCards[CardGrade.Myth].Add(new CardData(CardEffectID.Myth_AscensionTrigger, "초월", "필드에 같은 종류의 네모 16마리가 존재할 시, 모든 네모를 제거하고 즉시 랜덤 신화급 2마리 소환", CardGrade.Myth));
    }

    public void AssignRandomCardToSlot(CardSlotUI slot)
    {
        // 1. 모든 카드를 다 뽑아서 더 이상 뽑을 게 없는 경우 예외 처리
        int totalAvailableCount = 0;
        foreach (var list in allCards.Values) totalAvailableCount += list.Count;
        if (appliedCards.Count + appearedInSession.Count >= totalAvailableCount)
        {
            Debug.LogWarning("더 이상 뽑을 수 있는 카드가 없습니다! 중복 방지 리스트를 일부 초기화합니다.");
            appearedInSession.Clear(); // 이번 세션 기록이라도 지워서 나오게 함
        }

        CardData selectedData = null;
        int safetyNet = 0; // 무한 루프 방지

        while (selectedData == null && safetyNet < 100)
        {
            safetyNet++;
            CardGrade selectedGrade = GetRandomGrade();
            List<CardData> gradeList = allCards[selectedGrade];

            if (gradeList.Count == 0) continue;

            CardData candidate = gradeList[Random.Range(0, gradeList.Count)];

            // [핵심 조건] 이미 적용된 카드도 아니고, 이번 세션에 등장한 적도 없어야 함
            if (!appliedCards.Contains(candidate.id) && !appearedInSession.Contains(candidate.id))
            {
                selectedData = candidate;
                appearedInSession.Add(candidate.id); // 세션 기록에 추가
            }
        }

        if (selectedData != null)
        {
            slot.Setup(this, selectedData);
        }
    }

    private CardGrade GetRandomGrade()
    {
        float roll = Random.Range(0f, 100f);
        float cumulative = 0f;

        foreach (var pair in gradeProbabilities)
        {
            cumulative += pair.Value;
            if (roll <= cumulative) return pair.Key;
        }
        return CardGrade.Low;
    }

    // 보스 처치 후 InGameManager에서 이 함수를 호출
    public void OpenCardDraw()
    {
        rerollCount = 2;
        if (CardUIManager.instance.HasCard(CardEffectID.High_FateCard) && destinyCard1Used > 0) { //운명의 카드 효과 적용
            rerollCount = 4;
            destinyCard1Used--;
        }
        if (CardUIManager.instance.HasCard(CardEffectID.Epic_FateCard2) && destinyCard2Used > 0) { //운명의 카드 효과 적용
            rerollCount = 9;
            destinyCard2Used--;
        }
        if (CardUIManager.instance.HasCard(CardEffectID.Legendary_FateCard3) && destinyCard3Used > 0) { //운명의 카드 효과 적용
            rerollCount = 15;
            destinyCard3Used--;
        }
        appearedInSession.Clear(); // [중요] 새로운 카드 뽑기 창이 열릴 때 세션 기록 초기화

        cardRerollcount.text = $"새로고침: {rerollCount} / 2";
        cardRerollcount.color = Color.black;
        cardPanel.SetActive(true);
        Time.timeScale = 0f;

        foreach (var slot in cardSlots)
        {
            AssignRandomCardToSlot(slot);
        }
    }


    public void OnCardRerolled(CardSlotUI slot)
    {
        if (rerollCount > 0)
        {
            rerollCount--;
            // 리롤 시 기존 슬롯에 있던 ID는 유지하되, 새로운 카드를 뽑음
            // (이미 AssignRandomCardToSlot에서 appearedInSession에 추가되므로 중복 안됨)
            AssignRandomCardToSlot(slot);

            cardRerollcount.text = $"새로고침: {rerollCount} / 2";
            if (rerollCount <= 0) cardRerollcount.color = Color.gray;
        }
    }

    public void OnCardSelected(CardEffectID selectedEffect)
    {
        appliedCards.Add(selectedEffect);
        if (CardUIManager.instance.HasCard(CardEffectID.Mid_EmergencySupply)) InGameManager.instance.AddElementStone(2); //긴급 수급1 카드 효과
        if (CardUIManager.instance.HasCard(CardEffectID.High_EmergencySupply2)) InGameManager.instance.AddElementStone(5); //긴급 수급2 카드 효과
        if (CardUIManager.instance.HasCard(CardEffectID.Epic_EmergencySupply3)) InGameManager.instance.AddElementStone(10); //긴급 수급3 카드 효과
        RefreshAllUnitStats();
        cardPanel.SetActive(false);
        Time.timeScale = SpeedControl.GetFast();
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

    }

    public void RefreshAllUnitStats()
    {
        // 원본 리스트(activeUnits)가 아니라, .ToList()로 복사본을 만들어 돌립니다.
        // 이렇게 하면 루프 도중에 유닛이 추가/삭제되어도 에러가 나지 않습니다.
        foreach (Unit u in activeUnits.ToList())
        {
            if (u != null)
            {
                u.UpdateStatsFromGlobal();
            }
        }
    }

    public bool CheckElementBalance(UnitGrade grade)
    {
        HashSet<string> uniqueNames = new HashSet<string>();
        activeUnits.RemoveAll(u => u == null);
        foreach (var u in activeUnits)
        {
            // 리스트에서 이미 제거되었거나 파괴 중인 유닛은 제외
            if (u != null && u.data != null && u.data.grade == grade)
            {
                uniqueNames.Add(u.data.unitName);
            }
        }
        return uniqueNames.Count >= 6;
    }

    public bool HasCard(CardEffectID id)
    {
        return appliedCards.Contains(id);
    }
}