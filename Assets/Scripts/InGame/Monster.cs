using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

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
    public GameObject damageTextPrefab; //데미지 프리펩

    [Header("상태")]
    public bool isStunned = false;
    private bool isDead = false;

    private float damageMultiplier = 1f;
    private Coroutine debuffCoroutine;

    private SpriteRenderer spriteRenderer;
    private Coroutine slowCoroutine;
    private Coroutine stunCoroutine;
    private Color baseColor = Color.white;

    private Coroutine bossTeleportCoroutine; // 보스 텔레포트 관리용

    // ㅡㅡㅡ [숲 테마 기믹 변수] ㅡㅡㅡ
    [Header("숲 테마 기믹 (분열)")]
    public GameObject monsterPrefab; // 자기 자신의 프리팹을 인스펙터에서 할당
    private Coroutine regenCoroutine; // 지속 회복 코루틴
    private bool isForestTheme = false; // 현재 테마가 숲인지 저장
    private bool isClone = false; // 이 몬스터가 분열된 녀석인지 체크 (무한 분열 방지)

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = spriteRenderer.color;
    }

    public void Setup(Transform[] path, int currentRound, MonsterType type)
    {
        this.monsterType = type;
        this.isClone = false; // 본체이므로 클론 아님

        // --- DataManager에서 현재 테마와 단계 가져오기 ---
        int currentTheme = 0;
        int currentStage = 1;

        if (DataManager.instance != null && DataManager.instance.currentUser != null)
        {
            currentTheme = DataManager.instance.currentUser.selectedTheme; // 0 ~ 4 (바위산~공허)
            currentStage = DataManager.instance.currentUser.selectedStage; // 1 ~ 5
        }

        // [숲 테마 체크] 인덱스 1번을 숲이라고 가정
        isForestTheme = (currentTheme == 1 || currentTheme == 4);

        // 총 25단계 중 현재 위치를 0 ~ 24의 인덱스로 변환
        int totalStageIndex = (currentTheme * 5) + (currentStage - 1);

        // --- [밸런스 핵심] 성장률(growthRate) 계산 ---
        float minGrowthRate = 1.095f;
        float maxGrowthRate = 1.185f;
        float t = totalStageIndex / 24f;
        float growthRate = Mathf.Lerp(minGrowthRate, maxGrowthRate, t);

        // 1. 지수 함수 기반 체력 계산 (1라운드 100 기준)
        float initialHp = 100f;
        float exponentialHp = initialHp * Mathf.Pow(growthRate, currentRound);

        // 이동 속도 계산
        currentSpeed = baseSpeed + (currentRound * 0.01f);
        if (currentStage == 2 || currentStage == 5) currentSpeed *= 1.3f;

        // 2. 타입별 체력/방어력 승수 적용
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
                if (currentStage == 5)
                {
                    if (bossTeleportCoroutine != null) StopCoroutine(bossTeleportCoroutine);
                    bossTeleportCoroutine = StartCoroutine(BossTeleportRoutine());
                }
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

        // 숲 테마라면 지속 회복 시작
        if (isForestTheme && regenCoroutine == null)
        {
            regenCoroutine = StartCoroutine(ForestRegenRoutine());
        }
    }

    // ㅡㅡㅡ [분열 전용 Setup 함수] ㅡㅡㅡ
    public void SetupSplit(float parentMaxHp, Transform[] path, int index, float speed, float def, MonsterType type)
    {
        this.monsterType = type;
        this.waypoints = path;
        this.currentIndex = index;
        this.currentSpeed = speed;
        this.defense = def;

        this.isForestTheme = true;
        this.isClone = true; // 클론으로 태어남을 명시 (더 이상 분열 안 함)

        // ★ 분열된 녀석은 크기를 조금 작게 만듭니다 
        this.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

        // 본체의 1/3 체력으로 설정
        this.maxhp = Mathf.Max(1f, Mathf.Round(parentMaxHp / 3f));
        this.hp = this.maxhp;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxhp;
            hpSlider.value = hp;
        }

        // 분열된 놈도 회복 시작
        if (regenCoroutine == null)
        {
            regenCoroutine = StartCoroutine(ForestRegenRoutine());
        }
    }

    public void SetupOre(int deathCount)
    {
        this.monsterType = MonsterType.Ore;
        float initialHp = 1000f;
        maxhp = initialHp * Mathf.Pow(1.15f, deathCount);
        maxhp = Mathf.Round(maxhp);
        hp = maxhp;
        defense = deathCount * 2f;
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

        Transform target = waypoints[currentIndex];
        transform.position = Vector3.MoveTowards(transform.position, target.position, currentSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentIndex++;
            if (currentIndex >= waypoints.Length) currentIndex = 0;
        }
    }

    public void TakeDamage(float damage, Unit attacker, bool canCrit = true)
    {
        if (isDead || hp <= 0f) return;

        float finalDamage = damage;
        bool isCriticalHit = false;

        if (attacker != null && canCrit)
        {
            float critChance = attacker.combatStats.Get(StatType.CritChance);
            if (Random.value < critChance)
            {
                finalDamage *= attacker.combatStats.Get(StatType.CritDamage);
                isCriticalHit = true;
            }
        }

        float reductionPercent = defense / (defense + 100f);
        if (CardUIManager.instance.HasCard(CardEffectID.Myth_PrimordialLight)) reductionPercent = 0f;

        finalDamage = finalDamage * (1f - reductionPercent) * damageMultiplier;
        finalDamage = Mathf.Max(1f, Mathf.Round(finalDamage));

        float damageToRecord = Mathf.Min(finalDamage, Mathf.Max(0f, hp));
        if (attacker != null)
        {
            attacker.stats.totalDamage += damageToRecord;
        }
        ShowDamageText(finalDamage, isCriticalHit);
        hp -= finalDamage;

        if (hpSlider != null) hpSlider.value = hp;
        if (hp <= 0)
        {
            if (attacker != null)
            {
                attacker.stats.killCount++;
                attacker.OnMonsterKilled(this);
            }
            Die();
        }
    }

    private void ShowDamageText(float damage, bool isCrit)
    {
        if (damageTextPrefab == null) return;
        Vector3 randomOffset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0, 0.2f), 0);
        Vector3 spawnPos = transform.position + Vector3.up * 0.8f + randomOffset;
        GameObject obj = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
        DamageText dt = obj.GetComponent<DamageText>();
        if (dt != null) dt.Setup(damage, isCrit);
    }

    public void ApplyArmorReduction(float percent, float duration)
    {
        if (armorDebuffCoroutine != null) StopCoroutine(armorDebuffCoroutine);
        armorDebuffCoroutine = StartCoroutine(ArmorReductionRoutine(percent, duration));
    }

    IEnumerator ArmorReductionRoutine(float percent, float duration)
    {
        float originalDefense = defense;
        defense *= (1f - percent);
        yield return new WaitForSeconds(duration);
        defense = originalDefense;
        armorDebuffCoroutine = null;
    }

    IEnumerator BossTeleportRoutine()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(5f);
            if (isStunned || waypoints == null || waypoints.Length == 0) continue;
            int nextIndex = Random.Range(0, waypoints.Length);
            transform.position = waypoints[nextIndex].position;
            currentIndex = nextIndex;
            if (currentIndex >= waypoints.Length) currentIndex = 0;
        }
    }

    // 지속 체력 회복 (1초당 최대 체력의 1%)
    IEnumerator ForestRegenRoutine()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(1f);
            if (hp < maxhp && !isDead)
            {
                float regenAmount = maxhp * 0.01f;
                hp = Mathf.Min(maxhp, hp + regenAmount);
                if (hpSlider != null) hpSlider.value = hp;
            }
        }
    }

    // ㅡㅡㅡ [죽었을 때 분열 로직] ㅡㅡㅡ
    private void ForestSplit()
    {
        // 1. 오직 잡몹(Normal)만 분열되도록 방어코드 추가
        if (monsterType != MonsterType.Normal) return;

        // 2. 이미 한 번 분열된 녀석(클론)이라면 더 이상 분열하지 않음
        if (isClone) return;

        if (monsterPrefab == null)
        {
            Debug.LogWarning("Monster 프리팹이 할당되지 않아 분열할 수 없습니다.");
            return;
        }

        // 딱 2마리만 생성합니다.
        for (int i = 0; i < 2; i++)
        {
            // 약간 위치를 다르게 해서 겹치지 않게
            Vector3 spawnPos = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0);
            GameObject cloneObj = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
            Monster clone = cloneObj.GetComponent<Monster>();

            if (clone != null)
            {
                // 클론 셋업 (스탯과 상태 물려주기)
                clone.SetupSplit(maxhp, waypoints, currentIndex, currentSpeed, defense, monsterType);

                // ★ 매우 중요: 클론이 태어날 때마다 매니저에 알려야 마이너스 버그가 발생하지 않습니다.
                if (InGameManager.instance != null)
                {
                    InGameManager.instance.OnMonsterSpawned();
                }
            }
        }
    }

    public void ApplySlow(float percent, float duration)
    {
        if (slowCoroutine != null) StopCoroutine(slowCoroutine);
        slowCoroutine = StartCoroutine(SlowRoutine(percent, duration));
    }

    IEnumerator SlowRoutine(float percent, float duration)
    {
        if (!isStunned) spriteRenderer.color = new Color(0.5f, 0.5f, 1f, 1f);
        currentSpeed = baseSpeed * (1f - percent);
        yield return new WaitForSeconds(duration);
        currentSpeed = baseSpeed;
        if (!isStunned) spriteRenderer.color = baseColor;
        slowCoroutine = null;
    }

    public void ApplyDamageAmp(float ampValue, float duration)
    {
        if (debuffCoroutine != null) StopCoroutine(debuffCoroutine);
        debuffCoroutine = StartCoroutine(DamageAmpRoutine(ampValue, duration));
    }

    IEnumerator DamageAmpRoutine(float ampValue, float duration)
    {
        damageMultiplier = 1f + ampValue;
        yield return new WaitForSeconds(duration);
        damageMultiplier = 1f;
        debuffCoroutine = null;
    }

    public void ApplyStun(float duration)
    {
        var cm = InGameCrystalManager.Instance;
        if (cm != null && cm.FinalEarthStunTime > 0) duration += cm.FinalEarthStunTime;

        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        Color originColor = spriteRenderer.color;
        spriteRenderer.color = Color.gray;
        yield return new WaitForSeconds(duration);
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
            TakeDamage(dps * 0.5f, attacker);
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void AddVisualEffect(GameObject prefab, float duration)
    {
        if (prefab == null) return;
        GameObject visual = Instantiate(prefab, transform.position, Quaternion.identity, transform);
        visual.transform.localPosition = Vector3.zero;
        Destroy(visual, duration);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (regenCoroutine != null) StopCoroutine(regenCoroutine);
        if (bossTeleportCoroutine != null) StopCoroutine(bossTeleportCoroutine);

        // 죽기 전에 숲 테마 분열 판정 호출
        if (isForestTheme)
        {
            ForestSplit();
        }

        // 재화 지급 로직
        if (InGameManager.instance != null)
        {
            if (monsterType == MonsterType.Ore)
            {
                InGameManager.instance.AddElementStone(1);
            }
            else
            {
                if (monsterType == MonsterType.MiniBoss)
                {
                    int elementStoneReward = CardUIManager.instance.HasCard(CardEffectID.High_BonusReward) ? 2 : 1;
                    InGameManager.instance.AddElementStone(elementStoneReward);
                    InGameManager.instance.AddCoin(InGameManager.instance.currentRound * 5);
                }
                else if (monsterType == MonsterType.Boss)
                {
                    int elementStoneReward = CardUIManager.instance.HasCard(CardEffectID.High_BonusReward) ? 7 : 5;
                    InGameManager.instance.AddElementStone(elementStoneReward);
                    InGameManager.instance.AddCoin(InGameManager.instance.currentRound * 10);
                    InGameManager.instance.BossKilledSettingTime();
                    if (InGameManager.instance.currentRound < 100) CardUIManager.instance.OpenCardDraw();
                }
                else // 본체인 경우만 재화 지급
                {
                    if (!isClone)
                    {
                        int stage = DataManager.instance.currentUser.selectedStage;
                        int rewardCoin = 2; // 기본값

                        // 4, 5 스테이지일 때 40% 확률로 1코인으로 감소
                        if (stage == 4 || stage == 5)
                        {
                            if (Random.value < 0.4f) // 0.0 ~ 1.0 사이의 값 중 0.4 미만일 때 (40%)
                            {
                                rewardCoin = 1;
                            }
                        }

                        InGameManager.instance.AddCoin(rewardCoin);
                    }
                }

                // 매니저에게 본체가 죽었음을 알림
                InGameManager.instance.OnMonsterDestroyed();
            }
        }
        Destroy(gameObject);
    }
}