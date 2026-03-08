using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject monsterPrefab;
    public Transform[] waypoints;
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
        while (spawnedCount < monstersPerRound)
        {
            GameObject monsterObj = Instantiate(monsterPrefab, pathManager.waypoints[0].position, Quaternion.identity);
            // List를 Array로 변환해서 전달
            monsterObj.GetComponent<Monster>().Setup(pathManager.waypoints.ToArray());

            InGameManager.instance.OnMonsterSpawned();
            spawnedCount++;
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}