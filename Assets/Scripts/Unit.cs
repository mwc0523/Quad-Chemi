using UnityEngine;
using System.Collections;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using NUnit.Framework.Internal;

public class Unit : MonoBehaviour
{
    public UnitData data; // 위에서 만든 데이터 파일이 여기 꽂힙니다.
    private SpriteRenderer spriteRenderer;
    private List<GameObject> activeSuns = new List<GameObject>(); //살아있는 작은태양 리스트
    public UnitStatistics stats = new UnitStatistics();
    public float skillChanceBonus = 0f; //코일의 스킬 사용 확률 증가량
    private bool isCoilBuffActive = false;
    private Coroutine coilBuffCoroutine;


    [Header("시각적 효과")]
    public SpriteRenderer auraRenderer;  // 발밑 오라 (등급 색상 표현)
    public GameObject rangeCircle;       // 사거리 표시 원

    [Header("전투 설정")]
    private Transform target; // 현재 조준 중인 적
    public GameObject basicProjectilePrefab; // 기본 발사체 프리팹

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
    

    public IEnumerator ApplySkillChanceBuff(float amount, float duration)
    {
        isCoilBuffActive = true;
        skillChanceBonus = amount; // += 가 아니라 = 로 설정하여 중첩 원천 봉쇄
        Debug.Log($"버프 갱신: 현재 확률 보너스 {skillChanceBonus}");
        // 
        yield return new WaitForSeconds(duration);

        // 버프 종료
        skillChanceBonus = 0f;
        isCoilBuffActive = false;
        coilBuffCoroutine = null;
    }

    // --- 공격 로직 시작 ---
    IEnumerator AttackRoutine()
    {
        while (true)
        {
            if (data != null)
            {
                // 1. 타겟이 파괴되었거나(null), 비활성화되었다면 타겟을 비웁니다.
                if (target == null)
                {
                    target = null;
                }

                FindTarget();

                // 2. 타겟이 확실히 있을 때만 공격
                if (target != null)
                {
                    Shoot();
                    yield return new WaitForSeconds(1f / data.attackSpeed);
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    void FindTarget()
    {
        // 사거리 내의 모든 'Enemy' 레이어 오브젝트 찾기
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.attackRange/2, LayerMask.GetMask("Enemy"));

        if (hits.Length > 0)
        {
            // 가장 가까운 적 찾기
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

        // 1. OnAttack 트리거를 가진 스킬들 검사
        foreach (var skill in data.skills)
        {
            if (skill.trigger == SkillTrigger.OnAttack) //기본공격 존재
            {
                float finalChance = skill.triggerChance + skillChanceBonus; // 확률 계산!

                if (Random.value < finalChance)
                {
                    ExecuteSkill(skill);
                    skillFired = true;
                }
            }
            else if (skill.trigger == SkillTrigger.ReplaceBasicAttack) //기본공격 대체
            {
                ExecuteSkill(skill);
                basicAttackReplaced = true;
            }
        }
        if (!skillFired && !basicAttackReplaced) //스킬 안나갔으면 평타
        {
            ExecuteBasicAttack();
        }
    }

    // [기본 공격]
    void ExecuteBasicAttack()
    {
        if (data.projectilePrefab == null) return;
        GameObject projObj = Instantiate(data.projectilePrefab, transform.position, Quaternion.identity);
        Projectile proj = projObj.GetComponent<Projectile>();
        if (proj != null) proj.Setup(target, data.damage, ProjectileType.Normal, this);
    }

    // 스킬 효과 조립기 (부품들을 순서대로 실행)
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
                        // 1. 필드 위의 블리자드네모 개수 카운트
                        int blizzardCount = GetUnitCount("Blizzard");

                        // 2. 최종 데미지 배율 계산
                        // 기본 1800%(18.0f) + (개수 * 200%(2.0f))
                        // 예: 1마리일 때 20배(2000%), 2마리일 때 22배(2200%)
                        float finalDamageMultiplier = effect.value + (blizzardCount * 2.0f);

                        // 3. 계산된 데미지로 투사체 발사 루틴 실행
                        StartCoroutine(FireBlizzardRoutine(skill, effect, finalDamageMultiplier));
                    }
                    else
                    {
                        StartCoroutine(FireProjectileRoutine(skill, effect));
                    }
                    break;
                case SkillEffectType.Stun: ApplyStun(skill, effect); break;
                case SkillEffectType.Slow: ApplySlow(skill, effect); break;
                case SkillEffectType.DOT: ApplyDOT(skill, effect); break;
                case SkillEffectType.ChainLightning: ExecuteChainLightning(skill, effect); break;
                case SkillEffectType.BuffAlly: StartCoroutine(CoilBuffRoutine(skill, effect)); break;
                case SkillEffectType.DebuffEnemy:
                    if (data.unitName == "Steel")
                    {
                        // 1. 필드 위의 강철네모 개수 카운트
                        int steelCount = GetUnitCount("Steel");

                        // 2. 최종 피해 증가량 계산
                        // 기본 18%(0.18f) + (개수 * 2%(0.02f))
                        float finalDamageAmp = effect.value + (steelCount * 0.02f);

                        // 3. 계산된 값을 들고 디버프 실행
                        ApplySteelDebuff(skill, effect, finalDamageAmp);
                    }
                    else
                    {
                        ApplyDebuff(skill, effect);
                    }
                    break;
                case SkillEffectType.Execution: ApplyExecution(skill, effect); break;
                case SkillEffectType.SpawnEntity: ExecuteSpawnEntity(skill, effect); break;
                case SkillEffectType.TsunamiLauncher: TsunamiSkill(skill, effect); break;
            }
        }
    }
    
