using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class Unit : MonoBehaviour
{
    public UnitData data; // 위에서 만든 데이터 파일이 여기 꽂힙니다.
    private SpriteRenderer spriteRenderer;

    // 딜미터기용 통계
    public UnitStatistics stats = new UnitStatistics();

    // 성장/전투 스탯
    public int level = 1;                      // 나중에 로비에서 정해줄 유닛 레벨
    public UnitStats combatStats = new UnitStats(); // 전투용 스탯(공격력, 사거리, 공속 등)

    // 살아있는 작은 태양 리스트
    private readonly List<GameObject> activeSuns = new List<GameObject>();
    // 코일 버프 관련
    public float skillChanceBonus = 0f; // 코일의 스킬 사용 확률 증가량
    private bool isCoilBuffActive = false;
    private Coroutine coilBuffCoroutine; //코일 버프 갱신용
    private Coroutine worldTreeBuffCoroutine; //세계수 버프 갱신용
    private StatModifier worldTreeBuffModifier;
    private Coroutine judgementLaserCoroutine; //심판 전용 상태
    private float judgementCurrentMultiplier;
    private int judgementKillCounter = 0;
    public LineRenderer judgementLaserLine;
    private StatModifier atlasAttackModifier; // 현재 적용 중인 아틀라스 버프 저장용
    private Coroutine atlasBuffCoroutine;
    private int attackCount = 0;

    private GameObject currentSteelFX;
    private GameObject currentAreaFX;
    private GameObject currentCoilFX;
    private GameObject currentWorldTreeFX;
    private GameObject currentSelfFX;



    [Header("시각적 효과")]
    public SpriteRenderer auraRenderer;  // 발밑 오라 (등급 색상 표현)
    public GameObject rangeCircle;       // 사거리 표시 원

    [Header("전투 설정")]
    private Transform target;                  // 현재 조준 중인 적
    public GameObject basicProjectilePrefab;   // 기본 발사체 프리팹

    [Header("판매 설정")]
    public GameObject sellButton;
    public TextMeshProUGUI sellPriceText;

    [Header("랜덤 교환 설정")]
    public GameObject changeButton;
    public TextMeshProUGUI changePriceText;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // 게임 시작 시 혹은 소환 시 공격 루틴 시작
        StartCoroutine(AttackRoutine());
        if (data.unitName == "Atlas")
        {
            StartCoroutine(AtlasAuraRoutine());
        }
    }

    #region 전투 스탯 초기화 & 헬퍼

    void InitStatsFromData()
    {
        if (data == null) return;

        // 기본 공격력/사거리/공속은 현재 UnitData 값을 그대로 사용
        combatStats.SetBase(StatType.Attack, data.damage);
        combatStats.SetBase(StatType.AttackSpeed, data.attackSpeed);
        combatStats.SetBase(StatType.Range, data.attackRange);

        // 치명타 관련 기본값 (원하면 나중에 UnitData로 옮길 수 있음)
        combatStats.SetBase(StatType.CritChance, 0f);
        combatStats.SetBase(StatType.CritDamage, 1.5f); // 예: 기본 치명타 150%
    }

    float GetAttack()
    {
        float atk = combatStats.Get(StatType.Attack);
        if (atk <= 0f && data != null) atk = data.damage;
        return atk;
    }

    float GetRange()
    {
        float range = combatStats.Get(StatType.Range);
        if (range <= 0f && data != null) range = data.attackRange;
        return range;
    }

    #endregion

    #region 버프 스킬 보조

    public IEnumerator ApplySkillChanceBuff(float amount, float duration)
    {
        isCoilBuffActive = true;
        skillChanceBonus = amount; // += 가 아니라 = 로 설정하여 중첩 원천 봉쇄
        //Debug.Log($"버프 갱신: 현재 확률 보너스 {skillChanceBonus}");

        yield return new WaitForSeconds(duration);

        // 버프 종료
        skillChanceBonus = 0f;
        isCoilBuffActive = false;
        coilBuffCoroutine = null;
    }

    public void AddCoilBuff(float amount, float duration)
    {
        // 1. 이미 버프 코루틴이 돌고 있다면 강제 종료
        if (coilBuffCoroutine != null)
        {
            StopCoroutine(coilBuffCoroutine);
            coilBuffCoroutine = null;

            // 기존 보너스 초기화
            if (isCoilBuffActive)
            {
                skillChanceBonus = 0f;
            }
        }

        // 2. 새로운 버프 코루틴 시작
        coilBuffCoroutine = StartCoroutine(ApplySkillChanceBuff(amount, duration));
    }

    public void AddAtlasBuff(float amount, float duration)
    {
        // 1. 기존 버프 코루틴 및 모디파이어 제거 (중첩 방지 및 갱신)
        if (atlasBuffCoroutine != null)
        {
            StopCoroutine(atlasBuffCoroutine);
            combatStats.RemoveModifier(atlasAttackModifier);
            atlasAttackModifier = null;
        }

        // 2. 새로운 버프 시작
        atlasBuffCoroutine = StartCoroutine(ApplyAtlasAttackBuff(amount, duration));
    }

    IEnumerator ApplyAtlasAttackBuff(float amount, float duration)
    {
        // 공격력 20% 증가 모디파이어 생성 및 추가
        atlasAttackModifier = new StatModifier(StatType.Attack, amount, StatModifierType.PercentAdd, "AtlasBuff");
        combatStats.AddModifier(atlasAttackModifier);

        yield return new WaitForSeconds(duration);

        // 지속 시간 종료 후 제거
        combatStats.RemoveModifier(atlasAttackModifier);
        atlasAttackModifier = null;
        atlasBuffCoroutine = null;
    }

    #endregion

    #region 공격 루프 & 타겟팅

    IEnumerator AttackRoutine()
    {
        while (true)
        {
            if (data != null)
            {
                if (target == null)
                {
                    target = null;
                }

                FindTarget();

                if (target != null)
                {
                    Shoot();

                    float atkSpeed = combatStats.Get(StatType.AttackSpeed);
                    if (atkSpeed <= 0f) atkSpeed = 0.1f;
                    yield return new WaitForSeconds(1f / atkSpeed);
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    void FindTarget()
    {
        float range = GetRange();
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            range / 2f,
            LayerMask.GetMask("Enemy")
        );

        if (hits.Length > 0)
        {
            float minDistance = Mathf.Infinity;
            Transform nearestEnemy = null;

            foreach (var hit in hits)
            {
                if (hit == null || hit.gameObject == null) continue;
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestEnemy = hit.transform;
                }
            }

            target = nearestEnemy;
        }
        else
        {
            target = null;
        }
    }

    void Shoot()
    {
        if (data == null || target == null) return;

        bool basicAttackReplaced = false;
        bool skillFired = false;
        attackCount++;

        // 1. 모든 유닛 공통: OnAttack / ReplaceBasicAttack 스킬 처리
        foreach (var skill in data.skills)
        {
            if (skill.trigger == SkillTrigger.OnAttack)
            {
                float finalChance = skill.triggerChance + skillChanceBonus;

                if (Random.value < finalChance)
                {
                    ExecuteSkill(skill);
                    skillFired = true;
                }
            }
            else if (skill.trigger == SkillTrigger.ReplaceBasicAttack)
            {
                ExecuteSkill(skill);
                basicAttackReplaced = true;
            }
            else if (skill.trigger == SkillTrigger.OnAttackCount)
            {
                if (attackCount >= skill.triggerCount)
                {
                    ExecuteSkill(skill);
                    attackCount = 0; // 발동 후 초기화
                }
            }
        }

        // 2. 심판: 기본 공격은 항상 레이저로 교체 (OnAttack 스킬은 위에서 이미 처리됨)
        if (data.unitName == "Judgement")
        {
            if (judgementLaserCoroutine == null)
            {
                judgementLaserCoroutine = StartCoroutine(JudgementLaserRoutine());
            }
            return; // 평타 발사 금지
        }

        // 3. 나머지 유닛: 스킬 안 나갔고 기본공격 대체도 없으면 평타
        if (!skillFired && !basicAttackReplaced)
        {
            ExecuteBasicAttack();
        }
    }
    void ExecuteBasicAttack()
    {
        if (data.projectilePrefab == null) return;

        GameObject projObj = Instantiate(data.projectilePrefab, transform.position, Quaternion.identity);
        Projectile proj = projObj.GetComponent<Projectile>();

        float attack = GetAttack();
        if (proj != null) proj.Setup(target, attack, ProjectileType.Normal, this);
    }

    #endregion

    #region 스킬 실행

    void ExecuteSkill(SkillInfo skill)
    {
        foreach (var effect in skill.effects)
        {
            switch (effect.effectType)
            {
                case SkillEffectType.DamageArea:
                    if (data.unitName == "Poison" || data.unitName == "WorldTree") StartCoroutine(PoisonSkillRoutine(skill, effect));
                    else if (data.unitName == "End") StartCoroutine(EndMeteorRoutine(skill, effect));
                    else if (data.unitName == "Abyss") ExecuteAbyssRift(skill, effect); // 스킬 2
                    else StartCoroutine(FireAreaRoutine(skill, effect));
                    break;
                case SkillEffectType.DamageProjectile: 
                    if (data.unitName == "Blizzard")
                    {
                        int blizzardCount = GetUnitCount("Blizzard");
                        float finalDamageMultiplier = effect.value + (blizzardCount * 2.0f);
                        StartCoroutine(FireBlizzardRoutine(skill, effect, finalDamageMultiplier));
                    }
                    else if (data.unitName == "BlackHole") // 블랙홀 스킬3 추가
                    {
                        StartCoroutine(FireBlackSphereRoutine(skill, effect));
                    }
                    else
                    {
                        StartCoroutine(FireProjectileRoutine(skill, effect));
                    }
                    break;
                case SkillEffectType.Stun: // 절대영도 스킬 1 (빙결)
                    if (data.unitName == "AbsoluteZero")
                    {
                        // 빙결(기절)과 방어력 감소를 동시에 처리하는 함수 호출
                        ApplyFreezeAndDebuff(skill, effect);
                    }
                    else
                    {
                        ApplyStun(skill, effect);
                    }
                    break;
                case SkillEffectType.Slow:
                    ApplySlow(skill, effect);
                    break;
                case SkillEffectType.DOT:
                    ApplyDOT(skill, effect);
                    break;
                case SkillEffectType.ChainLightning:
                    ExecuteChainLightning(skill, effect);
                    break;
                case SkillEffectType.BuffAlly:
                    if (data.unitName == "Coil") StartCoroutine(CoilBuffRoutine(skill, effect));
                    else if (data.unitName == "WorldTree") StartCoroutine(WorldTreeBuffRoutine(skill, effect));
                    break;
                case SkillEffectType.DebuffEnemy:
                    if (data.unitName == "Steel")
                    {
                        int steelCount = GetUnitCount("Steel");
                        float finalDamageAmp = effect.value + (steelCount * 0.02f);
                        ApplySteelDebuff(skill, effect, finalDamageAmp);
                    }
                    else
                    {
                        ApplyDebuff(skill, effect);
                    }
                    break;
                case SkillEffectType.Execution:
                    ApplyExecution(skill, effect);
                    break;
                case SkillEffectType.SpawnEntity:
                    if (data.unitName == "BlackHole") // ★ 블랙홀 스킬2 추가
                    {
                        SpawnBlackHole(skill, effect);
                    }
                    else if (data.unitName == "Ragnarok")
                    {
                        SpawnElectricWall(skill, effect, target.position);
                    }
                    else
                    {
                        ExecuteSpawnEntity(skill, effect);
                    }
                    break;
                case SkillEffectType.TsunamiLauncher:
                    TsunamiSkill(skill, effect);
                    break;
                case SkillEffectType.SectorAttack:
                    if (data.unitName == "AbsoluteZero") FireSectorIce(skill, effect);
                    else if (data.unitName == "End") ExecuteEndSlash(skill, effect);
                    break;
            }
        }
    }

    public static int GetUnitCount(string unitName)
    {
        int count = 0;
        Unit[] allUnits = Object.FindObjectsOfType<Unit>();
        foreach (var u in allUnits)
        {
            if (u.data != null && u.data.unitName == unitName) count++;
        }
        return count;
    }

    IEnumerator FireAreaRoutine(SkillInfo skill, SkillEffect effect)
    {
        float attack = GetAttack();
        float range = GetRange();

        if (data.unitName == "Sun")
        {
            if (activeSuns.Count > 0 && activeSuns[0] != null)
            {
                foreach (var sun in activeSuns)
                {
                    if (sun == null) continue;
                    sun.GetComponent<SunOrbit>().RefreshDuration(effect.duration);
                }
                yield break;
            }

            activeSuns.Clear();
            int unitCount = GetUnitCount(data.unitName);

            int totalOrbits = 1 + unitCount;
            float angleStep = 360f / totalOrbits;

            for (int i = 0; i < totalOrbits; i++)
            {
                if (effect.effectPrefab != null)
                {
                    GameObject sun = Instantiate(effect.effectPrefab);
                    activeSuns.Add(sun);

                    SunOrbit orbit = sun.GetComponent<SunOrbit>();
                    orbit.Init(transform, skill.range, attack * effect.value, i * angleStep, effect.duration, this);
                }
            }
            yield break;
        }

        int totalShots = effect.count > 0 ? effect.count : 1;
        float damageRadius;
        if (data.unitName == "Meteor")
            damageRadius = skill.range > 0 ? skill.range / 2f : 0.5f;
        else
            damageRadius = skill.range > 0 ? skill.range / 2f : range / 2f;

        for (int i = 0; i < totalShots; i++)
        {
            if (target == null)
            {
                if (data.unitName == "Meteor" || data.unitName == "Judgement")
                {
                    float searchRange = range / 2f;
                    Collider2D[] potentialTargets =
                        Physics2D.OverlapCircleAll(transform.position, searchRange, LayerMask.GetMask("Enemy"));

                    if (potentialTargets.Length > 0)
                    {
                        Transform closest = null;
                        float minDistance = Mathf.Infinity;
                        foreach (var hit in potentialTargets)
                        {
                            float dist = Vector3.Distance(transform.position, hit.transform.position);
                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                closest = hit.transform;
                            }
                        }
                        target = closest;
                    }
                    else
                    {
                        yield break;
                    }
                }
                else
                {
                    yield break;
                }
            }

            Vector3 spawnPos = skill.range > 0 ? target.position : transform.position;

            if (data.unitName == "Meteor" || data.unitName == "Judgement")
            {
                //Debug.Log("메테오 소환!");
                // 적 머리 기준 오른쪽 위 (X: +2, Y: +5) 에서 시작
                Vector3 startPos = spawnPos + new Vector3(2f, 5f, 0f);
                float dropDuration = 0.25f; // 메테오가 떨어지는 속도 (조절 가능)
                float timer = 0f;

                // 떨어질 메테오 오브젝트 생성 (UnitData의 Projectile Prefab 활용)
                GameObject fallingMeteor = null;
                if (data.projectilePrefab != null)
                {
                    fallingMeteor = Instantiate(data.projectilePrefab, startPos, Quaternion.identity);
                    Projectile proj = fallingMeteor.GetComponent<Projectile>();
                    if (proj != null) Destroy(proj);
                }

                // Lerp를 이용해 목표 지점까지 이동
                while (timer < dropDuration)
                {
                    if (fallingMeteor != null)
                    {
                        fallingMeteor.transform.position = Vector3.Lerp(startPos, spawnPos, timer / dropDuration);
                    }
                    timer += Time.deltaTime;
                    yield return null; // 다음 프레임까지 대기
                }

                // 바닥에 닿았으므로 낙하 오브젝트는 삭제
                if (fallingMeteor != null) Destroy(fallingMeteor);
            }

            if (effect.effectPrefab != null)
            {
                if (currentAreaFX != null) Destroy(currentAreaFX);
                currentAreaFX = Instantiate(effect.effectPrefab, spawnPos, Quaternion.identity);
                Destroy(currentAreaFX, effect.duration > 0 ? effect.duration : 1.0f);
            }

            Collider2D[] damageHits =
                Physics2D.OverlapCircleAll(spawnPos, damageRadius, LayerMask.GetMask("Enemy"));
            foreach (var hit in damageHits)
            {
                Monster m = hit.GetComponent<Monster>();
                if (m != null) m.TakeDamage(attack * effect.value, this);
            }

            if (i < totalShots - 1) yield return new WaitForSeconds(0.3f);
        }
    }

    IEnumerator FireProjectileRoutine(SkillInfo skill, SkillEffect effect)
    {
        float attack = GetAttack();
        float range = GetRange();

        int shotCount = effect.count > 0 ? effect.count : 1;

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(transform.position, range / 2f, LayerMask.GetMask("Enemy"));
        int currentShot = 0;

        foreach (var hit in hits)
        {
            if (currentShot >= shotCount) break;
            if (hit == null || hit.gameObject == null) continue;

            if (effect.effectPrefab != null)
            {
                GameObject projObj = Instantiate(effect.effectPrefab, transform.position, Quaternion.identity);
                Projectile proj = projObj.GetComponent<Projectile>();
                if (proj != null) proj.Setup(hit.transform, attack * effect.value, proj.type, this);
            }
            currentShot++;
            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator FireBlizzardRoutine(SkillInfo skill, SkillEffect effect, float multiplier)
    {
        if (target == null || effect.effectPrefab == null) yield break;

        GameObject projObj = Instantiate(effect.effectPrefab, transform.position, Quaternion.identity);
        Projectile proj = projObj.GetComponent<Projectile>();

        if (proj != null)
        {
            float attack = GetAttack();
            proj.Setup(target, attack * multiplier, ProjectileType.Penetrate, this);
        }

        yield return null;
    }

    void SpawnSkillEffect(SkillInfo skill, SkillEffect effect, Vector3 targetPos)
    {
        if (effect.effectPrefab == null) return;

        float range = GetRange();
        Vector3 spawnPos = skill.range > 0 ? targetPos : transform.position;
        float lifeTime = effect.duration > 0 ? effect.duration : 1.0f;

        // ▼ 본인 중심 스킬인지 확인 (사거리가 0 이하) ▼
        if (skill.range <= 0)
        {
            if (currentSelfFX != null) Destroy(currentSelfFX);
            currentSelfFX = Instantiate(effect.effectPrefab, spawnPos, Quaternion.identity);
            Destroy(currentSelfFX, lifeTime);
        }
        else // 타겟 중심 스킬 (기존과 동일하게 개별 생성)
        {
            GameObject fx = Instantiate(effect.effectPrefab, spawnPos, Quaternion.identity);
            Destroy(fx, lifeTime);
        }
    }

    void ApplyStun(SkillInfo skill, SkillEffect effect)
    {
        float range = GetRange();
        float checkRange = skill.range > 0 ? skill.range / 2f : range / 2f;
        Vector3 checkPos = skill.range > 0 ? target.position : transform.position;

        SpawnSkillEffect(skill, effect, checkPos);

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(checkPos, checkRange, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null) m.ApplyStun(effect.duration);
        }
    }

    void ApplySlow(SkillInfo skill, SkillEffect effect)
    {
        float range = GetRange();
        float checkRange = skill.range > 0 ? skill.range / 2f : range / 2f;
        Vector3 checkPos = skill.range > 0 ? target.position : transform.position;

        SpawnSkillEffect(skill, effect, checkPos);

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(checkPos, checkRange, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null) m.ApplySlow(effect.value, effect.duration);
        }
    }

    void ApplyDOT(SkillInfo skill, SkillEffect effect)
    {
        float attack = GetAttack();
        float range = GetRange();

        float checkRange = skill.range > 0 ? skill.range / 2f : range / 2f;
        Vector3 checkPos = skill.range > 0 ? target.position : transform.position;

        SpawnSkillEffect(skill, effect, checkPos);

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(checkPos, checkRange, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null) m.ApplyDOT(attack * effect.value, effect.duration, this);
        }
    }

    void ExecuteChainLightning(SkillInfo skill, SkillEffect effect)
    {
        if (target != null && effect.effectPrefab != null)
        {
            GameObject chainObj = Instantiate(effect.effectPrefab, transform.position, Quaternion.identity);
            ChainLightning chain = chainObj.GetComponent<ChainLightning>();
            if (chain != null)
            {
                float attack = GetAttack();
                chain.Setup(target, attack * effect.value, effect.count, 3f, this);
            }
        }
    }

    void ApplyDebuff(SkillInfo skill, SkillEffect effect)
    {
        float range = GetRange();

        Vector3 checkPos;
        float checkRadius;

        if (skill.range <= 0)
        {
            checkPos = transform.position;
            checkRadius = range / 2f;
        }
        else
        {
            if (target == null) return;
            checkPos = target.position;
            checkRadius = skill.range / 2f;
        }

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(checkPos, checkRadius, LayerMask.GetMask("Enemy"));

        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null)
            {
                m.ApplyDamageAmp(effect.value, effect.duration);

                if (effect.effectPrefab != null)
                {
                    m.AddVisualEffect(effect.effectPrefab, effect.duration);
                }
            }
        }
    }

    //강철의 디버프
    void ApplySteelDebuff(SkillInfo skill, SkillEffect effect, float calculatedValue)
    {
        float range = GetRange();

        Vector3 checkPos = transform.position;
        float checkRadius = range / 2f;

        if (effect.effectPrefab != null)
        {
            if (currentSteelFX != null) Destroy(currentSteelFX);
            currentSteelFX = Instantiate(effect.effectPrefab, checkPos, Quaternion.identity);
            Destroy(currentSteelFX, effect.duration);
        }

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(checkPos, checkRadius, LayerMask.GetMask("Enemy"));

        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null)
            {
                m.ApplyStun(effect.duration);
                m.ApplyDamageAmp(calculatedValue, effect.duration);
            }
        }
    }

    //처형
    void ApplyExecution(SkillInfo skill, SkillEffect effect)
    {
        float range = GetRange();
        float checkRange = skill.range > 0 ? skill.range / 2f : range / 2f;
        Vector3 checkPos = skill.range > 0 ? target.position : transform.position;

        SpawnSkillEffect(skill, effect, checkPos);

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(checkPos, checkRange, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null && (m.hp / m.maxhp) <= effect.value)
            {
                m.TakeDamage(9999999999f, this);
            }
        }
    }

    void ExecuteSpawnEntity(SkillInfo skill, SkillEffect effect)
    {
        if (effect.effectPrefab == null) return;

        Vector3 spawnPos = skill.range > 0 && target != null ? target.position : transform.position;
        Instantiate(effect.effectPrefab, spawnPos, Quaternion.identity);
    }

    IEnumerator CoilBuffRoutine(SkillInfo skill, SkillEffect effect)
    {
        int coilCount = GetUnitCount("Coil");
        float finalBuffValue = effect.value + (coilCount * 0.05f);

        float range = GetRange();
        float buffRange = skill.range > 0 ? skill.range / 2f : range / 2f;
        Collider2D[] allies =
            Physics2D.OverlapCircleAll(transform.position, buffRange, LayerMask.GetMask("Unit"));

        foreach (var ally in allies)
        {
            Unit unit = ally.GetComponent<Unit>();
            if (unit != null)
            {
                if (unit.data != null && unit.data.unitName == "Coil") continue;
                unit.AddCoilBuff(finalBuffValue, effect.duration);
            }
        }

        if (effect.effectPrefab != null)
        {
            if (currentCoilFX != null) Destroy(currentCoilFX);
            currentCoilFX = Instantiate(effect.effectPrefab, transform.position, Quaternion.identity);
            Destroy(currentCoilFX, effect.duration);
        }
        yield return null;
    }

    void TsunamiSkill(SkillInfo skill, SkillEffect effect)
    {
        int tsunamiCount = GetUnitCount("Tsunami");
        float attack = GetAttack();
        float finalTsunamiDamage = attack * (effect.value + (tsunamiCount * 1.0f));

        if (effect.effectPrefab != null)
        {
            GameObject tsunamiObj = Instantiate(effect.effectPrefab, transform.position, Quaternion.identity);
            TsunamiEntity tsunami = tsunamiObj.GetComponent<TsunamiEntity>();

            if (tsunami != null)
            {
                tsunami.Setup(finalTsunamiDamage, effect.duration > 0 ? effect.duration : 3f, this);
            }
        }
    }

    // 맹독네모의 투사체 이펙트
    IEnumerator PoisonSkillRoutine(SkillInfo skill, SkillEffect effect)
    {
        if (target == null) yield break;

        // 1. 시작 및 목표 지점 고정 (적이 이동해도 던진 위치로 날아가게 함)
        Vector3 startPos = transform.position;
        Vector3 targetPos = target.position;

        // 2. 투사체(독병) 생성
        GameObject bottle = null;
        if (data.projectilePrefab != null)
        {
            bottle = Instantiate(data.projectilePrefab, startPos, Quaternion.identity);

            // 핵심: Projectile 스크립트를 파괴해서 직선으로 날아가는 기본 로직을 차단
            Projectile proj = bottle.GetComponent<Projectile>();
            if (proj != null) Destroy(proj);
        }

        // 포물선 이동을 위한 변수
        float duration = 0.5f; // 날아가는 체공 시간
        float time = 0f;
        float arcHeight = 2.0f; // 포물선의 높이

        // 3. 포물선 이동 연출
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            if (bottle != null)
            {
                // Vector3.Lerp로 직선 이동을 구한 뒤, Mathf.Sin으로 Y축(높이)만 더해줌 = 포물선
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
                currentPos.y += arcHeight * Mathf.Sin(t * Mathf.PI);

                bottle.transform.position = currentPos;

                // 독병이 빙글빙글 돌면서 날아가는 연출 (원치 않으시면 지워주세요)
                bottle.transform.Rotate(0, 0, 360 * Time.deltaTime);
            }
            yield return null;
        }

        // 목표 지점 도착 시 맹독병 파괴
        if (bottle != null) Destroy(bottle);

        // 4. 목표 지점에 장판(스프라이트) 생성
        if (effect.effectPrefab != null)
        {
            GameObject areaObj = Instantiate(effect.effectPrefab, targetPos, Quaternion.identity);

            // [에러 해결] GetOrAddComponent 대신 표준 유니티 방식 사용
            ContinuousRange rangeEffect = areaObj.GetComponent<ContinuousRange>();
            if (rangeEffect == null) rangeEffect = areaObj.AddComponent<ContinuousRange>();

            float damagePerSec = GetAttack() * effect.value;
            float radius = skill.range > 0 ? skill.range / 2f : GetRange() / 2f;

            // [에러 해결] 이름을 puddleDuration으로 변경하여 중복 회피
            float puddleDuration = effect.duration > 0 ? effect.duration : 5f;
            float tick = 1f;

            // 초기화 함수 호출
            rangeEffect.Initialize(damagePerSec, radius, puddleDuration, tick, this);
        }
    }

    // 세계수 버프 1개만 관리하는 함수
    public void ApplyWorldTreeBuff(float amount, float duration)
    {
        // 1) 이미 버프가 걸려 있다면 : 코루틴만 멈추고, 버프 수치는 그대로 두고, 타이머만 새로 시작
        if (worldTreeBuffCoroutine != null)
        {
            StopCoroutine(worldTreeBuffCoroutine);
            worldTreeBuffCoroutine = null;
        }

        // 2) 아직 버프가 한 번도 안 걸린 유닛이라면 : StatModifier를 새로 만들어서 한 장만 붙여둠
        if (worldTreeBuffModifier == null)
        {
            worldTreeBuffModifier = new StatModifier(
                StatType.AttackSpeed,
                amount,                        // 예: 0.5 => +50%
                StatModifierType.PercentAdd,   // % 가산 버프
                source: "WorldTreeBuff"
            );
            combatStats.AddModifier(worldTreeBuffModifier);
        }

        // 3) 새 지속시간으로 타이머 시작
        worldTreeBuffCoroutine = StartCoroutine(WorldTreeBuffTimer(duration));
    }

    // 내부용 타이머 코루틴
    IEnumerator WorldTreeBuffTimer(float duration)
    {
        float dur = duration > 0f ? duration : 4f;
        yield return new WaitForSeconds(dur);

        // 시간이 끝나면 버프 1장을 떼고 상태 초기화
        if (worldTreeBuffModifier != null)
        {
            combatStats.RemoveModifier(worldTreeBuffModifier);
            worldTreeBuffModifier = null;
        }
        worldTreeBuffCoroutine = null;
    }
    // 세계수 스킬1: 주변 아군 공격속도 버프
    IEnumerator WorldTreeBuffRoutine(SkillInfo skill, SkillEffect effect)
    {
        // 1. 버프 범위 계산 (skill.range가 0이면 자신의 공격범위 사용)
        float range = GetRange();
        float buffRange = skill.range > 0 ? skill.range / 2f : range / 2f;

        // 2. 범위 내 아군 유닛 찾기
        Collider2D[] allies = Physics2D.OverlapCircleAll(
            transform.position,
            buffRange,
            LayerMask.GetMask("Unit")
        );

        float amount = effect.value;                    // 예: 0.5f
        float duration = effect.duration > 0 ? effect.duration : 4f;

        foreach (var allyCol in allies)
        {
            Unit ally = allyCol.GetComponent<Unit>();
            if (ally == null) continue;

            // (1) 세계수끼리는 버프 금지 (본인 포함)
            if (ally.data != null && ally.data.unitName == "WorldTree")
                continue;

            // (2) 각 유닛이 자기 버프를 알아서 관리 (중첩 X, 시간만 갱신)
            ally.ApplyWorldTreeBuff(amount, duration);
        }

        // 선택: 시각 효과 (세계수 본인 발밑에만 한번)
        if (effect.effectPrefab != null)
        {
            if (currentWorldTreeFX != null) Destroy(currentWorldTreeFX);
            currentWorldTreeFX = Instantiate(effect.effectPrefab, transform.position, Quaternion.identity);
            Destroy(currentWorldTreeFX, duration);
        }

        yield return null;
    }

    // 심판 레이저 기본 공격
    IEnumerator JudgementLaserRoutine()
    {
        judgementCurrentMultiplier = 5f; // 500%부터 시작

        // ★ 선 활성화
        if (judgementLaserLine != null) {
            judgementLaserLine.enabled = true;
            judgementLaserLine.useWorldSpace = true;
            judgementLaserLine.positionCount = 2;
        }
        while (true)
        {
            if (target == null || data == null)
                break;

            float range = GetRange();
            if (Vector3.Distance(transform.position, target.position) > range)
                break;

            Monster m = target.GetComponent<Monster>();
            if (m == null)
                break;

            float attack = GetAttack();

            if (judgementLaserLine != null)
            {
                Vector3 startPos = transform.position;
                startPos.z = -1f;

                Vector3 endPos = target.position;
                endPos.z = -1f;

                judgementLaserLine.SetPosition(0, startPos);
                judgementLaserLine.SetPosition(1, endPos);
            }

            // 1/공격속도마다 데미지
            m.TakeDamage(attack * judgementCurrentMultiplier, this);
            judgementCurrentMultiplier += 5f;

            float atkSpeed = combatStats.Get(StatType.AttackSpeed);
            if (atkSpeed <= 0f) atkSpeed = 0.1f;
            yield return new WaitForSeconds(1f / atkSpeed);
        }

        // 끊기면 초기화
        if (judgementLaserLine != null)
            judgementLaserLine.enabled = false;

        judgementLaserCoroutine = null;
        judgementCurrentMultiplier = 5f;
    }
    // 몬스터가 이 유닛에게 직접 처치됐을 때 호출
    public void OnMonsterKilled(Monster victim)
    {
        if (data == null || (data.unitName != "Judgement" && data.unitName != "Thunderbolt" && data.unitName != "End")) return;
        if (data.unitName == "Judgement") {
            judgementKillCounter++;
            if (judgementKillCounter >= 20)
            {
                judgementKillCounter = 0;
                SkillInfo meteorSkill = default;
                bool found = false;

                foreach (var skill in data.skills)
                {
                    if (skill.skillName == "심판의 메테오")
                    {
                        meteorSkill = skill;
                        found = true;
                        break;
                    }
                }

                if (!found) return;

                // 현재 타겟이 없으면 굳이 발동하지 않음 (원하면 자기 위치 기준으로 바꿀 수 있음)
                if (target == null) return;

                ExecuteSkill(meteorSkill);
            }
        }
        else if (data.unitName == "Thunderbolt") {
            SkillInfo explodeSkill = default;
            bool found = false;

            foreach (var skill in data.skills)
            {
                // 인스펙터에 설정하신 스킬 이름과 일치해야 합니다.
                if (skill.skillName == "뇌력폭파")
                {
                    explodeSkill = skill;
                    found = true;
                    break;
                }
            }

            if (!found) return;

            // 2. 죽은 몬스터의 위치에서 즉시 폭발 로직 실행
            // 'victim' 객체가 파괴(Destroy)되기 직전이므로 위치 값을 가져올 수 있습니다.
            ExecuteExplosion(explodeSkill, victim.transform.position);
        }
        else if (data.unitName == "End") // 종말의 경우
        {
            StatModifier harvestMod = new StatModifier(StatType.Attack, 5f, StatModifierType.Flat, this);
            combatStats.AddModifier(harvestMod);
        }
    }
    
    // 블랙홀네모 스킬2: 블랙홀 소환 함수
    void SpawnBlackHole(SkillInfo skill, SkillEffect effect)
    {
        if (effect.effectPrefab == null || target == null) return;

        // 타겟 위치에 블랙홀 생성
        Vector3 spawnPos = target.position;
        GameObject bhObj = Instantiate(effect.effectPrefab, spawnPos, Quaternion.identity);

        BlackHoleEntity bh = bhObj.GetComponent<BlackHoleEntity>();
        if (bh != null)
        {
            float attack = GetAttack();
            // 데미지, 유지시간, 당기는 반경(range/2), 주인 유닛
            bh.Setup(attack * effect.value, effect.duration, skill.range / 2f, this);
        }
    }

    // ★ 블랙홀네모 스킬3: 검은 구체 발사 함수
    IEnumerator FireBlackSphereRoutine(SkillInfo skill, SkillEffect effect)
    {
        if (target == null || effect.effectPrefab == null) yield break;

        GameObject projObj = Instantiate(effect.effectPrefab, transform.position, Quaternion.identity);
        BlackSphereProjectile proj = projObj.GetComponent<BlackSphereProjectile>();

        if (proj != null)
        {
            float attack = GetAttack();
            // 첫타 2000%(value), 폭발 500%(고정), 폭발반경(range/2)
            proj.Setup(target, attack * effect.value, attack * 5f, skill.range / 2f, this);
        }

        yield return null;
    }

    // 절대영도 스킬1: 영구 동토 (Permafrost)
    void ApplyFreezeAndDebuff(SkillInfo skill, SkillEffect effect)
    {
        // 1칸 타일 범위 (유니티 단위 약 1.2f~1.5f)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f, LayerMask.GetMask("Enemy"));

        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null)
            {
                // 1. 빙결 (기존 기절 함수 활용)
                m.ApplyStun(2f);

                // 2. 방어력 30% 감소 (위에서 만든 함수 호출)
                // effect.value가 0.3으로 셋팅되어 있다고 가정
                m.ApplyArmorReduction(effect.value, 2f);
                SpawnSkillEffect(skill, effect, m.transform.position);
            }
        }
    }

    //절대영도네모 부채꼴 공격
    void FireSectorIce(SkillInfo skill, SkillEffect effect)
    {
        float sectorAngle = 60f;
        float sectorRange = skill.range;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, sectorRange, LayerMask.GetMask("Enemy"));

        // 절대영도가 바라보는 방향 결정
        Vector3 lookDir = target != null ? (target.position - transform.position).normalized : transform.right;

        // --- 이펙트 생성 로직 추가/수정 ---
        if (effect.effectPrefab != null)
        {
            // 1. 이펙트 위치: 유닛 위치에서 적 방향으로 0.5~1.0 unit 정도 앞으로 밀어줌 (Offset)
            Vector3 spawnPos = transform.position + (lookDir * 0.8f);

            // 2. 이펙트 회전: lookDir 방향을 바라보도록 쿼터니언 계산
            float angleDeg = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
            Quaternion spawnRot = Quaternion.Euler(0, 0, angleDeg-90f);

            // 3. 생성
            GameObject eff = Instantiate(effect.effectPrefab, spawnPos, spawnRot);
            Destroy(eff, 1.0f); // 수동 생성 시 파괴 예약 필수
        }

        // 데미지 판정 로직 (기존과 동일)
        foreach (var hit in hits)
        {
            Vector3 dirToEnemy = (hit.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(lookDir, dirToEnemy);

            if (angle <= sectorAngle * 0.5f)
            {
                Monster m = hit.GetComponent<Monster>();
                if (m != null)
                {
                    float damage = combatStats.Get(StatType.Attack) * effect.value;
                    m.TakeDamage(damage, this);
                }
            }
        }
    }
    //뇌전의 폭발
    void ExecuteExplosion(SkillInfo skill, Vector3 explosionPos)
    {
        // 스킬 데이터의 첫 번째 효과(폭발 데미지 500%)를 가져옵니다.
        if (skill.effects == null || skill.effects.Count == 0) return;
        SkillEffect effect = skill.effects[0];

        // 0.5칸 범위 내 적들을 감지 (유니티 단위에 따라 0.5f ~ 0.7f 조절)
        float explosionRange = 0.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(explosionPos, explosionRange, LayerMask.GetMask("Enemy"));

        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null)
            {
                // 500% 데미지 계산 (Value에 5가 들어있다고 가정)
                float damage = combatStats.Get(StatType.Attack) * effect.value;
                m.TakeDamage(damage, this);
            }
        }

        // 폭발 이펙트 생성 (폭발 위치에)
        if (effect.effectPrefab != null)
        {
            GameObject eff = Instantiate(effect.effectPrefab, explosionPos, Quaternion.identity);
            //float effectScale = explosionRange * 2.0f;
            //eff.transform.localScale = new Vector3(effectScale, effectScale, 1.0f);
            Destroy(eff, 1.0f);
        }
    }
    //아틀라스 버프
    IEnumerator AtlasAuraRoutine()
    {
        // 유닛이 파괴될 때까지 무한 반복
        while (true)
        {
            // 1초마다 주변 아군을 탐색하여 버프를 줍니다.
            float auraRange = 1f; // 주변 1칸
            float buffDuration = 1.5f; // 다음 탐색(1초)보다 길게 설정해서 버프가 끊기지 않게 함

            Collider2D[] allies = Physics2D.OverlapCircleAll(transform.position, auraRange, LayerMask.GetMask("Unit"));

            foreach (var ally in allies)
            {
                Unit unit = ally.GetComponent<Unit>();
                if (unit != null && unit != this)
                {
                    if(unit.data.unitName != "Atlas")
                        unit.AddAtlasBuff(0.2f, buffDuration);
                }
            }

            // 매 프레임 체크하면 무거우므로 1초 간격으로 체크
            yield return new WaitForSeconds(1.0f);
        }
    }
    //
    IEnumerator EndMeteorRoutine(SkillInfo skill, SkillEffect effect)
    {
        MapManager map = FindObjectOfType<MapManager>();
        if (map == null || map.allTilePositions.Count == 0) yield break;

        // 1. 전체 타일 리스트를 복사한 뒤 셔플(섞기)해서 상위 5개를 뽑습니다.
        List<Vector2> targets = new List<Vector2>(map.allTilePositions);
        for (int i = 0; i < targets.Count; i++)
        {
            int rand = Random.Range(i, targets.Count);
            Vector2 temp = targets[i];
            targets[i] = targets[rand];
            targets[rand] = temp;
        }

        // 2. 상위 5개 타일에 운석 투하
        int count = Mathf.Min(5, targets.Count);
        for (int i = 0; i < count; i++)
        {
            Vector2 dropPos = targets[i];
            // 개별 운석 낙하 처리 함수 호출
            StartCoroutine(DropIndividualMeteor(dropPos, skill, effect));
            // 약간의 시차를 두고 떨어지면 더 멋있습니다.
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator DropIndividualMeteor(Vector2 pos, SkillInfo skill, SkillEffect effect)
    {
        // 이펙트 생성 (하늘에서 떨어지는 연출은 프리팹 자체 애니메이션 추천)
        if (effect.effectPrefab != null)
        {
            GameObject m = Instantiate(effect.effectPrefab, pos, Quaternion.identity);
            Destroy(m, 2f);
        }

        // 실제 데미지 판정 (약간의 딜레이 후 땅에 닿았을 때)
        yield return new WaitForSeconds(0.5f);

        // 폭발 범위 내 적 감지 (운석이니까 1~1.5칸 정도 범위 추천)
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 1.2f, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null)
            {
                float damage = combatStats.Get(StatType.Attack) * effect.value; // 4000%
                m.TakeDamage(damage, this);
            }
        }
    }
    void ExecuteEndSlash(SkillInfo skill, SkillEffect effect)
    {
        //Debug.Log("휘두르기!!!");
        // 1. 데미지 판정 (공격범위 크기의 원형)
        float slashRange = skill.range; // 종말의 사거리인 4칸
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, slashRange, LayerMask.GetMask("Enemy"));

        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null)
            {
                m.TakeDamage(combatStats.Get(StatType.Attack) * effect.value, this);
            }
        }

        if (effect.effectPrefab != null)
        {
            GameObject slash = Instantiate(effect.effectPrefab, transform.position, Quaternion.identity);
            Destroy(slash, 1.5f);
        }
    }
    //심연 상시스킬
    IEnumerator AbyssAuraRoutine()
    {
        while (true)
        {
            float auraRange = 5.0f;
            Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, auraRange, LayerMask.GetMask("Enemy"));

            foreach (var enemy in enemies)
            {
                Monster m = enemy.GetComponent<Monster>();
                if (m != null)
                {
                    m.ApplySlow(0.7f, 1.2f);
                    if (m.hp > 0 && m.hp <= m.maxhp * 0.1f)
                    {
                        m.TakeDamage(9999999999f, this);
                    }
                }
            }
            yield return new WaitForSeconds(0.01f);
        }
    }
    void ExecuteAbyssRift(SkillInfo skill, SkillEffect effect)
    {
        // 1. 타겟 주변 1칸 범위 적 감지
        if (target == null) return;

        float riftRange = 1.0f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(target.position, riftRange, LayerMask.GetMask("Enemy"));

        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null)
            {
                float percentDamage = m.maxhp * 0.05f;
                m.TakeDamage(percentDamage, this);
            }
        }
        if (effect.effectPrefab != null)
        {
            GameObject rift = Instantiate(effect.effectPrefab, target.position, Quaternion.identity);
            Destroy(rift, 1.5f);
        }
    }
    void SpawnElectricWall(SkillInfo skill, SkillEffect effect, Vector3 spawnPos)
    {
        // 1. 거대한 번개 창 낙하 데미지 (5000%)
        float damageRange = 1.5f; // 창이 떨어질 때 주변 데미지 범위
        Collider2D[] hits = Physics2D.OverlapCircleAll(spawnPos, damageRange, LayerMask.GetMask("Enemy"));

        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null)
            {
                float damage = combatStats.Get(StatType.Attack) * effect.value; // 5000%
                m.TakeDamage(damage, this);
            }
        }

        // 2. 전기벽 생성 (10초간 유지)
        if (effect.effectPrefab != null)
        {
            GameObject wallObj = Instantiate(effect.effectPrefab, spawnPos, Quaternion.identity);
            ElectricWall wallScript = wallObj.GetComponent<ElectricWall>();

            if (wallScript == null) wallScript = wallObj.AddComponent<ElectricWall>();

            // 스킬 정보에 설정된 지속시간(10)과 범위(1)를 전달
            wallScript.Init(effect.duration, skill.range > 0 ? skill.range : 1.0f);
        }
    }

    







    #endregion

    #region 판매 & UI

    public void SellUnit()
    {
        int sellPrice = GetSellPrice();
        if (InGameManager.instance != null)
        {
            InGameManager.instance.AddCoin(sellPrice);
        }
        Destroy(gameObject);
    }

    public void ChangeUnit() {
        if(InGameManager.instance.currentElementStone >= 1) {
            InGameManager.instance.AddElementStone(-1);
            Transform parentTile = this.transform.parent;

            if (parentTile != null)
            {
                transform.SetParent(null);
                InGameManager.instance.SpawnRandomUnit(parentTile, 1);
                Destroy(gameObject);
            }
        }
    }

    int GetSellPrice()
    {
        switch (data.grade)
        {
            case UnitGrade.Low: return 5;
            case UnitGrade.Middle: return 10;
            case UnitGrade.High: return 20;
            case UnitGrade.Epic: return 40;
            case UnitGrade.Legend: return 80;
            case UnitGrade.Myth: return 160;
            default: return 0;
        }
    }

    public void SetUnit(UnitData newData)
    {
        data = newData;

        var saveData = DataManager.instance.currentUser.unitList.Find(u => u.unitID == data.unitName);
        level = (saveData != null) ? saveData.level : 1;

        InitStatsFromData();
        if (level > 1)
        {
            float mult = saveData.GetDamageMultiplier();
            combatStats.AddModifier(new StatModifier(
                StatType.Attack,
                mult - 1f,
                StatModifierType.PercentMul,
                "LobbyLevelBonus"
            ));
        }

        //강화버프 적용
        if (data.grade == UnitGrade.Low || data.grade == UnitGrade.Middle || data.grade == UnitGrade.High) combatStats.AddModifier(UpgradeManager.instance.tier1Modifier);
        else if (data.grade == UnitGrade.Epic || data.grade == UnitGrade.Legend) combatStats.AddModifier(UpgradeManager.instance.tier2Modifier);
        else if (data.grade == UnitGrade.Myth) combatStats.AddModifier(UpgradeManager.instance.tier3Modifier);

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = data.unitSprite;

        SetGradeVisual();

        if (rangeCircle != null) rangeCircle.SetActive(false);
    }

    void SetGradeVisual()
    {
        if (auraRenderer == null) return;
        switch (data.grade)
        {
            case UnitGrade.Low: auraRenderer.color = Color.white; break;
            case UnitGrade.Middle: auraRenderer.color = new Color(0.5f, 1f, 0.5f); break;
            case UnitGrade.High: auraRenderer.color = Color.blue; break;
            case UnitGrade.Epic: auraRenderer.color = new Color(0.6f, 0f, 1f); break;
            case UnitGrade.Legend: auraRenderer.color = Color.yellow; break;
            case UnitGrade.Myth: auraRenderer.color = Color.red; break;
        }
        auraRenderer.color = new Color(auraRenderer.color.r, auraRenderer.color.g, auraRenderer.color.b, 0.5f);
    }

    public void ShowRange(bool x)
    {
        if (data == null) return;

        if (rangeCircle != null)
        {
            rangeCircle.SetActive(x);
            if (x)
            {
                float range = GetRange();
                float scale = range * 2f;
                rangeCircle.transform.localScale = new Vector3(scale, scale, 1);
            }
        }

        if (sellButton != null)
        {
            sellButton.SetActive(x);
            if(x) {
                switch (data.grade)
                {
                    case UnitGrade.Low: sellPriceText.text = "+5C"; break;
                    case UnitGrade.Middle: sellPriceText.text = "+10C"; break;
                    case UnitGrade.High: sellPriceText.text = "+20C"; break;
                    case UnitGrade.Epic: sellPriceText.text = "+40C"; break;
                    case UnitGrade.Legend: sellPriceText.text = "+80C"; break;
                    case UnitGrade.Myth: sellPriceText.text = "+160C"; break;
                }
            }
        }

        if (changeButton != null)
        {
            if(data.grade == UnitGrade.Low)
                changeButton.SetActive(x);
        }
    }
    #endregion
}