using System.Collections.Generic;
using UnityEngine;

public class TsunamiEntity : MonoBehaviour
{
    private float damage;
    private float speed = 3f;
    private List<Transform> waypoints;
    private int nextWaypointIndex;
    private Unit owner;

    // [추가] 이미 데미지를 입은 적들을 저장하는 목록
    private HashSet<Monster> hitMonsters = new HashSet<Monster>();

    public void Setup(float _damage, float _duration, Unit _owner)
    {
        damage = _damage;
        owner = _owner;

        PathManager pathManager = Object.FindObjectOfType<PathManager>();
        if (pathManager != null)
        {
            waypoints = pathManager.waypoints;
            FindPathReverse();
        }

        Destroy(gameObject, _duration);
    }

    void Update()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        // 1. 이동
        transform.position = Vector3.MoveTowards(transform.position, waypoints[nextWaypointIndex].position, speed * Time.deltaTime);

        // 2. 방향 회전
        Vector3 dir = (waypoints[nextWaypointIndex].position - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        // 3. 웨이포인트 도달 및 순환 로직
        if (Vector3.Distance(transform.position, waypoints[nextWaypointIndex].position) < 0.1f)
        {
            nextWaypointIndex--;

            if (nextWaypointIndex < 0)
            {
                // 순환: 마지막 인덱스로 리셋
                nextWaypointIndex = waypoints.Count - 1;

                // [순환 시 텔레포트] 0번에서 마지막 포인트로 즉시 이동시켜 맵 가로지름 방지
                transform.position = waypoints[nextWaypointIndex].position;

                // [선택 사항] 한 바퀴 다 돌았을 때 다시 데미지를 줄 수 있게 하려면 아래 주석 해제
                // hitMonsters.Clear(); 
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Monster m = collision.GetComponent<Monster>();

            // [중복 체크] 리스트(HashSet)에 없는 적만 데미지를 줌
            if (m != null && !hitMonsters.Contains(m))
            {
                m.TakeDamage(damage, owner);
                hitMonsters.Add(m); // 데미지 준 적은 목록에 추가
            }
        }
    }

    void FindPathReverse()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        float minDistance = Mathf.Infinity;
        int closestIndex = 0;

        for (int i = 0; i < waypoints.Count; i++)
        {
            float dist = Vector3.Distance(transform.position, waypoints[i].position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }
        nextWaypointIndex = closestIndex;
    }
}