using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Unit : MonoBehaviour
{
    public UnitData data; // 위에서 만든 데이터 파일이 여기 꽂힙니다.
    private SpriteRenderer spriteRenderer;

    // 살아있는 작은 태양 리스트
    private readonly List<GameObject> activeSuns = new List<GameObject>();

    // 딜미터기용 통계
    public UnitStatistics stats = new UnitStatistics();

    // 성장/전투 스탯
    public int level = 1;                      // 나중에 로비에서 정해줄 유닛 레벨
    public UnitStats combatStats = new UnitStats(); // 전투용 스탯(공격력, 사거리, 공속 등)

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





    [Header("시각적 효과")]
    public SpriteRenderer auraRenderer;  // 발밑 오라 (등급 색상 표현)
    public GameObject rangeCircle;       // 사거리 표시 원

    [Header("전투 설정")]
    private Transform target;                  // 현재 조준 중인 적
    public GameObject basicProjectilePrefab;   // 기본 발사체 프리팹

    [Header("판매 설정")]
    public GameObject sellButton;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // 게임 시작 시 혹은 소환 시 공격 루틴 시작
        StartCoroutine(AttackRoutine());
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

    #region 코일 스킬 보조

    public IEnumerator ApplySkillChanceBuff(float amount, float duration)
    {
        isCoilBuffActive = true;
        skillChanceBonus = amount; // += 가 아니라 = 로 설정하여 중첩 원천 봉쇄
        Debug.Log($"버프 갱신: 현재 확률 보너스 {skillChanceBonus}");

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
                    if (data.unitName == "Poison") StartCoroutine(PoisonSkillRoutine(skill, effect));
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
                    if (data.unitName == "Coil")
                    {
                        StartCoroutine(CoilBuffRoutine(skill, effect));
                    }
                    else if (data.unitName == "WorldTree")
                    {
                        StartCoroutine(WorldTreeBuffRoutine(skill, effect));
                    }
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
                    else
                    {
                        ExecuteSpawnEntity(skill, effect);
                    }
                    break;
                case SkillEffectType.TsunamiLauncher:
                    TsunamiSkill(skill, effect);
                    break;
                case SkillEffectType.SectorAttack:
                    FireSectorIce(skill, effect);
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
                if (data.unitName == "Meteor")
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

            if (effect.effectPrefab != null)
            {
                GameObject fx = Instantiate(effect.effectPrefab, spawnPos, Quaternion.identity);
                float effectScale = skill.range > 0 ? skill.range : range;

                if (data.unitName == "Water")
                    fx.transform.localScale = new Vector3(effectScale, effectScale * 0.2f, 1f);
                else
                    fx.transform.localScale = new Vector3(effectScale, effectScale, 1f);

                Destroy(fx, effect.duration > 0 ? effect.duration : 1.0f);
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
        GameObject fx = Instantiate(effect.effectPrefab, spawnPos, Quaternion.identity);

        float scale = skill.range > 0 ? skill.range / 2f : range / 2f;
        fx.transform.localScale = new Vector3(scale, scale, 1f);

        float lifeTime = effect.duration > 0 ? effect.duration : 1.0f;
        Destroy(fx, lifeTime);
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

    void ApplySteelDebuff(SkillInfo skill, SkillEffect effect, float calculatedValue)
    {
        float range = GetRange();

        Vector3 checkPos = transform.position;
        float checkRadius = range / 2f;

        if (effect.effectPrefab != null)
        {
            GameObject fx = Instantiate(effect.effectPrefab, checkPos, Quaternion.identity);
            fx.transform.localScale = new Vector3(range, range, 1f);
            Destroy(fx, effect.duration);
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
                m.TakeDamage(9999999f, this);
                Debug.Log($"{m.name} 처형 성공!");
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
            GameObject fx = Instantiate(effect.effectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, effect.duration);
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

    IEnumerator PoisonSkillRoutine(SkillInfo skill, SkillEffect effect)
    {
        if (target == null) yield break;

        int poisonCount = GetUnitCount("Poison");

        float attack = GetAttack();
        float finalDamageMultiplier = effect.value + (poisonCount * 2.0f);
        float finalDamagePerSecond = attack * finalDamageMultiplier;

        Vector3 spawnPos = target.position;

        if (effect.effectPrefab != null)
        {
            GameObject poisonZone = Instantiate(effect.effectPrefab, spawnPos, Quaternion.identity);

            float zoneScale = skill.range > 0 ? skill.range : 3f;
            poisonZone.transform.localScale = new Vector3(zoneScale, zoneScale, 1f);

            float elapsed = 0f;
            float duration = effect.duration > 0 ? effect.duration : 5f;

            while (elapsed < duration)
            {
                Collider2D[] hits =
                    Physics2D.OverlapCircleAll(spawnPos, zoneScale / 2f, LayerMask.GetMask("Enemy"));
                foreach (var hit in hits)
                {
                    Monster m = hit.GetComponent<Monster>();
                    if (m != null)
                    {
                        m.TakeDamage(finalDamagePerSecond, this);
                    }
                }
                yield return new WaitForSeconds(1f);
                elapsed += 1f;
            }

            Destroy(poisonZone);
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
            GameObject fx = Instantiate(effect.effectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, duration);
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
                Debug.Log($"레이저출력");
                // Z값을 -1 정도로 앞으로 당겨서 다른 UI나 배경보다 앞에 오게 합니다.
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
        if (data == null || data.unitName != "Judgement") return;

        judgementKillCounter++;
        if (judgementKillCounter >= 20)
        {
            judgementKillCounter = 0;
            TriggerJudgementBonusMeteor();
        }
    }
    // 20킬마다 추가 메테오 발동
    void TriggerJudgementBonusMeteor()
    {
        if (data == null || data.skills == null) return;

        // UnitData에서 "심판의 메테오" 스킬을 찾아서 그대로 실행
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

        level = 1;          // 지금은 1 고정, 나중에 로비에서 받아오면 됨
        InitStatsFromData();

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
        }
    }

    #endregion
}