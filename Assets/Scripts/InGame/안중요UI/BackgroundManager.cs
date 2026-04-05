using UnityEngine;
using UnityEngine.UI; // UI 컴포넌트를 쓰려면 이게 꼭 필요해요!

public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager instance;

    public Sprite[] difficultyBackgrounds;
    private Image backgroundImage; // SpriteRenderer 대신 Image 사용

    void Awake()
    {
        instance = this;
        backgroundImage = GetComponent<Image>();
    }

    public void ChangeBackground(int difficultyIndex)
    {
        if (difficultyIndex >= 0 && difficultyIndex < difficultyBackgrounds.Length)
        {
            backgroundImage.sprite = difficultyBackgrounds[difficultyIndex];
        }
    }
}