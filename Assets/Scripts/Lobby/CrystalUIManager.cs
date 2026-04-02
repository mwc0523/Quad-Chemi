using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public enum CrystalSortType { GradeAsc, GradeDesc, DateAsc, DateDesc } //정렬 기준

public class CrystalUIManager : MonoBehaviour
{
    public static CrystalUIManager Instance;

    [Header("UI Refs")]
    public Transform gridParent;
    public GameObject gridCellPrefab;
    public Transform inventoryContent;
    public GameObject crystalItemPrefab;

    [Header("Preview Settings")]
    public Color validPreviewColor = new Color(0.5f, 1f, 0.5f, 0.6f); // 연두색 (반투명)
    public Color invalidPreviewColor = new Color(1f, 0.5f, 0.5f, 0.6f); // 연한 빨간색 (반투명)

    private List<CrystalCell> allCells = new List<CrystalCell>();

    // 프리뷰 시 원래 타일 색상을 기억하기 위한 딕셔너리
    private Dictionary<int, Color> originalCellColors = new Dictionary<int, Color>();
    private List<int> currentPreviewIndices = new List<int>();

    [Header("Sort Settings")]
    public CrystalSortType currentSortType = CrystalSortType.GradeAsc;

    private CrystalCell currentlyOpenedCell = null;

    void Awake() => Instance = this;

    void Start()
    {
        if (DataManager.instance == null) return;

        // 1. 이미 데이터 로드가 끝났다면 즉시 UI 생성 (씬 이동 시 등)
        if (DataManager.instance.isDataLoaded)
        {
            InitUI();
        }
        // 2. 아직 로드 중이라면, 완료 이벤트(OnDataLoaded)를 기다렸다가 생성
        else
        {
            DataManager.instance.OnDataLoaded += InitUI;
        }
    }

    // 기존 Start()에 있던 생성 로직을 묶어둔 함수
    private void InitUI()
    {
        GenerateGrid();
        UpdateGridStatus();
        RefreshInventory();
    }

