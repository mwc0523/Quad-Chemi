using UnityEngine;
using System.Collections.Generic;

public class ElectricWall : MonoBehaviour
{
    private float duration;
    // 벽 안에 들어와서 멈춘 몬스터들을 추적하기 위한 리스트
    private List<Monster> trappedMonsters = new List<Monster>();

    public void Init(float duration, float range)
    {
        this.duration = duration;
        // 콜라이더 크기를 range에 맞게 조정 (1칸 범위면 약 1.0f~1.5f)
        var col = GetComponent<BoxCollider2D>();
        if (col != null) col.size = new Vector2(range * 2f, range * 2f);

        Destroy(gameObject, duration);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Monster m = other.GetComponent<Monster>();
            if (m != null)
            {
                // 몬스터의 기존 Stun 로직을 활용해 이동을 멈춤
                m.isStunned = true;
                trappedMonsters.Add(m);
            }
        }
    }

    private void OnDestroy()
    {
        // 벽이 사라질 때 멈춰있던 모든 몬스터를 해제
        foreach (var m in trappedMonsters)
        {
            if (m != null) m.isStunned = false;
        }
    }
}