    //필드에 존재하는 동일 유닛 검색
    public static int GetUnitCount(string unitName)
    {
        int count = 0;
        // 필드 위의 모든 유닛을 검색 (성능 최적화가 필요하다면 UnitManager의 List를 참조)
        Unit[] allUnits = Object.FindObjectsOfType<Unit>();
        foreach (var u in allUnits)
        {
            if (u.data.unitName == unitName) count++;
        }
        return count;
    }

    // 1. 범위 데미지 (물, 땅, 용암 등)
    IEnumerator FireAreaRoutine(SkillInfo skill, SkillEffect effect)
    {
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

            activeSuns.Clear(); //기존에 작은 태양이 없던 경우
            int unitCount = Unit.GetUnitCount(data.unitName);

            int totalOrbits = 1 + unitCount;
            float angleStep = 360f / totalOrbits;

            // 2. 작은 태양들 생성
            for (int i = 0; i < totalOrbits; i++)
            {
                if (effect.effectPrefab != null)
                {
                    GameObject sun = Instantiate(effect.effectPrefab);
                    activeSuns.Add(sun); // 리스트에 추가

                    SunOrbit orbit = sun.GetComponent<SunOrbit>();
                    orbit.Init(transform, skill.range, data.damage * effect.value, i * angleStep, effect.duration, this);
                }
            }
            yield break; // 태양은 루프 방식이 아니므로 종료
        }


        int totalShots = effect.count > 0 ? effect.count : 1;
        float damageRadius;
        if(data.unitName == "Meteor") damageRadius = skill.range > 0 ? skill.range / 2f : 0.5f;
        else damageRadius = skill.range > 0 ? skill.range / 2f : data.attackRange / 2f;


