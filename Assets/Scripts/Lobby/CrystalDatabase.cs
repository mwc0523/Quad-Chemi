using System.Collections.Generic;
using UnityEngine;

public enum CrystalGrade { Common, Rare, Unique, Epic, Legendary, Mythic }

public static class CrystalDatabase
{
    // 0~3: 1행, 4~7: 2행, 8~11: 3행, 12~15: 4행 (4x4 구조)
    public static readonly Dictionary<int, int[]> Shapes = new Dictionary<int, int[]>
    {
        // --- 하급 (Common) 10종 (0~9) ---
        { 0, new int[] { 0,0,0,0, 0,1,1,1, 0,1,1,1, 0,0,0,0 } },
        { 1, new int[] { 0,0,0,0, 1,1,1,1, 0,0,1,1, 0,0,0,0 } },
        { 2, new int[] { 0,0,0,0, 0,1,1,1, 0,1,0,1, 0,0,0,1 } },
        { 3, new int[] { 0,0,0,0, 1,1,1,1, 1,0,0,1, 0,0,0,0 } },
        { 4, new int[] { 0,0,0,0, 1,1,0,0, 0,1,1,0, 0,0,1,1 } },
        { 5, new int[] { 0,0,0,0, 0,0,1,0, 1,1,0,0, 1,1,1,0 } },
        { 6, new int[] { 0,0,0,0, 1,0,0,0, 0,1,0,0, 1,1,1,1 } },
        { 7, new int[] { 0,0,0,0, 1,1,0,0, 1,0,1,0, 0,1,1,0 } },
        { 8, new int[] { 0,0,0,0, 1,1,0,0, 0,1,1,0, 0,1,1,0 } },
        { 9, new int[] { 0,0,0,0, 0,0,1,0, 0,1,1,1, 0,1,0,1 } },

        // --- 중급 (Rare) 8종 (10~17) ---
        { 10, new int[] { 0,0,0,0, 1,1,1,1, 0,0,0,1, 0,0,0,0 } },
        { 11, new int[] { 0,0,0,0, 0,1,1,1, 0,1,0,1, 0,0,0,0 } },
        { 12, new int[] { 0,0,0,0, 0,1,1,1, 0,0,0,1, 0,0,0,1 } },
        { 13, new int[] { 0,0,1,0, 0,1,0,1, 1,0,0,1, 0,0,0,0 } },
        { 14, new int[] { 0,0,1,0, 0,1,1,0, 1,0,1,0, 0,0,0,0 } },
        { 15, new int[] { 0,0,0,0, 0,1,1,0, 0,1,0,1, 0,0,1,0 } },
        { 16, new int[] { 0,0,0,0, 1,1,1,0, 0,0,1,1, 0,0,0,0 } },
        { 17, new int[] { 0,0,0,0, 0,1,1,1, 0,0,1,1, 0,0,0,0 } },

        // --- 상급 (Unique) 6종 (18~23) ---
        { 18, new int[] { 0,0,0,0, 0,1,0,0, 0,1,1,0, 0,0,1,0 } },
        { 19, new int[] { 0,0,0,0, 1,1,1,1, 0,0,0,0, 0,0,0,0 } },
        { 20, new int[] { 0,0,0,0, 0,1,1,1, 0,0,0,1, 0,0,0,0 } },
        { 21, new int[] { 0,0,0,0, 0,0,1,1, 1,1,0,0, 0,0,0,0 } },
        { 22, new int[] { 1,1,0,0, 0,0,1,0, 0,0,0,1, 0,0,0,0 } },
        { 23, new int[] { 0,0,0,0, 0,1,1,0, 0,1,1,0, 0,0,0,0 } },

        // --- 서사급 (Epic) 4종 (24~27) ---
        { 24, new int[] { 0,0,0,0, 0,1,0,0, 0,1,1,0, 0,0,0,0 } },
        { 25, new int[] { 0,0,0,0, 0,1,1,1, 0,0,0,0, 0,0,0,0 } },
        { 26, new int[] { 0,0,0,0, 0,1,0,0, 0,0,1,0, 0,1,0,0 } },
        { 27, new int[] { 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,0 } },

        // --- 전설급 (Legendary) 2종 (28~29) ---
        { 28, new int[] { 0,0,0,0, 0,1,0,0, 0,1,0,0, 0,0,0,0 } },
        { 29, new int[] { 0,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,0 } },

        // --- 신화급 (Mythic) 1종 (30) ---
        { 30, new int[] { 0,0,0,0, 0,1,0,0, 0,0,0,0, 0,0,0,0 } }
    };

    // 등급별 가중치 (전체 합 1000 = 100%)
    public static readonly Dictionary<CrystalGrade, int> GradeWeights = new Dictionary<CrystalGrade, int>
    {
        { CrystalGrade.Common, 560 },   // 56%
        { CrystalGrade.Rare, 300 },     // 30%
        { CrystalGrade.Unique, 100 },   // 10%
        { CrystalGrade.Epic, 25 },      // 2.5%
        { CrystalGrade.Legendary, 10 }, // 1%
        { CrystalGrade.Mythic, 5 }      // 0.5%
    };

    // 등급별 인덱스 범위 정보
    public static (int start, int end) GetIndexRange(CrystalGrade grade)
    {
        return grade switch
        {
            CrystalGrade.Common => (0, 9),
            CrystalGrade.Rare => (10, 17),
            CrystalGrade.Unique => (18, 23),
            CrystalGrade.Epic => (24, 27),
            CrystalGrade.Legendary => (28, 29),
            CrystalGrade.Mythic => (30, 30),
            _ => (0, 0)
        };
    }

    // 랜덤 조각 생성 로직 (가챠)
    public static int GetRandomShapeIndex()
    {
        int roll = Random.Range(0, 1000);
        int currentWeight = 0;

        foreach (var weight in GradeWeights)
        {
            currentWeight += weight.Value;
            if (roll < currentWeight)
            {
                var range = GetIndexRange(weight.Key);
                return Random.Range(range.start, range.end + 1);
            }
        }
        return 0;
    }

    public static CrystalGrade GetNextGrade(CrystalGrade current)
    {
        if (current == CrystalGrade.Mythic) return CrystalGrade.Mythic;
        return (CrystalGrade)((int)current + 1);
    }
}