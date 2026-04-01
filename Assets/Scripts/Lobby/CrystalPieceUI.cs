using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CrystalPieceUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CrystalPieceData pieceData;

    [Header("UI Components")]
    public Image[] shapeImages; // 4x4 배열(16개)의 이미지 컴포넌트
    public CanvasGroup canvasGroup;

    private Transform originalParent;
    private Vector3 originalPosition;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    // 데이터에 맞춰 조각의 모양과 색상을 초기화
    public void SetData(CrystalPieceData data)
    {
        pieceData = data;
        int[] shape = CrystalDatabase.Shapes[data.shapeIndex];

        for (int i = 0; i < 16; i++)
        {
            if (shapeImages.Length > i)
            {
                // 모양 데이터가 1인 곳만 활성화
                shapeImages[i].gameObject.SetActive(shape[i] == 1);
                shapeImages[i].color = GetElementColor(data.element);
            }
        }
    }

    private Color GetElementColor(CrystalElement element)
    {
        switch (element)
        {
            case CrystalElement.Fire: return Color.red;
            case CrystalElement.Water: return Color.blue;
            case CrystalElement.Earth: return new Color(0.4f, 0.2f, 0f); // 갈색
            case CrystalElement.Air: return Color.cyan;
            case CrystalElement.Prism: return Color.magenta;
            default: return Color.white;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalPosition = transform.position;

        // 드래그 중에는 마우스가 조각을 통과해 아래의 Cell을 감지해야 함
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;

        // 드래그 시 UI가 다른 요소 위에 보이도록 최상위 캔버스로 잠시 이동
        transform.SetParent(CrystalUIManager.Instance.transform);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 마우스 위치로 조각 이동
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1.0f;

        // 마우스 아래에 무엇이 있는지 검사
        CrystalCell targetCell = GetCellUnderMouse(eventData);

        if (targetCell != null && CrystalUIManager.Instance.CanPlacePiece(pieceData, targetCell.cellIndex))
        {
            // 배치 성공
            CrystalUIManager.Instance.PlacePiece(pieceData, targetCell.cellIndex);

            // 인벤토리에서 제거하거나 보이지 않게 처리
            gameObject.SetActive(false);
        }
        else
        {
            // 배치 실패 시 원래 위치로 복귀
            transform.SetParent(originalParent);
            transform.position = originalPosition;
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
}