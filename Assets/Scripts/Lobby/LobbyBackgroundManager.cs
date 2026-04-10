using UnityEngine;
using UnityEngine.UI;

public class LobbyBackgroundManager : MonoBehaviour
{
    public static LobbyBackgroundManager instance;

    [Header("테마별 배경 스프라이트 (0:바위산 ~ 4:공허)")]
    public Sprite[] themeBackgrounds;

    private Image backgroundImage;

    void Awake()
    {
        instance = this;
        backgroundImage = GetComponent<Image>();
    }

    void Start()
    {
        // 씬이 시작될 때 현재 선택된 테마로 배경 초기화
        UpdateBackground();
    }

    // 외부(테마 선택 버튼 등)에서 테마를 바꿨을 때 호출할 함수
    public void UpdateBackground()
    {
        if (DataManager.instance == null) return;

        int currentTheme = DataManager.instance.currentUser.selectedTheme;

        if (currentTheme >= 0 && currentTheme < themeBackgrounds.Length)
        {
            backgroundImage.sprite = themeBackgrounds[currentTheme];
        }
        else
        {
            Debug.LogWarning("선택된 테마 인덱스가 배경 배열 범위를 벗어났습니다.");
        }
    }
}