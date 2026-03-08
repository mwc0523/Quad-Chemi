using UnityEngine;
using System.Collections;

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

    [Header("드래그 & 합성 설정")]
    private Vector3 originalPos;       // 드래그 시작 전 원래 위치
    private Transform originalTile;    // 원래 배치되어 있던 타일(부모)
    private bool isDragging = false;

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
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.attackRange, LayerMask.GetMask("Enemy"));

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

        // 1. 확률 체크 (스킬 발동 여부)
        if (data.skillProjectilePrefab != null && Random.value < data.skillChance)
        {
            ExecuteSkill(); //스킬 발사
        }
        else
        {
            ExecuteBasicAttack(); //기본 공격
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

    // [스킬 실행] 원소별 분기
    void ExecuteSkill()
    {
        switch (data.unitName)
        {
            case "Fire": // 불네모: 3마리 타겟 공격
                StartCoroutine(FireSkillRoutine());
                break;

            case "Water": // 물네모: 범위 감속 공격
                GameObject waterObj = Instantiate(data.skillProjectilePrefab, transform.position, Quaternion.identity);
                Projectile waterProj = waterObj.GetComponent<Projectile>();
                if (waterProj != null)
                    waterProj.SetupArea(target, data.damage * data.skillDamageMultiplier, 2f, 0.2f); // 반지름 2, 20% 감속
                break;

            case "Earth": // 땅네모: 즉시 주변 기절 (발사체 없음)
                ExecuteEarthSkill();
                break;

            case "Air": // 공기네모: 직선 관통
                GameObject airObj = Instantiate(data.skillProjectilePrefab, transform.position, Quaternion.identity);
                Projectile airProj = airObj.GetComponent<Projectile>();
                if (airProj != null)
                    airProj.Setup(target, data.damage * data.skillDamageMultiplier, ProjectileType.Penetrate);
                break;
        }
    }

    IEnumerator FireSkillRoutine()
    {
        // 사거리 내 적 최대 3명 찾기
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.attackRange, LayerMask.GetMask("Enemy"));
        int count = 0;
        foreach (var hit in hits)
        {
            if (count >= 3) break;
            GameObject projObj = Instantiate(data.skillProjectilePrefab, transform.position, Quaternion.identity);
            projObj.GetComponent<Projectile>().Setup(hit.transform, data.damage * data.skillDamageMultiplier, ProjectileType.Normal);
            count++;
            yield return new WaitForSeconds(0.05f); // 아주 짧은 간격으로 발사
        }
    }

    void ExecuteEarthSkill()
    {
        if (data.skillProjectilePrefab != null)
        {
            GameObject effect = Instantiate(data.skillProjectilePrefab, transform.position, Quaternion.identity);
            
            // 이펙트 크기를 사거리에 맞춰서 키우고 싶다면 (선택사항)
            float effectScale = (float)1.27*data.attackRange;
            effect.transform.localScale = new Vector3(effectScale, effectScale, 1);
            Destroy(effect, 1.0f);
        }
        // 2. 실제 데미지 및 기절 로직 (기존과 동일)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.attackRange, LayerMask.GetMask("Enemy"));

        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null)
            {
                m.TakeDamage(data.damage * data.skillDamageMultiplier);
                m.ApplyStun(1.0f); // 1초 기절
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

    // 유닛 클릭 이벤트
    private void OnMouseDown()
    {
        // 매니저에게 이 유닛이 클릭되었음을 알립니다.
        InGameManager.instance.SelectUnit(this);
        isDragging = true;
        originalPos = transform.position;
        originalTile = transform.parent;
        StopAllCoroutines();
        if (spriteRenderer != null) spriteRenderer.sortingOrder = 100;
    }
    private void OnMouseDrag()
    {
        if (!isDragging) return;

        // 마우스 위치를 월드 좌표로 변환해서 유닛 위치에 적용
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f; // 2D 뷰이므로 z축은 0으로 고정
        transform.position = mousePos;
    }

    private void OnMouseUp()
    {
        isDragging = false;
        if (spriteRenderer != null) spriteRenderer.sortingOrder = 0; // 순서 원상복구

        // 1. 놓은 자리에 무엇이 있는지 레이캐스트로 검사
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        // 임시 로직: 일단 무조건 제자리로 돌아가게 해둡니다. (다음 단계에서 여기에 이동/합성 로직을 넣을 겁니다)
        ReturnToOriginalPosition();

        StartCoroutine(AttackRoutine());
    }

    public void ReturnToOriginalPosition()
    {
        transform.position = originalPos;
        transform.SetParent(originalTile);
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