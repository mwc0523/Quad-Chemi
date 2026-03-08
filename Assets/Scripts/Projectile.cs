using UnityEngine;

public enum ProjectileType { Normal, Area, Penetrate }

public class Projectile : MonoBehaviour //네모들이 공통으로 사용할 스크립트
{
    private Transform target;
    private float damage;
    private ProjectileType type;
    private float speed = 10f;

    // 물네모(범위), 땅네모용 설정
    private float areaRadius;
    private float slowPercent;
    private float stunDuration;

    // 발사체 초기화 (불네모, 공기네모 등에서 부름)
    public void Setup(Transform _target, float _damage, ProjectileType _type)
    {
        target = _target;
        damage = _damage;
        type = _type;

        // 공기네모(관통)라면 타겟 방향으로 일단 회전
        if (type == ProjectileType.Penetrate && target != null)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            Destroy(gameObject, 3f); // 관통탄은 3초뒤 자동 소멸
        }
    }

    // 물네모 전용 초기화
    public void SetupArea(Transform _target, float _damage, float _radius, float _slow)
    {
        Setup(_target, _damage, ProjectileType.Area);
        areaRadius = _radius;
        slowPercent = _slow;
    }

    void Update()
    {
        if (type == ProjectileType.Penetrate)
        {
            // 공기네모: 앞으로 쭉 전진
            transform.Translate(Vector3.right * speed * Time.deltaTime);
        }
        else if (target != null)
        {
            // 불, 물네모: 적을 추적
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, target.position) < 0.2f)
            {
                Explode();
            }
        }
        else if (type != ProjectileType.Penetrate)
        {
            // 타겟이 죽으면 발사체도 소멸
            Destroy(gameObject);
        }
    }

    void Explode()
    {
        if (target == null && type != ProjectileType.Area)
        {
            Destroy(gameObject);
            return;
        }
        if (type == ProjectileType.Normal) // 일반/불네모 등
        {
            if (target != null)
            {
                Monster m = target.GetComponent<Monster>();
                if (m != null)
                {
                    m.TakeDamage(damage);
                }
            }
        }
        else if (type == ProjectileType.Area) // 물네모/범위형
        {
            // 내 위치를 기준으로 원형 범위 안의 모든 적 감지
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, areaRadius, LayerMask.GetMask("Enemy"));
            foreach (var hit in hits)
            {
                Monster m = hit.GetComponent<Monster>();
                if (m != null)
                {
                    m.TakeDamage(damage);
                    if (slowPercent > 0) m.ApplySlow(slowPercent, 3f);
                }
            }
        }
        Destroy(gameObject);
    }

    // 공기네모 관통 처리
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (type == ProjectileType.Penetrate && collision.CompareTag("Enemy"))
        {
            collision.GetComponent<Monster>().TakeDamage(damage);
        }
    }
}