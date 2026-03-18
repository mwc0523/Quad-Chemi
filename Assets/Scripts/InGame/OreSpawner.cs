using System.Collections;
using UnityEngine;

public class OreSpawner : MonoBehaviour
{
    [Header("광석 설정")]
    public GameObject orePrefab;
    public Transform spawnPoint; // 맵 우측 하단 경로 타일의 위치

    [Header("스폰 설정")]
    public float respawnDelay = 1.0f; // 파괴 후 재생성까지 걸리는 시간

    private int destroyCount = 0; // 몇 번 부서졌는지 기록 (체력 증가용)

    void Start()
    {
        // 게임 시작과 동시에 광석 스폰 코루틴 실행
        StartCoroutine(OreSpawnRoutine());
    }

    IEnumerator OreSpawnRoutine()
    {
        while (true) // 게임이 끝날 때까지 무한 반복
        {
            // 1. 광석 생성
            GameObject oreObj = Instantiate(orePrefab, spawnPoint.position, Quaternion.identity);
            Monster oreMonster = oreObj.GetComponent<Monster>();

            // 2. 광석 전용 셋업 (파괴된 횟수를 넘겨줘서 체력을 올림)
            if (oreMonster != null)
            {
                oreMonster.SetupOre(destroyCount);
            }

            // 3. 광석이 파괴될 때까지 대기
            // oreMonster 객체가 파괴되어 null이 될 때까지 while문에서 멈춰있습니다.
            while (oreMonster != null && oreMonster.hp > 0)
            {
                yield return null; // 다음 프레임까지 대기
            }

            // 4. 파괴됨! 카운트 증가 및 대기
            destroyCount++;
            yield return new WaitForSeconds(respawnDelay);
        }
    }
}