using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChainLightning : MonoBehaviour
{
    private LineRenderer line;
    private float damage;
    private int remainingBounces;
    private float bounceRange;
    private List<Transform> hitTargets = new List<Transform>();

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        // 인스펙터 설정을 무시하고 코드로 확실히 세팅
        line.positionCount = 0;
        line.useWorldSpace = true;
    }

    public void Setup(Transform firstTarget, float _damage, int bounces, float range)
    {
        damage = _damage;
        remainingBounces = bounces;
        bounceRange = range == 0 ? 3f : range; // 범위가 0이면 기본 3으로 설정

        if (firstTarget != null)
        {
            // 시작점(내 위치) 추가
            line.positionCount = 1;
            line.SetPosition(0, transform.position);

            StartCoroutine(ChainRoutine(firstTarget));
        }
        else
        {
            Destroy(gameObject);
        }
    }

    IEnumerator ChainRoutine(Transform target)
    {
        // 타겟이 죽었는지 체크
        if (target == null || remainingBounces < 0)
        {
            Finish();
            yield break;
        }

        // 1. 현재 타겟 정보 저장 (위치값으로 저장해야 안전함)
        Vector3 targetPos = target.position;
        Monster m = target.GetComponent<Monster>();
        if (m != null) m.TakeDamage(damage);
        hitTargets.Add(target);

        // 2. 라인 그리기
        line.positionCount++;
        line.SetPosition(line.positionCount - 1, targetPos);

        // 3. 다음 적 찾기
        remainingBounces--;
        yield return new WaitForSeconds(0.05f); // 번개가 튀는 연출 대기

        Transform nextTarget = FindNextTarget(targetPos);

        if (nextTarget != null)
        {
            // 중요: target.position 대신 미리 저장한 targetPos를 넘김
            StartCoroutine(ChainRoutine(nextTarget));
        }
        else
        {
            Finish();
        }
    }

    void Finish()
    {
        // 0.2초 뒤에 선을 지우고 객체 파괴
        Destroy(gameObject, 0.2f);
    }

    Transform FindNextTarget(Vector3 currentPos)
    {
        // 몬스터 레이어가 "Enemy"가 맞는지 확인하세요
        Collider2D[] hits = Physics2D.OverlapCircleAll(currentPos, bounceRange, LayerMask.GetMask("Enemy"));
        Transform bestTarget = null;
        float minDistance = Mathf.Infinity;

        foreach (var hit in hits)
        {
            // 이미 맞은 적 제외 및 파괴된 객체 제외
            if (hit == null || hitTargets.Contains(hit.transform)) continue;

            float dist = Vector3.Distance(currentPos, hit.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestTarget = hit.transform;
            }
        }
        return bestTarget;
    }
}