        for (int i = 0; i < totalShots; i++)
        {
            // 1. 타겟이 사라졌을 때의 처리
            if (target == null)
            {
                // [예외] 메테오 유닛만 사거리 내 다른 적을 새로 찾음
                if (data.unitName == "Meteor")
                {
                    float searchRange = data.attackRange / 2f;
                    Collider2D[] potentialTargets = Physics2D.OverlapCircleAll(transform.position, searchRange, LayerMask.GetMask("Enemy"));

                    if (potentialTargets.Length > 0)
                    {
                        // 가장 가까운 적을 새 타겟으로 임시 설정
                        Transform closest = null;
                        float minDistance = Mathf.Infinity;
                        foreach (var hit in potentialTargets)
                        {
                            float dist = Vector3.Distance(transform.position, hit.transform.position);
                            if (dist < minDistance) { minDistance = dist; closest = hit.transform; }
                        }
                        // 임시 타겟 위치를 기억하게 함 (target 변수를 건드리지 않고 지역 변수로 해결)
                        target = closest;
                    }
                    else { yield break; } // 사거리 내 정말 적이 없으면 종료
                }
                else
                {
                    // 일반 유닛들은 기존처럼 타겟 없으면 바로 종료
                    yield break;
                }
            }

            // 2. 발사 위치 확정
            Vector3 spawnPos = skill.range > 0 ? target.position : transform.position;

            // 3. 이펙트 생성 (기존 코드 유지)
            if (effect.effectPrefab != null)
            {
                GameObject fx = Instantiate(effect.effectPrefab, spawnPos, Quaternion.identity);
                float effectScale = skill.range > 0 ? skill.range : data.attackRange;

                // Water 유닛 등 기존 스케일 로직 보존
                if (data.unitName == "Water")
                    fx.transform.localScale = new Vector3(effectScale, effectScale * 0.2f, 1f);
                else
                    fx.transform.localScale = new Vector3(effectScale, effectScale, 1f);

                Destroy(fx, effect.duration > 0 ? effect.duration : 1.0f);
            }

            // 4. 데미지 처리
            Collider2D[] damageHits = Physics2D.OverlapCircleAll(spawnPos, damageRadius, LayerMask.GetMask("Enemy"));
            foreach (var hit in damageHits)
            {
                Monster m = hit.GetComponent<Monster>();
                if (m != null) m.TakeDamage(data.damage * effect.value, this);
            }

            if (i < totalShots - 1) yield return new WaitForSeconds(0.3f);
        }
    }

    // 2. 발사체 스킬 (불네모 3발, 공기네모 관통풍 등)
    IEnumerator FireProjectileRoutine(SkillInfo skill, SkillEffect effect)
    {
        int shotCount = effect.count > 0 ? effect.count : 1;

        // 주변 적 다수 타겟팅 (불네모용)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.attackRange/2, LayerMask.GetMask("Enemy"));
        int currentShot = 0;

        foreach (var hit in hits)
        {
            if (currentShot >= shotCount) break;
            if (hit == null || hit.gameObject == null) continue;

            if (effect.effectPrefab != null)
            {
                GameObject projObj = Instantiate(effect.effectPrefab, transform.position, Quaternion.identity);
                Projectile proj = projObj.GetComponent<Projectile>();
                // 투사체 데미지는 기본공격력 * 배율
                if (proj != null) proj.Setup(hit.transform, data.damage * effect.value, proj.type, this);
            }
            currentShot++;
            yield return new WaitForSeconds(0.05f); // 다발 사격 시 약간의 딜레이
        }
    }
    IEnumerator FireBlizzardRoutine(SkillInfo skill, SkillEffect effect, float multiplier)
    {
        if (target == null || effect.effectPrefab == null) yield break;

        // 블리자드는 '일자로 긴 거리'를 가야 하므로 
        // 프리팹 내부 Projectile 스크립트의 Type이 'Penetrate'로 설정되어 있어야 합니다.
        GameObject projObj = Instantiate(effect.effectPrefab, transform.position, Quaternion.identity);
        Projectile proj = projObj.GetComponent<Projectile>();

        if (proj != null)
        {
            // 계산된 배율(multiplier)을 적용한 최종 데미지 전달
            proj.Setup(target, data.damage * multiplier, ProjectileType.Penetrate, this);
        }

        yield return null;
    }
    void SpawnSkillEffect(SkillInfo skill, SkillEffect effect, Vector3 targetPos) //이펙트 생성기
    {
        if (effect.effectPrefab == null) return;

        Vector3 spawnPos = skill.range > 0 ? targetPos : transform.position;
        GameObject fx = Instantiate(effect.effectPrefab, spawnPos, Quaternion.identity);

        // 사거리에 맞게 이펙트 크기 조절
        float scale = skill.range > 0 ? skill.range/2 : data.attackRange/2;
        fx.transform.localScale = new Vector3(scale, scale, 1f);

        // 지속시간이 끝나면 이펙트 삭제
        float lifeTime = effect.duration > 0 ? effect.duration : 1.0f;
        Destroy(fx, lifeTime);
    }

    // 3. 기절 (땅네모, 새싹네모 등)
    void ApplyStun(SkillInfo skill, SkillEffect effect)
    {
        float checkRange = skill.range > 0 ? skill.range/2 : data.attackRange/2;
        Vector3 checkPos = skill.range > 0 ? target.position : transform.position;
        SpawnSkillEffect(skill, effect, checkPos);
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, checkRange, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null) m.ApplyStun(effect.duration);
        }
    }

    // 4. 슬로우 (물풍선, 얼음, 모래폭풍)
    void ApplySlow(SkillInfo skill, SkillEffect effect)
    {
        float checkRange = skill.range > 0 ? skill.range / 2 : data.attackRange / 2;
        Vector3 checkPos = skill.range > 0 ? target.position : transform.position;
        SpawnSkillEffect(skill, effect, checkPos);
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, checkRange, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null) m.ApplySlow(effect.value, effect.duration); // value: 감속량 (0.2 = 20%)
        }
    }

    // 5. 지속 데미지 (새싹네모 DOT)
    void ApplyDOT(SkillInfo skill, SkillEffect effect)
    {
        float checkRange = skill.range > 0 ? skill.range / 2 : data.attackRange / 2;
        Vector3 checkPos = skill.range > 0 ? target.position : transform.position;
        SpawnSkillEffect(skill, effect, checkPos);
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, checkRange, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null) m.ApplyDOT(data.damage * effect.value, effect.duration, this);
        }
    }

    // 6. 체인 라이트닝 (전기네모)
    void ExecuteChainLightning(SkillInfo skill, SkillEffect effect)
    {
        if (target != null && effect.effectPrefab != null)
        {
            GameObject chainObj = Instantiate(effect.effectPrefab, transform.position, Quaternion.identity);
            ChainLightning chain = chainObj.GetComponent<ChainLightning>();
            if (chain != null)
            {
                // 시작타겟, 데미지, 튕기는 횟수, 튕기는 사거리
                chain.Setup(target, data.damage * effect.value, effect.count, 3f, this);
            }
        }
    }

    // 7. 적 디버프
    void ApplyDebuff(SkillInfo skill, SkillEffect effect)
    {
        Vector3 checkPos;
        float checkRadius;
        if (skill.range <= 0)
        {
            // 규칙: 0 이하이면 유닛 본인 중심, 본인의 공격 범위 전체
            checkPos = transform.position;
            checkRadius = data.attackRange / 2f;
        }
        else
        {
            // 규칙: 0보다 크면 타겟 중심, skill.range 크기만큼
            if (target == null) return; 
            checkPos = target.position;
            checkRadius = skill.range / 2f;
        }

        // 2. 범위 내 모든 적 스캔
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, checkRadius, LayerMask.GetMask("Enemy"));

        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null)
            {
                // 로직: 데미지 증폭 디버프 적용
                m.ApplyDamageAmp(effect.value, effect.duration);

                // 시각: 몬스터 몸에 바위(또는 디버프 아이콘) 부착
                if (effect.effectPrefab != null)
                {
                    m.AddVisualEffect(effect.effectPrefab, effect.duration);
                }
            }
        }
    }

    void ApplySteelDebuff(SkillInfo skill, SkillEffect effect, float calculatedValue)
    {
        // 강철벽 소환 위치 (본인 중심, 사거리만큼)
        Vector3 checkPos = transform.position;
        float checkRadius = data.attackRange / 2f;

        // 시각 효과 (강철벽 프리팹 소환)
        if (effect.effectPrefab != null)
        {
            GameObject fx = Instantiate(effect.effectPrefab, checkPos, Quaternion.identity);
            fx.transform.localScale = new Vector3(data.attackRange, data.attackRange, 1f);
            Destroy(fx, effect.duration);
        }

        // 범위 내 적 스캔
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, checkRadius, LayerMask.GetMask("Enemy"));

        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null)
            {
                // [스킬1] 4초간 기절 (duration은 4로 설정되어 있어야 함)
                m.ApplyStun(effect.duration);

                // [스킬1 + 스킬2] 최종 피해 증가 적용
                m.ApplyDamageAmp(calculatedValue, effect.duration);
            }
        }
    }

    // 8. 처형 (체력이 특정 퍼센트 이하인 적 즉사)
    void ApplyExecution(SkillInfo skill, SkillEffect effect)
    {
        float checkRange = skill.range > 0 ? skill.range / 2 : data.attackRange / 2;
        Vector3 checkPos = skill.range > 0 ? target.position : transform.position;

        SpawnSkillEffect(skill, effect, checkPos);

        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, checkRange, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            // effect.value가 0.1이라면 최대 체력의 10% 이하일 때 즉사
            if (m != null && (m.hp / m.maxhp) <= effect.value)
            {
                m.TakeDamage(9999999f, this); // 즉사 데미지
            }
        }
    }

    // 9. 독립 개체 소환 (해일, 강철벽 등)
    void ExecuteSpawnEntity(SkillInfo skill, SkillEffect effect)
    {
        if (effect.effectPrefab == null) return;
        // 내 위치나 타겟 위치에 그냥 프리팹을 소환하고 끝냅니다. (로직은 소환된 프리팹 스크립트가 알아서 함)
        Vector3 spawnPos = skill.range > 0 && target != null ? target.position : transform.position;
        Instantiate(effect.effectPrefab, spawnPos, Quaternion.identity);
    }

    // 10. 코일 스킬
    IEnumerator CoilBuffRoutine(SkillInfo skill, SkillEffect effect)
    {
        // 1. 필드 위의 코일 수 계산
        int coilCount = GetUnitCount("Coil");

        // 2. 최종 증가량 계산: 기본 10% + (코일 당 5%)
        // 예: 코일이 2마리면 10 + (2 * 5) = 20% 증가
        float finalBuffValue = effect.value + (coilCount * 0.05f);

        // 3. 범위 내 아군 찾기 (사거리/2)
        float buffRange = skill.range > 0 ? skill.range / 2f : data.attackRange / 2f;
        Collider2D[] allies = Physics2D.OverlapCircleAll(transform.position, buffRange, LayerMask.GetMask("Unit"));

        foreach (var ally in allies)
        {
            Unit unit = ally.GetComponent<Unit>();
            if (unit != null)
            {
                if (unit.data != null && unit.data.unitName == "Coil") continue;
                unit.AddCoilBuff(finalBuffValue, effect.duration);
            }
        }

        // 시각 효과 (있다면)
        if (effect.effectPrefab != null)
        {
            GameObject fx = Instantiate(effect.effectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, effect.duration);
        }
        yield return null;
    }

    // 11. 해일 스킬
    void TsunamiSkill(SkillInfo skill, SkillEffect effect)
    {
        int tsunamiCount = GetUnitCount("Tsunami");
        float finalTsunamiDamage = data.damage * (effect.value + (tsunamiCount * 1.0f));

        // 3. 해일 생성
        if (effect.effectPrefab != null)
        {
            GameObject tsunamiObj = Instantiate(effect.effectPrefab, transform.position, Quaternion.identity);
            TsunamiEntity tsunami = tsunamiObj.GetComponent<TsunamiEntity>();

            if (tsunami != null)
            {
                // 데미지와 지속시간(3초) 전달
                tsunami.Setup(finalTsunamiDamage, effect.duration > 0 ? effect.duration : 3f, this);
            }
        }
    }

    // 12. 맹독 전용 스킬 루틴
    IEnumerator PoisonSkillRoutine(SkillInfo skill, SkillEffect effect)
    {
        if (target == null) yield break;

        // 1. 필드에 존재하는 '맹독' 유닛 개수 파악 (자신 포함)
        int poisonCount = GetUnitCount("Poison");

        float finalDamageMultiplier = effect.value + (poisonCount * 2.0f);
        float finalDamagePerSecond = data.damage * finalDamageMultiplier;

        // 3. 독병 투척 이펙트 생성 (선택 사항: 투사체가 날아가는 연출이 없다면 바로 장판 생성)
        Vector3 spawnPos = target.position;

        if (effect.effectPrefab != null)
        {
            GameObject poisonZone = Instantiate(effect.effectPrefab, spawnPos, Quaternion.identity);

            // 스킬 범위 설정 (타일 3칸 범위 = skill.range 활용)
            float zoneScale = skill.range > 0 ? skill.range : 3f;
            poisonZone.transform.localScale = new Vector3(zoneScale, zoneScale, 1f);

            // 4. 지속 데미지 로직 (장판 프리팹에 스크립트가 있다면 그쪽에 데미지를 넘겨주고, 
            // 없다면 여기서 소환된 동안 반복 데미지를 주는 로직을 작성합니다.)

            // 간단 구현: 장판 프리팹 자체가 틱 데미지를 주는 구조가 아니라면 
            // 아래와 같이 코루틴에서 직접 처리 가능합니다.
            float elapsed = 0f;
            float duration = effect.duration > 0 ? effect.duration : 5f;

            while (elapsed < duration)
            {
                // 매 초(1초 간격) 장판 위의 적들에게 데미지
                Collider2D[] hits = Physics2D.OverlapCircleAll(spawnPos, zoneScale / 2f, LayerMask.GetMask("Enemy"));
                foreach (var hit in hits)
                {
                    Monster m = hit.GetComponent<Monster>();
                    if (m != null)
                    {
                        // 초당 데미지이므로 1초에 한 번씩 들어감 (틱을 더 쪼개려면 0.1f 등으로 수정)
                        m.TakeDamage(finalDamagePerSecond, this);
                    }
                }
                yield return new WaitForSeconds(1f);
                elapsed += 1f;
            }

            Destroy(poisonZone);
        }
    }









    public void AddCoilBuff(float amount, float duration)
    {
        // 1. 이미 버프 코루틴이 돌고 있다면 강제 종료
        if (coilBuffCoroutine != null)
        {
            StopCoroutine(coilBuffCoroutine);
            coilBuffCoroutine = null;

            // [중요] 중첩 방지: 기존에 적용되어 있던 버프 수치를 먼저 완전히 제거
            // 이전에 적용된 수치가 얼마였든 현재 보너스에서 0으로 리셋하거나 
            // isActive 체크를 통해 정확히 빼줘야 합니다.
            if (isCoilBuffActive)
            {
                // 현재 amount를 빼는 게 아니라, 보너스 자체를 0으로 밀거나 
                // 마지막에 적용했던 값을 저장해뒀다 빼야 안전합니다.
                // 여기선 가장 확실한 방법인 '0으로 초기화'를 사용하거나 
                // 아래 코루틴 구조로 개선합니다.
                skillChanceBonus = 0f;
            }
        }

        // 2. 새로운 버프 코루틴 시작
        coilBuffCoroutine = StartCoroutine(ApplySkillChanceBuff(amount, duration));
    }

    public void SellUnit()
    {
        int sellPrice = GetSellPrice();
        if (InGameManager.instance != null)
        {
            InGameManager.instance.AddCoin(sellPrice);
        }
        Destroy(gameObject);
    }

    // 등급에 따른 판매 가격 계산
    int GetSellPrice()
    {
        // 기본 가격 설정
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

    // 데이터를 받아서 유닛을 초기화하는 함수
    public void SetUnit(UnitData newData)
    {
        data = newData;

        // 1. 유닛 외형 설정
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = data.unitSprite;

        // 2. 등급별 오라 색상 설정
        SetGradeVisual();

        if (rangeCircle != null) rangeCircle.SetActive(false);
    }

    void SetGradeVisual()
    {
        if (auraRenderer == null) return;

        // 요청하신 등급별 색상 정의
        switch (data.grade)
        {
            case UnitGrade.Low: auraRenderer.color = Color.white; break;
            case UnitGrade.Middle: auraRenderer.color = new Color(0.5f, 1f, 0.5f); break; // 연두
            case UnitGrade.High: auraRenderer.color = Color.blue; break;
            case UnitGrade.Epic: auraRenderer.color = new Color(0.6f, 0f, 1f); break;   // 보라
            case UnitGrade.Legend: auraRenderer.color = Color.yellow; break;
            case UnitGrade.Myth: auraRenderer.color = Color.red; break;
        }
    }

    // 사거리를 켜는 함수 (매니저가 호출)
    public void ShowRange(bool x)
    {
        if (data == null) return;

        // 사거리 표시
        if (rangeCircle != null)
        {
            rangeCircle.SetActive(x);
            if (x)
            {
                float scale = data.attackRange * 2f;
                rangeCircle.transform.localScale = new Vector3(scale, scale, 1);
            }
        }
        // 판매 버튼 표시
        if (sellButton != null)
        {
            sellButton.SetActive(x);
        }
    }
}