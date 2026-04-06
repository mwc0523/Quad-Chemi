using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    public float moveSpeed = 0.5f; // 위로 올라가는 속도
    public float alphaSpeed = 2.0f; // 사라지는 속도
    public float destroyTime = 1.5f; // 파괴 시간
    private TextMeshPro textMesh;
    private Color alpha;

    void Awake()
    {
        // 몬스터의 자식으로 들어갈 경우 Canvas를 찾거나 직접 참조
        textMesh = GetComponentInChildren<TextMeshPro>();
        alpha = textMesh.color;
    }

    public void Setup(float damage, bool isCritical)
    {
        textMesh.text = GetUnitText(damage);
        if (isCritical)
        {
            textMesh.color = Color.red; // 치명타는 빨간색
            textMesh.fontSize *= 1.2f;  // 치명타는 조금 더 크게
        }
        alpha = textMesh.color;
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        // 위로 이동
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

        // 서서히 투명해짐
        alpha.a = Mathf.Lerp(alpha.a, 0, Time.deltaTime * alphaSpeed);
        textMesh.color = alpha;
    }

    public static string GetUnitText(float value)
    {
        if (value < 1000) return Mathf.RoundToInt(value).ToString(); // 1,000 미만은 그대로

        string[] units = { "", "K", "M", "B", "T" }; // 천, 백만, 십억, 조...
        int unitIndex = 0;
        double doubleValue = value;

        while (doubleValue >= 1000 && unitIndex < units.Length - 1)
        {
            doubleValue /= 1000.0;
            unitIndex++;
        }

        // 소수점 첫째 자리까지 표시 (예: 1.2K, 15.5M)
        return string.Format("{0:F1}{1}", doubleValue, units[unitIndex]);
    }
}