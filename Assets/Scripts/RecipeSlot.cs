using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeSlot : MonoBehaviour
{
    public Image unitIcon; 
    public TextMeshProUGUI nameText; 
    public TextMeshProUGUI gradeText;
    private UnitData data;

    public void Setup(UnitData _data)
    {
        if (_data == null) return;
        data = _data;

        if (unitIcon != null) unitIcon.sprite = data.unitSprite;
        if (nameText != null) nameText.text = data.unitName;

        // 등급 텍스트와 색상 입히기 (선택 사항)
        if (gradeText != null)
        {
            gradeText.text = data.grade.ToString();
            SetGradeColor(gradeText, data.grade);
        }
    }

    void SetGradeColor(TextMeshProUGUI text, UnitGrade grade)
    {
        switch (grade)
        {
            case UnitGrade.Low: text.color = Color.white; break;
            case UnitGrade.Middle: text.color = new Color(0.5f, 1f, 0.5f); break; // 연두
            case UnitGrade.High: text.color = Color.blue; break;
            case UnitGrade.Epic: text.color = new Color(0.6f, 0f, 1f); break;   // 보라
            case UnitGrade.Legend: text.color = Color.yellow; break;
            case UnitGrade.Myth: text.color = Color.red; break;
        }
    }

    public void OnClick()
    {
        // 클릭 시 매니저에게 내 정보를 전달하며 상세창을 띄우라고 함
        FindObjectOfType<RecipeManager>().ShowRecipeDetail(data);
    }
}