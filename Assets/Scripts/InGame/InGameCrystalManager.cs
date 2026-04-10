using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class InGameCrystalManager : MonoBehaviour
{
    public static InGameCrystalManager Instance;

    [Header("Final Buff Values")]
    // 변수명을 로직과 일치하도록 수정했습니다.
    public float FinalFireAtk; public float FinalFireExtraDmg;
    public float FinalWaterSlow; public float FinalWaterSkillChance;
    public float FinalEarthStunTime; public float FinalEarthCritDmg;
    public float FinalAirAtkSpeed; public float FinalAirCritChance;

    private Dictionary<int, CrystalPieceData> cellToPieceMap = new Dictionary<int, CrystalPieceData>();
    private CrystalElement[] gridElements = new CrystalElement[25];

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CalculateAllBuffs();
    }

    public void CalculateAllBuffs()
    {
        //Debug.Log("계산 시작 시도!");
        if (DataManager.instance == null) return;

        // 변수 초기화
        FinalFireAtk = 0; FinalFireExtraDmg = 0;
        FinalWaterSlow = 0; FinalWaterSkillChance = 0;
        FinalEarthStunTime = 0; FinalEarthCritDmg = 0;
        FinalAirAtkSpeed = 0; FinalAirCritChance = 0;

        cellToPieceMap.Clear();
        var inventory = DataManager.instance.currentUser.crystalInventory;
        List<CrystalPieceData> placedPieces = new List<CrystalPieceData>();

        // 1. 그리드 매핑
        for (int i = 0; i < 25; i++) gridElements[i] = CrystalElement.None;

        foreach (var piece in inventory)
        {
            if (!piece.isPlaced) continue;
            List<int> indices = GetIndices(piece);
            foreach (int idx in indices)
            {
                cellToPieceMap[idx] = piece;
                gridElements[idx] = piece.element;
            }
            if (piece.element != CrystalElement.Prism) placedPieces.Add(piece);
        }

        // 2. 프리즘 배율 적용 및 합산
        foreach (var piece in placedPieces)
        {
            float multiplier = 1f;
            HashSet<CrystalPieceData> touchingPrisms = new HashSet<CrystalPieceData>();
            foreach (int myIdx in GetIndices(piece))
            {
                foreach (int neighbor in GetNeighbors(myIdx))
                {
                    if (cellToPieceMap.TryGetValue(neighbor, out var neighborPiece))
                    {
                        if (neighborPiece.element == CrystalElement.Prism)
                            touchingPrisms.Add(neighborPiece);
                    }
                }
            }
            foreach (var prism in touchingPrisms) multiplier *= prism.primaryValue;

            AddToTotal(piece, multiplier);
        }

        // 3. 빙고 보너스 적용
        ApplyBingoBonuses();

        //LogFinalBuffs();
    }

    private void AddToTotal(CrystalPieceData piece, float mult)
    {
        switch (piece.element)
        {
            case CrystalElement.Fire:
                FinalFireAtk += piece.primaryValue * mult;
                FinalFireExtraDmg += piece.secondaryValue * mult; break;
            case CrystalElement.Water:
                FinalWaterSlow += piece.primaryValue * mult;
                FinalWaterSkillChance += piece.secondaryValue * mult; break;
            case CrystalElement.Earth:
                FinalEarthStunTime += piece.primaryValue * mult;
                FinalEarthCritDmg += piece.secondaryValue * mult; break;
            case CrystalElement.Air:
                FinalAirAtkSpeed += piece.primaryValue * mult;
                FinalAirCritChance += piece.secondaryValue * mult; break;
        }
    }

    private void ApplyBingoBonuses()
    {
        // % 단위를 계수(0.01)로 변환하며 빙고 보너스 합산
        FinalFireAtk = (FinalFireAtk / 100f) + CalculateBingoBonus(CrystalElement.Fire, 0.1f, 0.05f, 0.02f);
        FinalFireExtraDmg /= 100f;

        FinalWaterSkillChance = (FinalWaterSkillChance / 100f) + CalculateBingoBonus(CrystalElement.Water, 0.015f, 0.01f, 0.005f);
        FinalWaterSlow /= 100f;

        FinalEarthCritDmg = (FinalEarthCritDmg / 100f) + CalculateBingoBonus(CrystalElement.Earth, 0.2f, 0.1f, 0.04f);
        // 스턴 시간은 Flat 수치이므로 나누지 않음

        FinalAirCritChance = (FinalAirCritChance / 100f) + CalculateBingoBonus(CrystalElement.Air, 0.01f, 0.005f, 0.002f);
        FinalAirAtkSpeed /= 100f;
    }

    private float CalculateBingoBonus(CrystalElement element, float b1, float b2, float b3)
    {
        int lines = CountBingo(element);
        float total = 0f;
        for (int i = 1; i <= lines; i++)
        {
            if (i <= 2) total += b1;
            else if (i <= 4) total += b2;
            else total += b3;
        }
        return total;
    }

    private int CountBingo(CrystalElement target)
    {
        int lines = 0;

        // 1. 가로줄 체크 (5줄)
        for (int r = 0; r < 5; r++)
        {
            bool hasTarget = false;
            bool allMatch = true;
            for (int c = 0; c < 5; c++)
            {
                var el = gridElements[r * 5 + c];
                if (el == target) hasTarget = true;
                if (el != target && el != CrystalElement.Prism) { allMatch = false; break; }
            }
            if (allMatch && hasTarget) lines++;
        }

        // 2. 세로줄 체크 (5줄) - 이 부분이 누락되었을 가능성이 큽니다.
        for (int c = 0; c < 5; c++)
        {
            bool hasTarget = false;
            bool allMatch = true;
            for (int r = 0; r < 5; r++)
            {
                var el = gridElements[r * 5 + c];
                if (el == target) hasTarget = true;
                if (el != target && el != CrystalElement.Prism) { allMatch = false; break; }
            }
            if (allMatch && hasTarget) lines++;
        }

        // 3. 대각선 체크 (2줄) - \ 방향
        {
            bool hasTarget = false;
            bool allMatch = true;
            for (int i = 0; i < 5; i++)
            {
                var el = gridElements[i * 5 + i];
                if (el == target) hasTarget = true;
                if (el != target && el != CrystalElement.Prism) { allMatch = false; break; }
            }
            if (allMatch && hasTarget) lines++;
        }

        // 4. 대각선 체크 - / 방향
        {
            bool hasTarget = false;
            bool allMatch = true;
            for (int i = 0; i < 5; i++)
            {
                var el = gridElements[i * 5 + (4 - i)];
                if (el == target) hasTarget = true;
                if (el != target && el != CrystalElement.Prism) { allMatch = false; break; }
            }
            if (allMatch && hasTarget) lines++;
        }

        return lines;
    }

    private List<int> GetNeighbors(int index)
    {
        List<int> neighbors = new List<int>();
        int r = index / 5; int c = index % 5;
        if (r > 0) neighbors.Add(index - 5);
        if (r < 4) neighbors.Add(index + 5);
        if (c > 0) neighbors.Add(index - 1);
        if (c < 4) neighbors.Add(index + 1);
        return neighbors;
    }

    private List<int> GetIndices(CrystalPieceData piece)
    {
        int[] shape = CrystalDatabase.Shapes[piece.shapeIndex];
        int rootRow = (piece.placedRootIndex / 5) - 1;
        int rootCol = (piece.placedRootIndex % 5) - 1;
        List<int> indices = new List<int>();
        for (int i = 0; i < 16; i++)
        {
            if (shape[i] == 0) continue;
            int tr = rootRow + (i / 4); int tc = rootCol + (i % 4);
            if (tr >= 0 && tr < 5 && tc >= 0 && tc < 5) indices.Add(tr * 5 + tc);
        }
        return indices;
    }
    
    //로비에 보낼 용도
    public string GetLobbyBuffSummary()
    {
        // 1. 먼저 최신 배치 상태로 수치를 다시 계산합니다.
        CalculateAllBuffs();

        StringBuilder sb = new StringBuilder();

        // 2. 각 원소별로 최종 수치가 0보다 큰 경우에만 텍스트를 추가합니다.
        // Fire
        if (FinalFireAtk > 0) sb.AppendLine($"전체 공격력 증가 <color=#FFD700>{FinalFireAtk * 100:F1}%</color>");
        if (FinalFireExtraDmg > 0) sb.AppendLine($"평타 공격마다 적에게 추가 데미지 <color=#FFD700>{FinalFireExtraDmg * 100:F1}%</color>");

        // Water
        if (FinalWaterSlow > 0) sb.AppendLine($"공격 시 0.5초간 적 이동속도 <color=#FFD700>{FinalWaterSlow * 100:F1}%</color> 감소");
        if (FinalWaterSkillChance > 0) sb.AppendLine($"스킬 사용 확률 <color=#FFD700>{FinalWaterSkillChance * 100:F1}%</color> 증가");

        // Earth
        if (FinalEarthStunTime > 0) sb.AppendLine($"모든 스턴 지속 시간 <color=#FFD700>{FinalEarthStunTime:F2}초</color> 증가");
        if (FinalEarthCritDmg > 0) sb.AppendLine($"치명타 피해량 <color=#FFD700>{FinalEarthCritDmg * 100:F1}%</color> 증가");

        // Air
        if (FinalAirAtkSpeed > 0) sb.AppendLine($"전체 공격 속도 <color=#FFD700>{FinalAirAtkSpeed * 100:F1}%</color> 증가");
        if (FinalAirCritChance > 0) sb.AppendLine($"치명타 확률 <color=#FFD700>{FinalAirCritChance * 100:F1}%</color> 증가");

        // 만약 아무런 버프가 없다면
        if (sb.Length == 0) return "배치된 결정이 없습니다.";

        return sb.ToString();
    }

    private void LogFinalBuffs() //로그 찍는 용도의 함수
    {
        Debug.Log("<color=cyan><b>[Crystal System: Final Buffs]</b></color>");

        string fire = $"<color=red>Fire</color> -> Atk: {FinalFireAtk:P1} / ExtraDmg: {FinalFireExtraDmg:P1}";
        string water = $"<color=blue>Water</color> -> Slow: {FinalWaterSlow:P1} / SkillChance: {FinalWaterSkillChance:P1}";
        string earth = $"<color=brown>Earth</color> -> Stun: {FinalEarthStunTime:F2}s / CritDmg: {FinalEarthCritDmg:P1}";
        string air = $"<color=white>Air</color> -> AtkSpeed: {FinalAirAtkSpeed:P1} / CritChance: {FinalAirCritChance:P1}";

        Debug.Log($"{fire}\n{water}\n{earth}\n{air}");
    }
}