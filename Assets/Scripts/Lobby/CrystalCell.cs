using UnityEngine;
using UnityEngine.UI;

public class CrystalCell : MonoBehaviour
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
}