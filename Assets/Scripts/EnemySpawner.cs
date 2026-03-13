using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("프리팹 설정")]
    public GameObject monsterPrefab;
    public GameObject miniBossPrefab; // 🌟 중간보스 전용 프리팹
    public GameObject bossPrefab;     // 🌟 보스 전용 프리팹

    [Header("스폰 설정")]
    public PathManager pathManager;
    public float spawnInterval = 0.4f;
    public int monstersPerRound = 30;

    private int spawnedCount = 0;

    public void StartSpawn()
    {
        spawnedCount = 0;
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        int currentRound = InGameManager.instance.currentRound;

        // 1. 10라운드 단위 (진보스 등장)
        if (currentRound % 10 == 0)
        {
            // 보스는 단 1마리만 소환하고, 일반 몹은 나오지 않으므로 여기서 코루틴 종료
            SpawnMonster(bossPrefab, currentRound, MonsterType.Boss);
            yield break;
        }

        // 2. 5라운드 단위 (중간보스 등장)
        // 위에서 % 10 조건에 걸러졌으므로 여기는 5, 15, 25... 라운드만 실행됨
        if (currentRound % 5 == 0)
        {
            SpawnMonster(miniBossPrefab, currentRound, MonsterType.MiniBoss);

            // 중간보스 소환 후 일반 몹이 나오기 전 약간의 딜레이
            yield return new WaitForSeconds(spawnInterval * 2f);
        }

        // 3. 일반 몬스터 소환 (보스 라운드가 아닐 때만 여기까지 코드가 도달함)
        while (spawnedCount < monstersPerRound)
        {
            SpawnMonster(monsterPrefab, currentRound, MonsterType.Normal);
            spawnedCount++;
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // 코드를 깔끔하게 만들기 위한 스폰 전용 헬퍼 함수
    private void SpawnMonster(GameObject prefab, int round, MonsterType type)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"{type} 프리팹이 할당되지 않았습니다!");
            return;
        }

        GameObject monsterObj = Instantiate(prefab, pathManager.waypoints[0].position, Quaternion.identity);

        // 저번에 수정한 MonsterType 인자 넘겨주기
        monsterObj.GetComponent<Monster>().Setup(pathManager.waypoints.ToArray(), round, type);

        InGameManager.instance.OnMonsterSpawned();
    }
}