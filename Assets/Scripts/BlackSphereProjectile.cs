using UnityEngine;

public class BlackSphereProjectile : MonoBehaviour
{
    private Transform target;
    private float mainDamage;
    private float splashDamage;
    private float splashRadius;
    private Unit owner;
    private float speed = 8f; // 투사체 날아가는 속도

    public void Setup(Transform targetInfo, float mainDmg, float splashDmg, float radius, Unit unit)
    {
        target = targetInfo;
        mainDamage = mainDmg;
        splashDamage = splashDmg;
        splashRadius = radius;
        owner = unit;
    }

    void Update()
    {
        // 타겟이 죽거나 없어지면 구체도 사라짐 (원하면 그냥 직진하게 바꿀 수도 있음)
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        // 적에게 거의 닿았을 때 폭발!
        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            Explode();
        }
    }

    void Explode()
    {
        // 1. 첫 타겟에게 2000% 데미지
        Monster m = target.GetComponent<Monster>();
        if (m != null) m.TakeDamage(mainDamage, owner);

        // 2. 주변 반경에 500% 스플래시 폭발 데미지
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, splashRadius, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            Monster splashTarget = hit.GetComponent<Monster>();
            // 첫 타겟 제외하고 주변 적에게만 500% 데미지 (중복 타격 방지)
            if (splashTarget != null && splashTarget != m)
            {
                splashTarget.TakeDamage(splashDamage, owner);
            }
        }

        // TODO: 파티클 효과 같은 걸 넣고 싶다면 여기서 Instantiate()로 생성!

        Destroy(gameObject); // 구체 파괴
    }
}