using UnityEngine;

public enum ProjectileType { Normal, Skill, Penetrate }

public class Projectile : MonoBehaviour
{
    private Transform target;
    private float damage;
    public ProjectileType type;
    private float speed = 12f;
    private float explosionRadius;
    public SkillEffect skillEffect;

    // 셋업 함수 (유닛에서 호출)
    public void Setup(Transform _target, float _damage, ProjectileType _type)
    {
        target = _target;
        damage = _damage;
        type = _type;

        if (type == ProjectileType.Penetrate)
        {
            // 관통탄은 목표 방향으로 회전 후 직진
            if (target != null)
            {
                Vector3 dir = (target.position - transform.position).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
            Destroy(gameObject, 3f);
        }
    }

    void Update()
    {
        if (type == ProjectileType.Penetrate)
        {
            transform.Translate(Vector3.right * speed * Time.deltaTime);
        }
        else if (target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, target.position) < 0.1f)
            {
                HitTarget(target.GetComponent<Monster>());
            }
        }
        else
        {
            if (type != ProjectileType.Penetrate) Destroy(gameObject);
        }
    }
    public void SetupArea(Transform _target, float _damage, float _radius, SkillEffect _effect)
    {
        Setup(_target, _damage, ProjectileType.Skill);
        explosionRadius = _radius;
        skillEffect = _effect;
    }

    void HitTarget(Monster m)
    {
        // 범위 공격(Area)인 경우
        if (explosionRadius > 0)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, LayerMask.GetMask("Enemy"));
            foreach (var hit in hits)
            {
                Monster targetMonster = hit.GetComponent<Monster>();
                if (targetMonster != null)
                {
                    targetMonster.TakeDamage(damage);

                    // 슬로우 효과가 붙어있다면 적용 (물네모의 경우)
                    if (skillEffect.effectType == SkillEffectType.Slow)
                    {
                        targetMonster.ApplySlow(skillEffect.value, skillEffect.duration);
                    }
                }
            }
        }
        else if (m != null) // 단일 공격인 경우
        {
            m.TakeDamage(damage);
        }

        if (type != ProjectileType.Penetrate)
        {
            Destroy(gameObject);
        }
    }



    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (type == ProjectileType.Penetrate && collision.CompareTag("Enemy"))
        {
            Monster m = collision.GetComponent<Monster>();
            if (m != null) HitTarget(m);
        }
    }

}