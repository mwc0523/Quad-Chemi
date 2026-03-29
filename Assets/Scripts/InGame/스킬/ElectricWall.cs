using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ElectricWall : MonoBehaviour
{
    private float duration;
    private float range;
    private List<Monster> trappedMonsters = new List<Monster>();
    private bool isApplyingAmp = false;

    public void Init(float duration, float range)
    {
        this.duration = duration;
        this.range = range;

        var col = GetComponent<BoxCollider2D>();
        if (col != null) col.size = new Vector2(range * 2f, range * 2f);

        // [라그나로크의 창] 카드 효과 체크 및 코루틴 시작
        if (CardUIManager.instance.HasCard(CardEffectID.Myth_SpearOfRagnarok))
        {
            StartCoroutine(DamageAmpRoutine());
        }

        Destroy(gameObject, duration);
    }

    // 벽 유지 시간 동안 범위 내 적들에게 피해 증가 디버프를 지속적으로 거는 코루틴
    IEnumerator DamageAmpRoutine()
    {
        isApplyingAmp = true;

        while (true) // Destroy(gameObject)에 의해 객체가 파괴되면 코루틴도 자동 종료됨
        {
            // 1. 현재 벽 범위 내의 적들을 다시 탐색 (trappedMonsters 활용)
            for (int i = trappedMonsters.Count - 1; i >= 0; i--)
            {
                Monster m = trappedMonsters[i];
                if (m != null)
                {
                    // 2. 몬스터의 ApplyDamageAmp 호출 (0.5f = 50% 증가)
                    // 지속 시간을 아주 짧게(예: 0.5초) 주어, 벽 안에 있는 동안만 유지되게 함
                    // ApplyDamageAmp 내부에서 이미 Coroutine을 덮어쓰므로 중첩(Stack)되지 않음
                    m.ApplyDamageAmp(0.5f, 0.5f);
                }
                else
                {
                    trappedMonsters.RemoveAt(i);
                }
            }

            // 0.2초마다 갱신하여 디버프가 끊기지 않게 유지
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Monster m = other.GetComponent<Monster>();
            if (m != null && !trappedMonsters.Contains(m))
            {
                m.isStunned = true;
                trappedMonsters.Add(m);

                // 들어온 즉시 디버프 1회 적용 (반응성 향상)
                if (CardUIManager.instance.HasCard(CardEffectID.Myth_SpearOfRagnarok))
                {
                    m.ApplyDamageAmp(0.5f, 0.5f);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 벽 밖으로 밀려나거나 나가는 적 처리
        if (other.CompareTag("Enemy"))
        {
            Monster m = other.GetComponent<Monster>();
            if (m != null && trappedMonsters.Contains(m))
            {
                m.isStunned = false;
                trappedMonsters.Remove(m);
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var m in trappedMonsters)
        {
            if (m != null)
            {
                m.isStunned = false;
            }
        }
    }
}