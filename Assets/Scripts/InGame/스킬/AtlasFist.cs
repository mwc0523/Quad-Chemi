using UnityEngine;

public class AtlasFist : MonoBehaviour
{
    private Animator anim;
    private Transform target;
    private float damage;
    private float speed = 5f;
    private float hitRadius = 0.5f; // 주먹의 타격 판정 범위
    private Unit owner;
    private bool hasHit = false;

    void Awake()
    {
        // 시작하자마자 애니메이터를 가져옵니다.
        anim = GetComponent<Animator>();
    }

    public void Setup(Transform _target, float _damage, Unit _owner, bool isLeft)
    {
        target = _target;
        damage = _damage;
        owner = _owner;

        // 시각적 구분: 왼손/오른손에 따라 약간의 Y축 오프셋을 줄 수 있습니다.
        Vector3 offset = isLeft ? transform.right * -0.2f : transform.right * 0.2f;
        transform.position += offset;

        if (anim != null)
        {
            // 애니메이터의 isLeft 파라미터를 설정해 왼손/오른손 애니메이션을 틀어줍니다.
            anim.SetBool("isLeft", isLeft);
        }

        // 적을 바라보게 회전
        if (target != null)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle-90f, Vector3.forward);
        }
    }

    void Update()
    {
        if (target == null || hasHit)
        {
            Destroy(gameObject, 0.1f);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            Hit();
        }
    }

    void Hit()
    {
        hasHit = true;

        // 제안하신 OverlapCircleAll 방식 데미지 처리
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            Monster m = hit.GetComponent<Monster>();
            if (m != null)
            {
                // 양손을 번갈아 날리므로, 한 대당 데미지는 적절히 조절 (예: 전체 데미지의 50%)
                m.TakeDamage(damage, owner);
            }
        }

        // TODO: 여기서 주먹 타격 이펙트(먼지 등)를 소환하면 더 좋습니다!
        Destroy(gameObject);
    }
}