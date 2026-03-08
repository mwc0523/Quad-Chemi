using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Monster : MonoBehaviour
{
    private Transform[] waypoints;
    private int currentIndex = 0;

    [Header("능력치")]
    public float maxhp = 1000f;
    public float hp;
    public float baseSpeed = 1f;
    private float currentSpeed;

    [Header("UI 연결")]
    public Slider hpSlider;

    [Header("상태")]
    public bool isStunned = false;
    private bool isDead = false;

    public void Setup(Transform[] path)
    {
        hp = maxhp;
        waypoints = path;
        if (hpSlider != null)
        {
            hpSlider.minValue = 0;
            hpSlider.maxValue = maxhp;
            hpSlider.value = hp;
        }
        currentSpeed = baseSpeed;
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
            if (currentIndex >= waypoints.Length)
            {
                currentIndex = 0;
            }
        }
    }

    // 데미지 함수
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        hp -= damage;
        if (hpSlider != null)
        {
            hpSlider.value = hp;
        }
        if (hp <= 0) Die();
    }

    // 감속 효과 (예: 0.2f면 20% 느려짐)
    public void ApplySlow(float percent, float duration)
    {
        StopCoroutine("SlowRoutine");
        StartCoroutine(SlowRoutine(percent, duration));
    }

    IEnumerator SlowRoutine(float percent, float duration)
    {
        currentSpeed = baseSpeed * (1f - percent);
        yield return new WaitForSeconds(duration);
        currentSpeed = baseSpeed;
    }

    // 기절 효과
    public void ApplyStun(float duration)
    {
        StopCoroutine("StunRoutine");
        StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        InGameManager.instance.OnMonsterDestroyed();
        InGameManager.instance.AddCoin(1);
        Destroy(gameObject);
    }
}