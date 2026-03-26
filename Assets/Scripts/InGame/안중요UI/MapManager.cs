
using UnityEngine;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    [Header("타일 프리팹을 넣어주세요")]
    public GameObject pathTilePrefab;  // 길 (ㅇ)
    public GameObject buildTilePrefab; // 배치칸 (ㅁ)

    [Header("타일 크기 간격")]
    public float tileSize = 0.9f; // 타일 사이의 간격.

    public List<Transform> buildTiles = new List<Transform>(); // 유닛을 배치 가능한 타일들
    public List<Vector2> allTilePositions = new List<Vector2>();

    // 기획해주신 6x7 맵 구조 (0: 길, 1: 배치칸)
    private int[,] mapData = new int[7, 6] {
        { 0, 0, 0, 0, 0, 0 }, // 0번 줄 (맨 위)
        { 0, 1, 1, 1, 1, 0 }, // 1번 줄
        { 0, 1, 1, 1, 1, 0 }, // 2번 줄
        { 0, 0, 0, 0, 0, 0 }, // 3번 줄 (중간 가로지르는 길)
        { 0, 1, 1, 1, 1, 0 }, // 4번 줄
        { 0, 1, 1, 1, 1, 0 }, // 5번 줄
        { 0, 0, 0, 0, 0, 0 }  // 6번 줄 (맨 아래)
    };

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        allTilePositions.Clear();
        buildTiles.Clear();
        // 맵이 화면 중앙에 예쁘게 오도록 시작점(좌상단) 좌표를 잡아줍니다.
        float startX = -2.5f * tileSize;
        float startY = 3.0f * tileSize;

        for (int y = 0; y < 7; y++)
        {
            for (int x = 0; x < 6; x++)
            {
                // 타일이 놓일 위치 계산 (y가 커질수록 아래로(- 방향) 내려감)
                Vector2 position = new Vector2(startX + (x * tileSize), startY - (y * tileSize));

                // 생성된 타일을 정리하기 위해 MapManager의 자식으로 둡니다.
                GameObject spawnedTile = null;

                if (mapData[y, x] == 0) //길칸
                {
                    spawnedTile = Instantiate(pathTilePrefab, position, Quaternion.identity, transform);
                    allTilePositions.Add(position);
                }
                else if (mapData[y, x] == 1) //배치칸
                {
                    spawnedTile = Instantiate(buildTilePrefab, position, Quaternion.identity, transform);
                    buildTiles.Add(spawnedTile.transform);
                }

                // 타일 이름 예쁘게 정리 (예: "Tile_0_0")
                if (spawnedTile != null)
                {
                    spawnedTile.name = $"Tile_{x}_{y}";
                }
            }
        }
    }
}