    void GenerateGrid()
    {
        foreach (Transform child in gridParent) Destroy(child.gameObject);
        allCells.Clear();

        for (int i = 0; i < 25; i++)
        {
            GameObject cellObj = Instantiate(gridCellPrefab, gridParent);
            CrystalCell cell = cellObj.GetComponent<CrystalCell>();
            cell.cellIndex = i;

            cell.isUnlocked = DataManager.instance.currentUser.unlockedCrystalGridIndices.Contains(i);

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

        // ❗ 마우스가 가리키는 곳을 4x4의 (1,1) 지점으로 설정 -> 전체 필드가 왼쪽 위로 한 칸 이동함
        int rootRow = (root / 5) - 1;
        int rootCol = (root % 5) - 1;

        for (int i = 0; i < 16; i++)
        {
            if (shape[i] == 0) continue;

            int targetRow = rootRow + (i / 4);
            int targetCol = rootCol + (i % 4);

            // 그리드 5x5 범위 체크
            if (targetRow < 0 || targetRow >= 5 || targetCol < 0 || targetCol >= 5)
                return null;

            indices.Add(targetRow * 5 + targetCol);
        }
        return indices;
    }

    public Vector3 GetCellPosition(int index)
    {
        if (index >= 0 && index < allCells.Count) return allCells[index].transform.position;
        return Vector3.zero;
    }

    public void PlacePiece(CrystalPieceData piece, int rootIndex)
    {
        if (!CanPlacePiece(piece, rootIndex)) return;

        piece.isPlaced = true;
        piece.placedRootIndex = rootIndex;
        UpdateGridStatus();
        DataManager.instance.SaveData();
    }

    public void UpdateGridStatus()
    {
        // 1. 모든 셀 상태 및 색상 초기화
        foreach (var cell in allCells)
        {
            cell.SetOccupied(false);
            // GetComponent<Image>()를 지우고, 스크립트에 연결된 cell.cellImage를 직접 사용합니다.
            if (cell.cellImage != null)
            {
                cell.cellImage.color = cell.isUnlocked ? Color.white : Color.gray;
            }
        }

        // 2. 배치된 피스들을 순회하며 타일 색칠
        foreach (var piece in DataManager.instance.currentUser.crystalInventory)
        {
            if (piece.isPlaced)
            {
                int[] shape = CrystalDatabase.Shapes[piece.shapeIndex];
                List<int> indices = GetRequiredIndices(shape, piece.placedRootIndex);

                if (indices != null)
                {
                    Color pieceColor = CrystalPieceUI.GetElementColor(piece.element);
                    foreach (int idx in indices)
                    {
                        allCells[idx].SetOccupied(true);
                        // 여기도 GetComponent<Image>() 대신 cellImage 사용
                        if (allCells[idx].cellImage != null)
                        {
                            allCells[idx].cellImage.color = pieceColor;
                        }
                    }
                }
            }
        }
    }

    public void ShowPreview(CrystalPieceData piece, int rootIndex)
    {
        ClearPreview();

        int[] shape = CrystalDatabase.Shapes[piece.shapeIndex];
        List<int> requiredIndices = GetRequiredIndices(shape, rootIndex); // 보정된 인덱스들 가져오기

        bool canPlace = CanPlacePiece(piece, rootIndex);
        Color previewColor = canPlace ? validPreviewColor : invalidPreviewColor;

        if (requiredIndices != null)
        {
            foreach (int targetIndex in requiredIndices)
            {
                Image cellImage = allCells[targetIndex].cellImage;
                if (cellImage != null)
                {
                    if (!originalCellColors.ContainsKey(targetIndex))
                        originalCellColors[targetIndex] = cellImage.color;

                    cellImage.color = previewColor;
                    currentPreviewIndices.Add(targetIndex);
                }
            }
        }
    }

    public void ClearPreview()
    {
        foreach (int idx in currentPreviewIndices)
        {
            Image cellImage = allCells[idx].cellImage;
            if (cellImage != null && originalCellColors.ContainsKey(idx))
            {
                cellImage.color = originalCellColors[idx]; // 원래 색상으로 복구
            }
        }
        currentPreviewIndices.Clear();
        originalCellColors.Clear();
    }

    public CrystalPieceData GetPieceAtCell(int cellIndex)
    {
        foreach (var piece in DataManager.instance.currentUser.crystalInventory)
        {
            if (!piece.isPlaced) continue;

            int[] shape = CrystalDatabase.Shapes[piece.shapeIndex];
            List<int> indices = GetRequiredIndices(shape, piece.placedRootIndex);

            if (indices != null)
            {
                //Debug.Log($"배치된 피스 검사 중: 루트 {piece.placedRootIndex}, 차지하는 칸 수: {indices.Count}");
                if (indices.Contains(cellIndex)) return piece;
            }
        }
        return null;
    }

    // 2. 크리스탈을 그리드에서 제거하고 인벤토리로 돌려보냅니다.
    public void RemovePiece(CrystalPieceData piece)
    {
        piece.isPlaced = false;
        piece.placedRootIndex = -1;

        UpdateGridStatus();    // 그리드 색상 갱신
        RefreshInventory();    // 인벤토리 아이템 생성
        DataManager.instance.SaveData();
    }
    // ----------------------------------------

    public void RegisterOpenedCell(CrystalCell newCell)
    {
        if (currentlyOpenedCell != null && currentlyOpenedCell != newCell)
        {
            currentlyOpenedCell.CloseUnlockButton();
        }
        currentlyOpenedCell = newCell;
    }

    // 3. 가격 상승 공식 계산
    public int GetCurrentUnlockPrice()
    {
        int unlockedCount = DataManager.instance.currentUser.unlockedCrystalGridIndices.Count;
        int baseFreeCells = 6; // 처음에 제공되는 무료 칸 갯수
        int purchasedCount = Mathf.Max(0, unlockedCount - baseFreeCells);

        // 예시: 100 에테르부터 시작해서 한 칸 열 때마다 50 에테르씩 증가
        // 기획에 맞게 숫자를 조절하세요!
        return 100 + (purchasedCount * 50);
    }

    // 3. 재화 검사 및 칸 오픈 서버 저장
    public bool TryUnlockCell(int cellIndex)
    {
        int price = GetCurrentUnlockPrice();

        if (DataManager.instance.currentUser.aether >= price)
        {
            // 1. 재화 차감
            DataManager.instance.currentUser.aether -= price;

            // 2. 해금 인덱스 추가
            DataManager.instance.currentUser.unlockedCrystalGridIndices.Add(cellIndex);

            // 3. 타일 상태 업데이트
            allCells[cellIndex].SetUnlock(true);
            UpdateGridStatus();

            // 4. 서버 저장
            DataManager.instance.SaveData();
            UIManager.instance.RefreshTopBar();
            Debug.Log($"[{cellIndex}]번 칸 해금 완료! 남은 에테르: {DataManager.instance.currentUser.aether}");
            return true;
        }
        else
        {
            Debug.Log("에테르가 부족합니다.");
            // TODO: 에테르 부족 팝업을 띄우는 로직이 있다면 여기에 추가
            return false;
        }
    }

    public void OnSortChanged(int index)
    {
        currentSortType = (CrystalSortType)index;
        RefreshInventory();
    }

    public void RefreshInventory()
    {
        foreach (Transform child in inventoryContent) Destroy(child.gameObject);

        // 1. 현재 인벤토리 데이터 복사 (원본 데이터 보존을 위해)
        List<CrystalPieceData> sortedList = new List<CrystalPieceData>(DataManager.instance.currentUser.crystalInventory);

        // 2. 정렬 로직 적용
        sortedList.Sort((a, b) =>
        {
            switch (currentSortType)
            {
                case CrystalSortType.GradeAsc: // 등급 낮은 순
                    return a.grade.CompareTo(b.grade);
                case CrystalSortType.GradeDesc: // 등급 높은 순
                    return b.grade.CompareTo(a.grade);
                case CrystalSortType.DateAsc: // 획득 오래된 순
                    return a.acquisitionTick.CompareTo(b.acquisitionTick);
                case CrystalSortType.DateDesc: // 획득 최신순
                    return b.acquisitionTick.CompareTo(a.acquisitionTick);
                default: return 0;
            }
        });

        // 3. 정렬된 리스트로 UI 생성
        foreach (var data in sortedList)
        {
            if (!data.isPlaced)
            {
                GameObject obj = Instantiate(crystalItemPrefab, inventoryContent);
                CrystalPieceUI ui = obj.GetComponent<CrystalPieceUI>();
                ui.SetData(data);
            }
        }
    }

    public void AddRandomCrystalTest()
{
    if (DataManager.instance == null || DataManager.instance.currentUser == null) return;

    // 1. 원소 랜덤 선택
    CrystalElement randomElement = (CrystalElement)Random.Range(1, 6);
    
    // 2. 데이터베이스의 확률 로직을 사용하여 랜덤 모양(인덱스) 결정
    int randomShapeIndex = CrystalDatabase.GetRandomShapeIndex();
    
    // 3. 모양 인덱스를 통해 해당 등급(Grade) 역추적 (DataManager에 저장하기 위함)
    CrystalGrade grade = GetGradeFromIndex(randomShapeIndex);
    CrystalPieceData newPiece = new CrystalPieceData(randomShapeIndex, randomElement, grade);
    DataManager.instance.currentUser.crystalInventory.Add(newPiece);
    DataManager.instance.SaveData();
    RefreshInventory();
}

// 인덱스로 등급을 찾는 헬퍼 함수
private CrystalGrade GetGradeFromIndex(int index)
{
    if (index <= 9) return CrystalGrade.Common;
    if (index <= 17) return CrystalGrade.Rare;
    if (index <= 23) return CrystalGrade.Unique;
    if (index <= 27) return CrystalGrade.Epic;
    if (index <= 29) return CrystalGrade.Legendary;
    return CrystalGrade.Mythic;
}
}