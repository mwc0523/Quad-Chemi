using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;
public class GameResultManager : MonoBehaviour
{
    [Header("상단 정보 UI")]
    public TextMeshProUGUI resultStageText;
    public TextMeshProUGUI resultTypeText;   // 클리어 / 실패
    public TextMeshProUGUI reachedWaveText;  // 도달 웨이브 숫자

    [Header("중앙 연출 UI")]
    public Animator chestAnimator; // 상자 애니메이션
    public TextMeshProUGUI rewardEssenceText; // 정수(Stone) 획득량
    public TextMeshProUGUI rewardAetherText;  // 에테르(Gold?) 획득량
    public TextMeshProUGUI rewardExpText; //경험치 획득량

    [Header("연출 설정")]
    [SerializeField] private float countDuration = 1.5f;

    private const float ESSENCE_EXP_SCALE = 0.0851f; // (50,000 / 1,000 - 1) / 24^2
    private const float AETHER_SCALE = 0.0185f;      // (200 / 18 - 1) / 24^2

    void Start()
    {
        Time.timeScale = 1f;
        int wave = DataManager.instance.currentUser.currentRunReachedWave;
        bool clear = DataManager.instance.currentUser.isCurrentRunClear;
        OnGameEnd(wave, clear);
    }

    public void OnGameEnd(int reachedWave, bool isClear)
    {
        var user = DataManager.instance.currentUser; // 코드 가독성을 위해 변수화

        // 1. 현재 스테이지 정보 가져오기
        int currentTheme = user.selectedTheme;
        int currentStage = user.selectedStage;
        int totalStageIndex = (currentTheme * 5) + (currentStage - 1);

        // 2. 기본 UI 설정
        resultStageText.text = $"{UIManager.themeNames[currentTheme]} {currentStage}단계";
        resultTypeText.text = isClear ? "클리어!" : "실패...";
        reachedWaveText.text = reachedWave.ToString();

        // 3. 진행도(Progress) 갱신 로직 추가 -----------------------------------------

        // 현재 유저의 진행도를 숫자로 환산 (비교용)
        // 예: 테마0-스테이지1 = 1점 / 테마0-스테이지5 = 5점 / 테마1-스테이지1 = 6점
        int currentProgressScore = (currentTheme * 5) + currentStage;
        int savedProgressScore = (user.highestClearedTheme + 1) * 5 + user.highestReachedStage;

        // A. 최고 도달 라운드 갱신
        // 현재 플레이 중인 스테이지가 기록된 최고 스테이지와 같거나 더 높을 때 라운드 비교
        if (currentProgressScore > savedProgressScore)
        {
            user.highestReachedRound = reachedWave;
        }
        else if (currentProgressScore == savedProgressScore)
        {
            if (reachedWave > user.highestReachedRound)
                user.highestReachedRound = reachedWave;
        }

        // B. 클리어 시 다음 스테이지 개방
        if (isClear && currentProgressScore == savedProgressScore)
        {
            if (currentStage < 5)
            {
                // 다음 스테이지로
                user.highestReachedStage++;
            }
            else
            {
                // 5단계를 깼으므로 현재 테마 클리어 처리 및 다음 테마 1단계로
                user.highestClearedTheme = currentTheme;
                user.highestReachedStage = 1;
            }
        }
        // -------------------------------------------------------------------------

        // 4. 차등 보상 계산
        int earnedEssence = CalculateEssence(reachedWave, totalStageIndex);
        int earnedAether = CalculateAether(reachedWave, totalStageIndex);
        int earnedExp = CalculateExp(reachedWave, totalStageIndex);

        // 5. 데이터 업데이트 및 저장 
        user.essence += earnedEssence;
        user.aether += earnedAether;
        user.AddExp(earnedExp);

        DataManager.instance.SaveData();
        DataManager.instance.SaveDataImmediate();

        rewardEssenceText.text = "+0";
        rewardAetherText.text = "+0";
        rewardExpText.text = "+0";

        StartCoroutine(ShowRewardSequence(earnedEssence, earnedAether, earnedExp));
    }

