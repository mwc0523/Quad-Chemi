using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DamageEntry : MonoBehaviour
{
    public Image unitIcon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI killText;
    public TextMeshProUGUI ratioText;
    public Slider damageSlider;

    public void SetData(string name, Sprite icon, float damage, int kills, float ratio)
    {
        nameText.text = name;
        unitIcon.sprite = icon;
        damageText.text = damage.ToString("N0"); // 콤마 포함 숫자
        killText.text = $"{kills}";

        ratioText.text = (ratio * 100f).ToString("F1") + "%";
        damageSlider.value = ratio;
    }
}