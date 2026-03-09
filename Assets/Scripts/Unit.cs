using UnityEngine;
using System.Collections;
using UnityEditor.Experimental.GraphView;

public class Unit : MonoBehaviour
{
    public UnitData data; // 위에서 만든 데이터 파일이 여기 꽂힙니다.
    private SpriteRenderer spriteRenderer;

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
        if (proj != null) proj.Setup(target, data.damage, ProjectileType.Normal);
    }

    // 스킬 효과 조립기 (부품들을 순서대로 실행)
    void ExecuteSkill(SkillInfo skill)
    {
        foreach (var effect in skill.effects)
        {
            switch (effect.effectType)
            {
                case SkillEffectType.DamageArea:
                    FireAreaProjectile(skill, effect);
                    break;
                case SkillEffectType.DamageProjectile:
                    StartCoroutine(FireProjectileRoutine(skill, effect));
                    break;
                case SkillEffectType.Stun:
                    ApplyStun(skill, effect);
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
            }
        }
    }

    // 1. 범위 데미지 (물, 땅, 용암 등)
    void FireAreaProjectile(SkillInfo skill, SkillEffect effect)
    {
        if (target == null || effect.effectPrefab == null) return;

        // 1. 타겟 위치(또는 유닛 위치)에 이펙트 생성
        Vector3 spawnPos = (skill.range > 0) ? target.position : transform.position;
        GameObject fx = Instantiate(effect.effectPrefab, spawnPos, Quaternion.identity);

        float effectScale = data.attackRange * 1.3f;
        fx.transform.localScale = new Vector3(effectScale, effectScale, 1f);
        float lifeTime = effect.duration > 0 ? effect.duration : 1.0f;
        Destroy(fx, lifeTime);

        // 3. 실제 데미지 처리 (OverlapCircle 사용)
        float damageRange = data.attackRange / 2;
        Collider2D[] hits = Physics2D.OverlapCircleAll(spawnPos, damageRange, LayerMask.GetMask("Enemy"));

        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null)
            {
                m.TakeDamage(data.damage * effect.value);
            }
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
                if (proj != null) proj.Setup(hit.transform, data.damage * effect.value, proj.type);
            }
            currentShot++;
            yield return new WaitForSeconds(0.05f); // 다발 사격 시 약간의 딜레이
        }
    }

    // 3. 기절 (땅네모, 새싹네모 등)
    void ApplyStun(SkillInfo skill, SkillEffect effect)
    {
        float checkRange = skill.range > 0 ? skill.range/2 : data.attackRange/2;
        Vector3 checkPos = skill.range > 0 ? target.position : transform.position;

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
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, checkRange, LayerMask.GetMask("Enemy"));

        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            // value: 초당 데미지 배율 (400% = 4)
            if (m != null) m.ApplyDOT(data.damage * effect.value, effect.duration);
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
                chain.Setup(target, data.damage * effect.value, effect.count, 3f);
            }
        }
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