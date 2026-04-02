using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CrystalPieceUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
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

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetData(CrystalPieceData data)
    {
        pieceData = data;
        if (!CrystalDatabase.Shapes.ContainsKey(data.shapeIndex)) return;

        // 1. 배경색 설정 (CrystalGrade 기준)
        if (backgroundImage != null)
        {
            // data.grade가 CrystalGrade 타입이어야 합니다. 
            // 만약 CrystalPieceData 내부 변수명이 다르다면 그에 맞춰 수정하세요.
            Color gradeColor = GetColorByGrade(data.grade);
            backgroundImage.color = gradeColor;
        }

        // 2. 모양 이미지 설정 (원소 기준)
        int[] shape = CrystalDatabase.Shapes[data.shapeIndex];
        Color elementColor = GetElementColor(data.element);

        for (int i = 0; i < 16; i++)
        {
            if (i < shapeImages.Length)
            {
                shapeImages[i].enabled = (shape[i] == 1);
                if (shapeImages[i].enabled)
                {
                    shapeImages[i].color = elementColor;
                }
            }
        }
    }

    private Color GetColorByGrade(CrystalGrade grade)
    {
        return grade switch
        {
            CrystalGrade.Common => new Color(0.85f, 0.85f, 0.85f),    // 하급: 회색
            CrystalGrade.Rare => new Color(0.55f, 0.85f, 0.55f),      // 중급: 연두
            CrystalGrade.Unique => new Color(0.5f, 0.75f, 1f),        // 상급: 하늘
            CrystalGrade.Epic => new Color(0.75f, 0.5f, 0.95f),      // 서사급: 보라
            CrystalGrade.Legendary => new Color(1f, 0.85f, 0.3f),    // 전설급: 골드
            CrystalGrade.Mythic => new Color(1f, 0.5f, 0.5f),       // 신화급: 다홍
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