using UnityEngine;

public class TideEffect : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 3f;
    private Vector3 moveDirection;

    public void Setup(Vector3 direction)
    {
        moveDirection = direction;
        // 해일이 진행 방향을 바라보게 회전 (필요 시)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifeTime); // 일정 시간 후 자동 삭제
    }

    void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }
}