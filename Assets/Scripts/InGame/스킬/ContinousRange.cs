using UnityEngine;
using System.Collections;

public class ContinuousRange : MonoBehaviour
{
    private float damagePerTick;
    private float radius;
    private float duration;
    private float tickInterval;
    private Unit attacker;
    private LayerMask targetLayer;

    // 데이터를 주입받는 초기화 함수
    public void Initialize(float totalDamagePerSec, float radius, float duration, float tickInterval, Unit attacker, string layerName = "Enemy")
    {
        this.radius = radius;
        this.duration = duration;
        this.tickInterval = tickInterval;
        this.attacker = attacker;
        this.targetLayer = LayerMask.GetMask(layerName);

        // 초당 데미지를 틱당 데미지로 환산 (예: 초당 300%인데 0.5초 주기면 한 번에 150%씩)
        this.damagePerTick = totalDamagePerSec * tickInterval;

        StartCoroutine(ProcessRoutine());
        Destroy(gameObject, duration);
    }

    private IEnumerator ProcessRoutine()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            ApplyTickDamage();

            elapsed += tickInterval;
            yield return new WaitForSeconds(tickInterval);
        }
    }

    private void ApplyTickDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, targetLayer);
        foreach (var hit in hits)
        {
            // Monster뿐만 아니라 IDamageable 같은 인터페이스가 있다면 더 범용적입니다.
            Monster m = hit.GetComponent<Monster>();
            if (m != null)
            {
                m.TakeDamage(damagePerTick, attacker);
            }
        }
    }

    // 에디터에서 범위를 시각적으로 확인
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}