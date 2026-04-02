using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CrystalCell : MonoBehaviour, IPointerClickHandler
{
    public int cellIndex;
    public bool isUnlocked;
    public bool isOccupied;

    [SerializeField] private Image cellImage;
    [SerializeField] private Color lockColor = Color.gray;
    [SerializeField] private Color unlockColor = Color.white;

    public void SetUnlock(bool unlock)
    {
        isUnlocked = unlock;
        if (cellImage != null)
            cellImage.color = isUnlocked ? unlockColor : lockColor;
    }

    // 칸을 차지하거나 비울 때 호출
    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //Debug.Log($"{cellIndex}번 타일 클릭됨!"); // 로그가 찍히는지 확인

        if (eventData.dragging) return;

        CrystalPieceData piece = CrystalUIManager.Instance.GetPieceAtCell(cellIndex);
        if (piece != null)
        {
            //Debug.Log("크리스탈 발견! 회수 진행");
            CrystalUIManager.Instance.RemovePiece(piece);
        }
        else
        {
            //Debug.Log("이 칸에는 배치된 크리스탈이 없음");
        }
    }
} 