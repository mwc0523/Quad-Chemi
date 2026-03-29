using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public enum MonsterType { Normal, MiniBoss, Boss, Ore }

public class Monster : MonoBehaviour
{
    public MonsterType monsterType = MonsterType.Normal; //기본값
    private Transform[] waypoints;
    private int currentIndex = 0;

    private Coroutine armorDebuffCoroutine;

    [Header("능력치")]
    public float maxhp = 500f;
    public float hp;
    public float baseSpeed = 1f;
    private float currentSpeed;
    public float defense;

    [Header("UI 연결")]
    public Slider hpSlider;

    [Header("상태")]
    public bool isStunned = false;
    private bool isDead = false;

    private float damageMultiplier = 1f;
    private Coroutine debuffCoroutine;

    private SpriteRenderer spriteRenderer;
    private Coroutine slowCoroutine;
    private Coroutine stunCoroutine;
    private Color baseColor = Color.white;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = spriteRenderer.color;
    }

    public void Setup(Transform[] path, int currentRound, MonsterType type)
    {
        this.monsterType = type;

        // --- [신규 추가] DataManager에서 현재 테마와 단계 가져오기 ---
        int currentTheme = 0;
        int currentStage = 1;

        if (DataManager.instance != null && DataManager.instance.currentUser != null)
        {
            currentTheme = DataManager.instance.currentUser.selectedTheme; // 0 ~ 4 (바위산~공허)
            currentStage = DataManager.instance.currentUser.selectedStage; // 1 ~ 5
        }

        // 총 25단계 중 현재 위치를 0 ~ 24의 인덱스로 변환
        int totalStageIndex = (currentTheme * 5) + (currentStage - 1);

        // --- [밸런스 핵심] 성장률(growthRate) 계산 ---
        // 바위산 1단계 (전투력 1만 타겟)
        float minGrowthRate = 1.095f;

        // 공허 5단계 (전투력 1000만 타겟)
        float maxGrowthRate = 1.185f;

        // 현재 인덱스(0~24)에 맞춰 min과 max 사이의 값을 부드럽게 추출 (0.0f ~ 1.0f 비율)
        float t = totalStageIndex / 24f;
        float growthRate = Mathf.Lerp(minGrowthRate, maxGrowthRate, t);

        // 1. 지수 함수 기반 체력 계산 (1라운드 100 기준)
        float initialHp = 100f;

        // 지수 계산: HP = 100 * (growthRate ^ round)
        float exponentialHp = initialHp * Mathf.Pow(growthRate, currentRound);

        // 이동 속도 계산 (기존 유지)
        currentSpeed = baseSpeed + (currentRound * 0.01f);

        // 2. 타입별 체력/방어력 승수 적용 (이하 기존 코드 동일)
        switch (monsterType)
        {
            case MonsterType.MiniBoss:
                maxhp = exponentialHp * (currentRound / 30f);
                defense = currentRound * 1.5f;
                currentSpeed *= 0.8f;
                break;
            case MonsterType.Boss:
                maxhp = exponentialHp * (currentRound / 10f);
                defense = currentRound * 2f;
                currentSpeed *= 0.6f;
                break;
            default:
                maxhp = exponentialHp;
                defense = currentRound;
                break;
        }

        // 소수점 정리 및 할당
        maxhp = Mathf.Round(maxhp);
        hp = maxhp;
        waypoints = path;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxhp;
            hpSlider.value = hp;
        }
    }


    public void SetupOre(int deathCount)
    {
        this.monsterType = MonsterType.Ore;

        // 원소석 체력 계산 (예: 기본 1000에서 시작, 파괴될 때마다 1.15배씩 증가)
        // 수치는 기획에 맞게 수정하세요!
        float initialHp = 1000f;
        maxhp = initialHp * Mathf.Pow(1.15f, deathCount);
        maxhp = Mathf.Round(maxhp);
        hp = maxhp;

        // 방어력도 조금씩 단단해지게 설정
        defense = deathCount * 2f;

        // ★ 핵심: 움직이지 않게 하기 위해 waypoints를 null로 둡니다.
        waypoints = null;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxhp;
            hpSlider.value = hp;
        }
    }

    void Update()
    {
        if (isDead || isStunned || waypoints == null) return;

        // 이동 로직
        Transform target = waypoints[currentIndex];
        transform.position = Vector3.MoveTowards(transform.position, target.position, currentSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentIndex++;
            if (currentIndex >= waypoints.Length) currentIndex = 0;
        }
    }
    public void TakeDamage(float damage, Unit attacker)
    {
        if (isDead || hp <= 0f) return;

        float reductionPercent = defense / (defense + 100f); // 방어력 계산
        if(CardUIManager.instance.HasCard(CardEffectID.Myth_PrimordialLight)) reductionPercent = 0f; //태초의 빛 카드 효과 적용 (방어력 무시)

        float finalDamage = damage * (1f - reductionPercent) * damageMultiplier; // 실제 받는 데미지 계산
        finalDamage = Mathf.Round(finalDamage);
        finalDamage = Mathf.Max(1f, finalDamage); // 최소 1은 들어가게 함

        // 실제로 이 한 번의 공격으로 빠지는 체력
        float damageToRecord = Mathf.Min(finalDamage, Mathf.Max(0f, hp));
        if (attacker != null)
        {
            attacker.stats.totalDamage += damageToRecord;  // ★ 누적은 항상 0 이상만 더해짐
            //Debug.Log(attacker.data.unitName + "의 공격으로 " + damageToRecord + "의 데미지를 입음");
        }
        hp -= finalDamage;

        if (hpSlider != null) hpSlider.value = hp;
        if (hp <= 0)
        {
            if (attacker != null)
            {
                attacker.stats.killCount++;
                attacker.OnMonsterKilled(this);   // 심판 업보용 20킬 카운트
            }
            Die();
        }
    }
    // 방어력 감소 효과 (예: 0.3f면 30% 감소)
    public void ApplyArmorReduction(float percent, float duration)
    {
        if (armorDebuffCoroutine != null) StopCoroutine(armorDebuffCoroutine);
        armorDebuffCoroutine = StartCoroutine(ArmorReductionRoutine(percent, duration));
    }

    IEnumerator ArmorReductionRoutine(float percent, float duration)
    {
        float originalDefense = defense;
        // 방어력 감소 적용
        defense *= (1f - percent);

        yield return new WaitForSeconds(duration);

        // 복구 (다른 디버프와 겹칠 수 있으므로 원래대로 돌리거나 라운드 수치로 재계산)
        defense = originalDefense;
        armorDebuffCoroutine = null;
    }

    // ㅡㅡㅡ 상태이상 구현부 ㅡㅡㅡ

    // 감속 효과 (예: 0.2f면 20% 느려짐)
    public void ApplySlow(float percent, float duration)
    {
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }
        // 새로 시작하고 변수에 저장합니다.
        slowCoroutine = StartCoroutine(SlowRoutine(percent, duration));
    }
    IEnumerator SlowRoutine(float percent, float duration)
    {
        if(!isStunned)
            spriteRenderer.color = new Color(0.5f, 0.5f, 1f, 1f); //파란색으로

        currentSpeed = baseSpeed * (1f - percent);
        yield return new WaitForSeconds(duration);
        currentSpeed = baseSpeed;
        if (!isStunned)
            spriteRenderer.color = baseColor; //색 되돌리기

        slowCoroutine = null;
    }

    public void ApplyDamageAmp(float ampValue, float duration)
    {
        if (debuffCoroutine != null) StopCoroutine(debuffCoroutine);
        debuffCoroutine = StartCoroutine(DamageAmpRoutine(ampValue, duration));
    }

    IEnumerator DamageAmpRoutine(float ampValue, float duration)
    {
        // 바위네모 스킬이 10% 증가라면 ampValue는 0.1
        damageMultiplier = 1f + ampValue;

        yield return new WaitForSeconds(duration);

        damageMultiplier = 1f; // 원래대로 복구
        debuffCoroutine = null;
    }

    // 기절 효과
    public void ApplyStun(float duration)
    {
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
        }
        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }
    IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        Color originColor = spriteRenderer.color;
        spriteRenderer.color = Color.gray; // 기절 시각 효과

        yield return new WaitForSeconds(duration);

        // 기절 해제 로직
        spriteRenderer.color = originColor;
        isStunned = false;
        stunCoroutine = null;
    }

    public void ApplyDOT(float damagePerSecond, float duration, Unit attacker)
    {
        StartCoroutine(DOTRoutine(damagePerSecond, duration, attacker));
    }

    IEnumerator DOTRoutine(float dps, float duration, Unit attacker)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            TakeDamage(dps * 0.5f, attacker); // 0.5초마다 데미지
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void AddVisualEffect(GameObject prefab, float duration)
    {
        if (prefab == null) return;

        // 몬스터를 부모로 설정하여 생성 (따라다니게 됨)
        GameObject visual = Instantiate(prefab, transform.position, Quaternion.identity, transform);

        // 위치는 몬스터 중앙, 크기는 1:1 (이미지는 몬스터 크기에 맞춰짐)
        visual.transform.localPosition = Vector3.zero;
        //visual.transform.localScale = Vector3.one;

        Destroy(visual, duration);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // 재화 지급 로직
        if (InGameManager.instance != null)
        {
            if (monsterType == MonsterType.Ore)
            {
                InGameManager.instance.AddElementStone(1);
            }
            else {
                if (monsterType == MonsterType.MiniBoss)
                {
                    int elementStoneReward = CardUIManager.instance.HasCard(CardEffectID.High_BonusReward) ? 2 : 1; // 보상 증가 카드 효과 적용
                    InGameManager.instance.AddElementStone(elementStoneReward);
                    // 코인도 보너스로 더 줄 수 있습니다.
                    InGameManager.instance.AddCoin(InGameManager.instance.currentRound * 5);
                }
                else if (monsterType == MonsterType.Boss)
                {
                    int elementStoneReward = CardUIManager.instance.HasCard(CardEffectID.High_BonusReward) ? 7 : 5; // 보상 증가 카드 효과 적용
                    InGameManager.instance.AddElementStone(elementStoneReward); // 보스는 더 많이!
                    InGameManager.instance.AddCoin(InGameManager.instance.currentRound * 10);
                    InGameManager.instance.BossKilledSettingTime(); //라운드 남은 시간 줄이기
                    if(InGameManager.instance.currentRound < 100) CardUIManager.instance.OpenCardDraw(); //마지막 라운드가 아니라면 카드 뽑기
                }
                else
                {
                    InGameManager.instance.AddCoin((InGameManager.instance.currentRound > 50) ? 2 : 1); // 일반 몹
                }

                InGameManager.instance.OnMonsterDestroyed();
            }
        }
        Destroy(gameObject);
    }
}