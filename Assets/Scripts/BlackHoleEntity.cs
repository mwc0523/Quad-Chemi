using UnityEngine;

public class BlackHoleEntity : MonoBehaviour
{
    private float damagePerSecond;
    private float pullRadius;
    private float pullForce = 1.5f; // 당기는 힘 (필요시 조절)
    private Unit owner;

    public void Setup(float dps, float duration, float radius, Unit unit)
    {
        damagePerSecond = dps;
        pullRadius = radius;
        owner = unit;

        // 크기를 시각적으로 맞춰줌 (반경의 2배 = 지름)
        transform.localScale = new Vector3(radius * 2, radius * 2, 1);

        Destroy(gameObject, duration);
        // 1초마다 데미지 틱 발생
        InvokeRepeating(nameof(DealDamageTick), 0f, 1f);
    }

    void Update()
    {
        // 범위 내 적들을 중심부로 서서히 끌어당김
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, pullRadius, LayerMask.GetMask("Enemy"));
        foreach (var col in enemies)
        {
            col.transform.position = Vector3.MoveTowards(col.transform.position, transform.position, pullForce * Time.deltaTime);
        }
    }

    void DealDamageTick()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, pullRadius, LayerMask.GetMask("Enemy"));
        foreach (var col in enemies)
        {
            Monster m = col.GetComponent<Monster>();
            if (m != null && owner != null)
            {
                m.TakeDamage(damagePerSecond, owner);
            }
        }
    }
}