using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Monster : MonoBehaviour
{
    private Transform[] waypoints;
    private int currentIndex = 0;

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

    public void Setup(Transform[] path, int currentRound)
    {

        // 1. 일반 몬스터 체력 계산 (10라운드마다 증가량 상승)
        float baseHp = 0f;
        float cumulativeHp = baseHp;

        for (int i = 1; i <= currentRound; i++)
        {
            // 10라운드마다 증가 수치를 높임 (단계별 가속 등차수열)
            // 1~10렙: +500, 11~20렙: +1000 ... 91~100렙: +5000
            float increaseStep = Mathf.CeilToInt(i / 10f) * 500f;
            cumulativeHp += increaseStep;
        }

        maxhp = cumulativeHp;

        /*// 2. 미니보스 & 보스 체력 보정 (아직 프리팹은 없지만 로직만 선언)
        // 5라운드마다 미니보스 (10, 20... 포함되므로 조건 확인)
        if (currentRound % 5 == 0)
        {
            if (currentRound % 10 == 0)
            {
                // 10라운드 단위 진보스: 일반몹의  라운드/10배
                
                maxhp *= currentRound/10f;

                // 100라운드 최종 보스: 특별히 더 강력하게 (약 600만~700만 HP)
                if (currentRound == 100) maxhp *= 1.5f;
            }
            else
            {
                // 5, 15, 25... 미니보스: 일반몹의 4배
                maxhp *= 4f;
            }
        }*/

        //속도 설정
        currentSpeed = baseSpeed + (currentRound * 0.01f); // 라운드당 속도 미세 증가

        //방어력 설정
        defense = currentRound;

        hp = maxhp;
        waypoints = path;

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
        if (isDead) return;

        float reductionPercent = defense / (defense + 100f); //방어력 계산

        float finalDamage = damage * (1f - reductionPercent) * damageMultiplier; //실제 받는 데미지 계산
        finalDamage = Mathf.Round(finalDamage);

        finalDamage = Mathf.Max(1f, finalDamage); //최소 1은 들어가게 함

        if (attacker != null)
        {
            if(hp < finalDamage) attacker.stats.totalDamage += hp;
            else attacker.stats.totalDamage += damage; // 데미지 기록
        }
        hp -= finalDamage;

        if (hpSlider != null) hpSlider.value = hp;
        if (hp <= 0)
        {
            attacker.stats.killCount++;
            Die();
        }
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
        visual.transform.localScale = Vector3.one;

        Destroy(visual, duration);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        int reward = 1; // 기본 코인

        
        // 라운드 정보를 알고 있다면 보너스 지급
        int currentRound = InGameManager.instance.currentRound;
        /*
        if (currentRound % 10 == 0)
            reward = 50; // 보스 보상
        else if (currentRound % 5 == 0)
            reward = 20; // 미니보스 보상
        */
        InGameManager.instance.OnMonsterDestroyed();
        InGameManager.instance.AddCoin(reward);

        // 100라운드 보스 처치 시 게임 승리 팝업 호출 (예시)
        if (currentRound == 100)
        {
            // InGameManager.instance.ShowVictoryUI(); 
        }

        Destroy(gameObject);
    }
}