using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // List 사용을 위해 필요

public class CrystalUIManager : MonoBehaviour
{
    public static CrystalUIManager Instance;

    [Header("UI Refs")]
    public Transform gridParent;
    public GameObject gridCellPrefab;
    public Transform inventoryContent;
    public GameObject crystalItemPrefab;

    private List<CrystalCell> allCells = new List<CrystalCell>();

    void Awake() => Instance = this;

    void Start()
    {
        if (DataManager.instance == null) return;
        GenerateGrid();
        RefreshInventory();
    }

    void GenerateGrid()
    {
        // 기존 칸이 있다면 청소
        foreach (Transform child in gridParent) Destroy(child.gameObject);
        allCells.Clear();

        for (int i = 0; i < 25; i++)
        {
            GameObject cellObj = Instantiate(gridCellPrefab, gridParent);
            CrystalCell cell = cellObj.GetComponent<CrystalCell>();
            cell.cellIndex = i;

            bool isUnlocked = DataManager.instance.currentUser.unlockedCrystalGridIndices.Contains(i);
            cell.SetUnlock(isUnlocked);

            allCells.Add(cell);
        }
    }

    public bool CanPlacePiece(CrystalPieceData piece, int rootIndex)
    {
        if (!CrystalDatabase.Shapes.ContainsKey(piece.shapeIndex)) return false;

        int[] shape = CrystalDatabase.Shapes[piece.shapeIndex];
        List<int> requiredIndices = GetRequiredIndices(shape, rootIndex);

        if (requiredIndices == null) return false;

        foreach (int idx in requiredIndices)
        {
            if (idx < 0 || idx >= allCells.Count) return false;
            if (!allCells[idx].isUnlocked) return false;
            if (allCells[idx].isOccupied) return false;
        }
        return true;
    }

    private List<int> GetRequiredIndices(int[] shape, int root)
    {
        List<int> indices = new List<int>();
        int rootRow = root / 5;
        int rootCol = root % 5;

        for (int i = 0; i < 16; i++)
        {
            if (shape[i] == 0) continue;

            int shapeRow = i / 4;
            int shapeCol = i % 4;

            int targetRow = rootRow + shapeRow;
            int targetCol = rootCol + shapeCol;

            if (targetRow >= 5 || targetCol >= 5) return null;
            indices.Add(targetRow * 5 + targetCol);
        }
        return indices;
    }

    public void PlacePiece(CrystalPieceData piece, int rootIndex)
    {
        if (!CanPlacePiece(piece, rootIndex)) return;

        piece.isPlaced = true;
        piece.placedRootIndex = rootIndex;

        UpdateGridStatus(); // 배치 상태 업데이트
        DataManager.instance.SaveData();
    }

    // 그리드의 점유 상태를 데이터에 맞춰 동기화
    public void UpdateGridStatus()
    {
        foreach (var cell in allCells) cell.SetOccupied(false);

        foreach (var piece in DataManager.instance.currentUser.crystalInventory)
        {
            if (piece.isPlaced)
            {
                int[] shape = CrystalDatabase.Shapes[piece.shapeIndex];
                List<int> indices = GetRequiredIndices(shape, piece.placedRootIndex);
                if (indices != null)
                {
                    foreach (int idx in indices) allCells[idx].SetOccupied(true);
                }
            }
        }
    }

    public void RefreshInventory()
    {
        // 기존 아이템들 삭제
        foreach (Transform child in inventoryContent) Destroy(child.gameObject);

        foreach (var data in DataManager.instance.currentUser.crystalInventory)
        {
            // 배치되지 않은 조각만 인벤토리에 표시
            if (!data.isPlaced)
            {
                GameObject obj = Instantiate(crystalItemPrefab, inventoryContent);
                CrystalPieceUI ui = obj.GetComponent<CrystalPieceUI>();
                ui.SetData(data);
            }
        }
    }
}