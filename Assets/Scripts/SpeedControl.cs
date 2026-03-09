using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeedControl : MonoBehaviour
{
    private float fast = 1f;
    public TMP_Text speedText; // 버튼의 텍스트 연결

    public void ChangeSpeed()
    {
        if (fast != 3f) fast++;
        else fast = 1f;

        Time.timeScale = fast; // 2배속
        if (speedText != null) speedText.text = "x" + fast.ToString("F0");

        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }
}