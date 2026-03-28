using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeedControl : MonoBehaviour
{
    // static으로 변경하여 어디서든 접근 가능하게 하거나, 인스턴스를 static으로 관리합니다.
    private static float currentFast = 1f;
    public TMP_Text speedText;

    void Awake()
    {
        // 게임 시작 시 초기화
        Time.timeScale = currentFast;
        UpdateTimeScaleText();
    }

    public void ChangeSpeed()
    {
        if (currentFast < 3f) currentFast++;
        else currentFast = 1f;

        ApplySpeed();
    }

    // 시간을 실제로 적용하고 텍스트를 갱신하는 공통 함수
    public static void ApplySpeed()
    {
        Time.timeScale = currentFast;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // 텍스트 UI 갱신 (인스턴스가 있다면)
        var instance = FindFirstObjectByType<SpeedControl>();
        if (instance != null) instance.UpdateTimeScaleText();
    }

    public void UpdateTimeScaleText()
    {
        if (speedText != null) speedText.text = "x" + currentFast.ToString("F0");
    }

    // 외부에서 현재 설정된 배속을 가져올 때
    public static float GetFast()
    {
        return currentFast;
    }
}