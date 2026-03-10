using UnityEngine;
using System.Collections;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;

public class Unit : MonoBehaviour
{
    public UnitData data; // 위에서 만든 데이터 파일이 여기 꽂힙니다.
    private SpriteRenderer spriteRenderer;
    private List<GameObject> activeSuns = new List<GameObject>(); //살아있는 작은태양 리스트
    public UnitStatistics stats = new UnitStatistics();


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
                float finalChance = skill.triggerChance; // 확률 계산!

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
                case SkillEffectType.DamageArea: StartCoroutine(FireAreaRoutine(skill, effect)); break;
                case SkillEffectType.DamageProjectile: StartCoroutine(FireProjectileRoutine(skill, effect)); break;
                case SkillEffectType.Stun: ApplyStun(skill, effect); break;
                case SkillEffectType.Slow: ApplySlow(skill, effect); break;
                case SkillEffectType.DOT: ApplyDOT(skill, effect); break;
                case SkillEffectType.ChainLightning: ExecuteChainLightning(skill, effect); break;
                case SkillEffectType.DebuffEnemy: ApplyDebuff(skill, effect); break;
                case SkillEffectType.Execution: ApplyExecution(skill, effect); break;
                case SkillEffectType.SpawnEntity: ExecuteSpawnEntity(skill, effect); break;
            }
        }
    }
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
                Debug.Log("태양 지속시간 연장!");
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
    void SpawnSkillEffect(SkillInfo skill, SkillEffect effect, Vector3 targetPos) //이펙트 생성기
    {
        if (effect.effectPrefab == null) return;

        Vector3 spawnPos = skill.range > 0 ? targetPos : transform.position;
        GameObject fx = Instantiate(effect.effectPrefab, spawnPos, Quaternion.identity);

        // 사거리에 맞게 이펙트 크기 조절
        float scale = skill.range > 0 ? skill.range : data.attackRange;
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