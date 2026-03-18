using System.Collections.Generic;
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
    private Unit owner;
    private HashSet<int> hitEnemies = new HashSet<int>();

    // 셋업 함수 (유닛에서 호출)
    public void Setup(Transform _target, float _damage, ProjectileType _type, Unit _owner)
    {
        target = _target;
        damage = _damage;
        type = _type;
        owner = _owner;

        if (target != null)
        {
            LookAtTarget(target.position);
        }

        if (type == ProjectileType.Penetrate)
        {
            Destroy(gameObject, 3f);
        }
    }

    private void LookAtTarget(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        // 스프라이트가 오른쪽(Right)을 바라보고 있다는 가정하에 회전
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void Update()
    {
        if (type == ProjectileType.Penetrate)
        {
            // [수정] 휘지 않고 직선으로 날아가게 함 (LookAtTarget을 여기서 안 하면 직선 유지)
            transform.Translate(Vector3.up * speed * Time.deltaTime, Space.Self);
        }
        else if (target != null)
        {
            // [추가] 일반 유도탄/평타도 실시간으로 적을 바라보게 함
            LookAtTarget(target.position);

            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.1f)
            {
                HitTarget(target.GetComponent<Monster>());
            }
        }
        else
        {
            // 타겟이 사라졌을 때: 관통탄은 계속 가고, 일반탄은 파괴
            if (type == ProjectileType.Penetrate)
                transform.Translate(Vector3.up * speed * Time.deltaTime, Space.Self);
            else
                Destroy(gameObject);
        }
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
                    targetMonster.TakeDamage(damage, owner);

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
            m.TakeDamage(damage, owner);
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
            int instanceID = collision.gameObject.GetInstanceID();

            if (m != null && !hitEnemies.Contains(instanceID))
            {
                hitEnemies.Add(instanceID); // 리스트에 추가
                HitTarget(m);
            }
        }
    }
}