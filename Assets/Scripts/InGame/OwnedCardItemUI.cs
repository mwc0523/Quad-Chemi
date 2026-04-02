using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OwnedCardItemUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public Image backgroundImage;

    public void Setup(CardData data)
    {
        nameText.text = $"[{data.grade}] {data.name}";
        descText.text = data.desc;
        backgroundImage.color = GetColorByGrade(data.grade);
    }

    private Color GetColorByGrade(CardGrade grade)
    {
        return grade switch
        {
            CardGrade.Low => new Color(0.85f, 0.85f, 0.85f),
            CardGrade.Mid => new Color(0.55f, 0.85f, 0.55f),
            CardGrade.High => new Color(0.5f, 0.75f, 1f),
            CardGrade.Epic => new Color(0.75f, 0.5f, 0.95f),
            CardGrade.Legendary => new Color(1f, 0.85f, 0.3f),
            CardGrade.Myth => new Color(1f, 0.5f, 0.5f),
            _ => Color.white
        };
    }
}