    private IEnumerator ShowRewardSequence(int targetEssence, int targetAether, int targetExp)
    {
        // 상자 애니메이션이 있다면 실행
        if (chestAnimator != null)
        {
            chestAnimator.SetTrigger("Open");
            yield return new WaitForSeconds(0.1f); // 상자 열리는 시간 대기
        }

        float elapsed = 0f;

        while (elapsed < countDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / countDuration;

            // 보간법(Lerp)을 사용하여 숫자를 부드럽게 증가
            int currentEssence = Mathf.RoundToInt(Mathf.Lerp(0, targetEssence, progress));
            int currentAether = Mathf.RoundToInt(Mathf.Lerp(0, targetAether, progress));
            int currentExp = Mathf.RoundToInt(Mathf.Lerp(0, targetExp, progress));

            rewardEssenceText.text = $"+{currentEssence:N0}";
            rewardAetherText.text = $"+{currentAether:N0}";
            rewardExpText.text = $"+{currentExp:N0}";

            yield return null;
        }

        // 마지막에 오차 없도록 최종값 강제 설정
        rewardEssenceText.text = $"+{targetEssence:N0}";
        rewardAetherText.text = $"+{targetAether:N0}";
        rewardExpText.text = $"+{targetExp:N0}";
    }


    // --- 보상 계산 공식 (2차 함수 적용) ---

    private int CalculateEssence(int wave, int stageIndex)
    {
        // 바위산 1단계(index 0) 100웨이브 기준 기본 보상 약 1,000
        float baseReward = (wave * 10) + ((wave / 10) * 5);

        // 가중치: 1 + (0.0851 * index^2) -> index 24일 때 약 50배
        float stageMultiplier = 1f + (ESSENCE_EXP_SCALE * stageIndex * stageIndex);

        return Mathf.RoundToInt(baseReward * stageMultiplier);
    }

    private int CalculateExp(int wave, int stageIndex)
    {
        // 기본 경험치 공식 (기존 유지)
        float baseExp = (wave * 10) + ((int)(wave / 10) * 120);

        // 정수와 동일한 가중치 적용
        float stageMultiplier = 1f + (ESSENCE_EXP_SCALE * stageIndex * stageIndex);

        return Mathf.RoundToInt(baseExp * stageMultiplier);
    }

    private int CalculateAether(int wave, int stageIndex)
    {
        // 바위산 1단계 100웨이브 기준 평균 18개 (기본 8 + 랜덤 10)
        int baseBonus = 0;
        if (wave >= 80) baseBonus = 8;
        else if (wave >= 70) baseBonus = 4;
        else if (wave >= 60) baseBonus = 2;
        else if (wave >= 50) baseBonus = 1;

        float randomBonus = 0;
        for (int i = 0; i < wave; i++)
        {
            if (Random.value < 0.1f) randomBonus++;
        }

        float totalBaseAether = baseBonus + randomBonus;

        // 가중치: 1 + (0.0185 * index^2) -> index 24일 때 약 11.6배 (18 * 11.6 = 약 208개)
        float stageMultiplier = 1f + (AETHER_SCALE * stageIndex * stageIndex);

        return Mathf.RoundToInt(totalBaseAether * stageMultiplier);
    }

    public void ReGame()
    {
        if (DataManager.instance.currentUser.ticket >= 1)
        {
            DataManager.instance.currentUser.ticket -= 1;
            DataManager.instance.SaveData();
            SceneManager.LoadScene("InGame");
        }
        else {
            Debug.Log("티켓 부족");
        }
    }

    public void JustGoToLobby()
    {
        SceneManager.LoadScene("Lobby");
    }

    /*
    // --- 연출 로직 ---
    private IEnumerator ShowRewardSequence(int essence, int aether)
    {
        // 1. 상자 열리는 애니메이션 트리거
        if (chestAnimator != null) chestAnimator.SetTrigger("Open");

        yield return new WaitForSeconds(0.8f); // 애니메이션 타이밍에 맞춰 대기

        // 2. 텍스트 표시 (터지는 효과음이나 파티클을 여기서 생성하면 굿)
        rewardEssenceText.text = $"+ {essence}";
        rewardAetherText.text = $"+ {aether}";

        // 3. 획득 수치 색상이나 크기 연출 추가 가능
    }*/
}