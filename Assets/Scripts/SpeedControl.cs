using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeedControl : MonoBehaviour
{
    private float fast = 1f;
    public TMP_Text speedText; // ¹öÆ°ÀÇ ÅØ½ºÆ® ¿¬°á

    public void ChangeSpeed()
    {
        if (fast != 3f) fast++;
        else fast = 1f;

        Time.timeScale = fast; // 2¹è¼Ó
        if (speedText != null) speedText.text = "x" + fast.ToString("F0");

        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }
}