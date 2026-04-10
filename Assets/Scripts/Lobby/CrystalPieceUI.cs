using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CrystalPieceUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public CrystalPieceData pieceData;

    [Header("UI Components")]
    public Image backgroundImage;
    public Image[] shapeImages;
    public CanvasGroup canvasGroup;

    [Header("Drag Settings")]
    public float dragScale = 4.2f; // 드래그 시 커질 배율 (인스펙터에서 타일 크기에 맞게 조절하세요)

    private Transform originalParent;
    private Vector3 originalPosition;
    private RectTransform rectTransform;
    private int currentHoverCellIndex = -1; // 최적화를 위한 현재 마우스 위치 캐싱
    private Vector2 originalSize;
    private Vector2 dragOffset;
    private bool isDragging = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetData(CrystalPieceData data)
    {
        pieceData = data;
        if (!CrystalDatabase.Shapes.ContainsKey(data.shapeIndex)) return;

        // 1. 배경색 설정 (생략...)
        backgroundImage.color = GetColorByGrade(data.grade);

        // 2. 모양 이미지 설정 (회전 로직 적용)
        int[] originalShape = CrystalDatabase.Shapes[data.shapeIndex];
        int[] rotatedShape = GetRotatedShape(originalShape, data.rotationCount);
        Color elementColor = GetElementColor(data.element);

        for (int i = 0; i < 16; i++)
        {
            if (i < shapeImages.Length)
            {
                shapeImages[i].enabled = (rotatedShape[i] == 1);
                if (shapeImages[i].enabled)
                {
                    shapeImages[i].color = elementColor;
                }
            }
        }
    }

    public static int[] GetRotatedShape(int[] original, int rotationCount)
    {
        if (rotationCount == 0) return original;

        int[] current = (int[])original.Clone();
        int[] next = new int[16];

        for (int r = 0; r < rotationCount; r++)
        {
            for (int i = 0; i < 16; i++)
            {
                int row = i / 4;
                int col = i % 4;
                // 90도 회전 공식
                int nextIndex = col * 4 + (3 - row);
                next[nextIndex] = current[i];
            }
            current = (int[])next.Clone();
        }
        return current;
    }

    private Color GetColorByGrade(CrystalGrade grade)
    {
        return grade switch
        {
            CrystalGrade.Low => new Color(0.85f, 0.85f, 0.85f),    // 하급: 회색
            CrystalGrade.Middle => new Color(0.55f, 0.85f, 0.55f),      // 중급: 연두
            CrystalGrade.High => new Color(0.5f, 0.75f, 1f),        // 상급: 하늘
            CrystalGrade.Epic => new Color(0.75f, 0.5f, 0.95f),      // 서사급: 보라
            CrystalGrade.Legend => new Color(1f, 0.85f, 0.3f),    // 전설급: 골드
            CrystalGrade.Myth => new Color(1f, 0.5f, 0.5f),       // 신화급: 다홍
            _ => Color.white
        };
    }

    public static Color GetElementColor(CrystalElement element)
    {
        switch (element)
        {
            case CrystalElement.Fire: return Color.red;
            case CrystalElement.Water: return Color.blue;
            case CrystalElement.Earth: return new Color(0.4f, 0.2f, 0f);
            case CrystalElement.Air: return Color.cyan;
            case CrystalElement.Prism: return Color.magenta;
            default: return Color.white;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true; // 드래그 시작됨
        originalParent = transform.parent;
        originalPosition = transform.position;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.8f;
        }

        if (backgroundImage != null) backgroundImage.enabled = false;
        Canvas mainCanvas = GetComponentInParent<Canvas>();

        // 1. 월드 좌표 유지하지 않고(false) 부모 이동 (좌표계 초기화)
        transform.SetParent(mainCanvas.transform, false);
        transform.SetAsLastSibling();

        if (TryGetComponent(out LayoutElement le)) le.ignoreLayout = true;

        // 2. 피벗과 앵커를 중앙으로 강제 초기화
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        // 3. 스케일 적용
        transform.localScale = Vector3.one * dragScale;

        // 4. 즉시 마우스 위치로 이동시켜서 튀는 현상 방지
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 마우스 위치를 캔버스 로컬 좌표로 변환
        RectTransform canvasRect = transform.parent as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            // ⭐ 오프셋 없이 마우스 위치에 피벗(중앙)을 바로 박아버립니다.
            // 이렇게 하면 스케일에 상관없이 마우스가 항상 조각의 정중앙을 잡게 됩니다.
            rectTransform.anchoredPosition = localPoint;
        }

        // --- 프리뷰 로직 ---
        CrystalCell hoverCell = GetCellUnderMouse(eventData);
        if (hoverCell != null)
        {
            if (currentHoverCellIndex != hoverCell.cellIndex)
            {
                currentHoverCellIndex = hoverCell.cellIndex;
                CrystalUIManager.Instance.ShowPreview(pieceData, currentHoverCellIndex);
            }
        }
        else
        {
            CrystalUIManager.Instance.ClearPreview();
            currentHoverCellIndex = -1;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Invoke(nameof(ResetDragFlag), 0.1f);
        canvasGroup.alpha = 1.0f;
        CrystalUIManager.Instance.ClearPreview();

        CrystalCell targetCell = GetCellUnderMouse(eventData);

        if (targetCell != null && CrystalUIManager.Instance.CanPlacePiece(pieceData, targetCell.cellIndex))
        {
            // 1. 데이터 상으로 배치 처리
            CrystalUIManager.Instance.PlacePiece(pieceData, targetCell.cellIndex);

            // 2. ❗ 시각적 오브젝트는 제거 (이제 매니저가 그리드 색상을 바꿀 것임)
            Destroy(gameObject);
        }
        else
        {
            ReturnToInventory();
        }
    }

    private void ResetDragFlag()
    {
        isDragging = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging) return;

        // 1. 현재 씬에 합성 패널이 활성화되어 있는지 확인
        // FindFirstObjectByType은 Unity 2021.3+ 권장 방식입니다. 구버전이면 FindObjectOfType 사용.
        CrystalMergePanel mergePanel = Object.FindFirstObjectByType<CrystalMergePanel>(FindObjectsInactive.Exclude);

        if (mergePanel != null && mergePanel.gameObject.activeInHierarchy)
        {
            // 2. 합성 패널이 열려있다면 합성 재료로 등록 시도
            mergePanel.TrySelectMaterial(pieceData);
        }
        else
        {
            // 3. 합성 패널이 없다면 기존처럼 정보 패널 띄우기
            CrystalUIManager.Instance.ShowCrystalInfoPanel(pieceData);
        }
    }

    private CrystalCell GetCellUnderMouse(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            CrystalCell cell = result.gameObject.GetComponent<CrystalCell>();
            if (cell != null) return cell;
        }
        return null;
    }

    private void ReturnToInventory()
    {
        if (TryGetComponent(out LayoutElement le)) le.ignoreLayout = false;
        transform.SetParent(originalParent);
        transform.position = originalPosition;

        if (backgroundImage != null) backgroundImage.enabled = true;
        transform.localScale = Vector3.one;
        canvasGroup.blocksRaycasts = true;
        CrystalUIManager.Instance.RefreshInventory();
    }
}