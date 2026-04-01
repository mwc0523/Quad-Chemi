using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    
    [Header("Page Panels")]
    public GameObject[] pages; // 0:Shop, 1:Character, 2:MainMenu, 3:crystal, 4:Special
    public CharacterPanelManager characterPanelManager;

    [Header("Top Bar UI")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI nicknameText;
    public TextMeshProUGUI ticketText;
    public TextMeshProUGUI essenceText; //정수
    public TextMeshProUGUI aetherText; //에테르

    [Header("Ticket Timer UI")]
    public TextMeshProUGUI ticketTimerText;

    [Header("Experience UI")]
    public Slider expSlider; // 경험치 슬라이더
    public TextMeshProUGUI expText;
    public TextMeshProUGUI expPercentText;

    [Header("Stage Selection UI")]
    public TextMeshProUGUI themeNameText; // 테마 이름 (예: 바위산)
    public TextMeshProUGUI stageLevelText; // 단계 이름 (예: 1단계)

    // 테마 및 단계 조작 버튼 (<, > 모양 버튼 4개)
    public Button btnThemePrev;
    public Button btnThemeNext;
    public Button btnStagePrev;
    public Button btnStageNext;

    public static readonly string[] themeNames = { "바위산", "숲", "바다", "화산", "공허" };

    void Awake()
    {
        // 싱글톤 인스턴스 할당
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        OpenPage(2); // 게임 시작 시 메인 화면(로비) 오픈
        RefreshTopBar(); // 초기 데이터 화면에 표시
        UpdateStageSelectionUI();
    }

    void Update()
    {
        // 매 프레임 티켓 회복 로직 체크
        UpdateTicketRecovery();
    }

    public void OpenPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(false);
        }
        if(index == 2) RefreshTopBar();
        else if (index == 1) characterPanelManager.RefreshPanel();
        pages[index].SetActive(true);
    }

    private void UpdateTicketRecovery()
    {
        if (DataManager.instance == null) return;
        UserProfile data = DataManager.instance.currentUser;

        // 1. 티켓이 이미 꽉 찼다면 타이머 숨기기
        if (data.ticket >= UserProfile.MAX_TICKET)
        {
            if (ticketTimerText != null) ticketTimerText.text = "MAX";
            data.lastTicketChargeTime = ""; // 꽉 차면 시작 시간 초기화
            return;
        }

        // 2. 현재 시간 가져오기
        DateTime now = DateTime.Now;

        // 3. 마지막 충전 시각이 비어있다면 현재 시간으로 설정 (처음 티켓이 깎인 순간)
        if (string.IsNullOrEmpty(data.lastTicketChargeTime))
        {
            data.lastTicketChargeTime = now.ToBinary().ToString();
            DataManager.instance.SaveData();
            return;
        }

        // 4. 경과 시간 계산
        DateTime lastCharge = DateTime.FromBinary(long.Parse(data.lastTicketChargeTime));
        TimeSpan elapsed = now - lastCharge;

        double totalSecondsElapsed = elapsed.TotalSeconds;
        int secondsPerTicket = UserProfile.CHARGE_INTERVAL_MINUTES * 60;

        // 5. 티켓 충전 처리 (접속 중 + 오프라인 공통 적용)
        if (totalSecondsElapsed >= secondsPerTicket)
        {
            int amountToRecover = (int)(totalSecondsElapsed / secondsPerTicket);
            data.ticket = Mathf.Min(data.ticket + amountToRecover, UserProfile.MAX_TICKET);

            // 마지막 충전 시간 갱신 (남은 초만큼 앞으로 당겨서 오차 방지)
            DateTime nextStartTime = lastCharge.AddSeconds(amountToRecover * secondsPerTicket);
            data.lastTicketChargeTime = nextStartTime.ToBinary().ToString();

            RefreshTopBar();
            DataManager.instance.SaveData();
        }

        // 6. UI 타이머 표시 (다음 충전까지 남은 시간)
        if (ticketTimerText != null)
        {
            double remainingSeconds = secondsPerTicket - (totalSecondsElapsed % secondsPerTicket);
            int min = (int)remainingSeconds / 60;
            int sec = (int)remainingSeconds % 60;
            ticketTimerText.text = $"({min:D2}:{sec:D2})";
        }
    }

    // DataManager의 실제 데이터를 읽어와서 UI를 갱신
    public void RefreshTopBar()
    {
        if (DataManager.instance == null) return;

        UserProfile data = DataManager.instance.currentUser;
        data.AddExp(0);

        ticketText.text = $"{data.ticket}/{UserProfile.MAX_TICKET}";
        levelText.text = data.playerLevel.ToString();
        nicknameText.text = data.nickname;

        // "10/10" 형태로 표시
        ticketText.text = $"{data.ticket}/10";

        // 정수가 길어질 수 있으니 보기 좋게 콤마 추가 (예: 1,000,000)
        essenceText.text = data.essence.ToString("N0");
        aetherText.text = data.aether.ToString("N0");
        if (expSlider != null)
        {
            int requiredExp = data.GetRequiredExp(data.playerLevel);
            expSlider.maxValue = requiredExp;
            expSlider.value = data.currentExp;

            // 만약 경험치 텍스트 UI도 만들어 연결했다면 활성화
            if (expText != null)
            {
                expText.text = $"{data.currentExp} / {requiredExp}";
                expPercentText.text = $"{((float)data.currentExp / requiredExp * 100f):F1}%";
            }
        }
    }

    // [신규] 선택된 난이도 UI 텍스트 갱신 및 버튼 활성화 제어
    public void UpdateStageSelectionUI()
    {
        if (DataManager.instance == null) return;
        UserProfile data = DataManager.instance.currentUser;

        // 텍스트 적용
        themeNameText.text = themeNames[data.selectedTheme];
        stageLevelText.text = $"{data.selectedStage} 단계";

        // 유저가 접근할 수 있는 최대 테마 계산 (클리어한 테마 + 1)
        int maxAllowedTheme = Mathf.Min(data.highestClearedTheme + 1, 4);

        // 이전/다음 테마 버튼 활성화 로직
        btnThemePrev.interactable = (data.selectedTheme > 0);
        btnThemeNext.interactable = (data.selectedTheme < maxAllowedTheme);

        // 이전/다음 단계 버튼 활성화 로직
        btnStagePrev.interactable = (data.selectedStage > 1);

        // 현재 선택한 테마가 최고 도달 테마보다 낮으면(이미 클리어한 테마면) 5단계까지 자유롭게 선택 가능
        if (data.selectedTheme < maxAllowedTheme)
        {
            btnStageNext.interactable = (data.selectedStage < 5);
        }
        else // 현재 도전 중인 최고 테마라면 최고 도달 단계까지만 선택 가능
        {
            btnStageNext.interactable = (data.selectedStage < data.highestReachedStage) && (data.selectedStage <5);
        }
    }

    // [신규] 테마 변경 버튼 클릭 시 호출 (인스펙터에서 좌/우 버튼에 매핑: amount에 -1 또는 1)
    public void OnClickChangeTheme(int amount)
    {
        UserProfile data = DataManager.instance.currentUser;
        int maxAllowedTheme = Mathf.Min(data.highestClearedTheme + 1, 4);

        data.selectedTheme = Mathf.Clamp(data.selectedTheme + amount, 0, maxAllowedTheme);

        // 테마를 변경하면 단계를 안전하게 1단계로 초기화
        data.selectedStage = 1;

        UpdateStageSelectionUI();
    }

    // [신규] 단계 변경 버튼 클릭 시 호출 (인스펙터에서 좌/우 버튼에 매핑: amount에 -1 또는 1)
    public void OnClickChangeStage(int amount)
    {
        UserProfile data = DataManager.instance.currentUser;

        int maxAllowedStage = (data.selectedTheme < Mathf.Min(data.highestClearedTheme + 1, 4)) ? 5 : data.highestReachedStage;

        data.selectedStage = Mathf.Clamp(data.selectedStage + amount, 1, maxAllowedStage);

        UpdateStageSelectionUI();
    }












    public void GoToInGame()
    {
        if(DataManager.instance.currentUser.ticket >= 1) {
            DataManager.instance.currentUser.ticket -= 1;
            DataManager.instance.SaveData();
            DataManager.instance.SaveDataImmediate();
            SceneManager.LoadScene("InGame");
        }
        else
        {
            Debug.Log("티켓 부족");
        }
    }
}