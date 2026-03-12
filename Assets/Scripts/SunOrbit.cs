using UnityEngine;

public class SunOrbit : MonoBehaviour
{
    private Transform centerUnit;
    private float radius;
    private float damage;
    private float angle;
    private float speed = 100f; // 회전 속도
    private float remainingTime;
    private Unit owner;

    public void Init(Transform center, float r, float dmg, float startAngle, float duration, Unit own)
    {
        centerUnit = center;
        radius = r;
        damage = dmg;
        angle = startAngle;
        remainingTime = duration;
        owner = own;
    }
    public void RefreshDuration(float newDuration)
    {
        remainingTime = newDuration; // 시간을 다시 10초로 리셋
    }

    void Update()
    {
        if (centerUnit == null) { Destroy(gameObject); return; }

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // 반시계 방향 회전 계산
        angle += speed * Time.deltaTime;
        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
        float y = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

        transform.position = centerUnit.position + new Vector3(x, y, 0);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Monster m = collision.GetComponent<Monster>();
            if (m != null) m.TakeDamage(damage, owner);
        }
    }
}