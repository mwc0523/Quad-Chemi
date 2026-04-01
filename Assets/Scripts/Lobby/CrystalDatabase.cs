using System.Collections.Generic;


public static class CrystalDatabase
{
    // 0~3: 1행, 4~7: 2행 ... (4x4 구조)
    public static readonly Dictionary<int, int[]> Shapes = new Dictionary<int, int[]>
    {
        // 하급 예시 (0번)
        { 0, new int[] { 0,0,0,0,
                         0,1,1,1,
                         0,1,1,1,
                         0,0,0,0 } },
        // 신화급 (1칸짜리 예시)
        { 25, new int[] { 0,0,0,0,
                          0,1,0,0,
                          0,0,0,0,
                          0,0,0,0 } }
    };

    // 등급별 가중치 및 조각 모양 인덱스 범위 등을 여기서 관리할 수 있습니다.
}