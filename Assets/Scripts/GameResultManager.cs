using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;
public class GameResultManager : MonoBehaviour
{
    [Header("상단 정보 UI")]
    public TextMeshProUGUI resultTypeText;   // 클리어 / 실패
    public TextMeshProUGUI reachedWaveText;  // 도달 웨이브 숫자

    [Header("중앙 연출 UI")]
    public Animator chestAnimator; // 상자 애니메이션
    public TextMeshProUGUI rewardEssenceText; // 정수(Stone) 획득량
    public TextMeshProUGUI rewardAetherText;  // 에테르(Gold?) 획득량
    public TextMeshProUGUI rewardExpText; //경험치 획득량

    void Start() {
        int wave = DataManager.instance.currentUser.currentRunReachedWave;
        bool clear = DataManager.instance.currentUser.isCurrentRunClear;
        OnGameEnd(wave, clear);
    }
    public void OnGameEnd(int reachedWave, bool isClear)
    {
        // 1. 기본 UI 텍스트 설정
        resultTypeText.text = isClear ? "클리어!" : "실패...";
        reachedWaveText.text = reachedWave.ToString();

        // 2. 형의 기획 공식에 따른 보상 계산
        int earnedEssence = CalculateEssence(reachedWave);
        int earnedAether = CalculateAether(reachedWave);
        int earnedExp = CalculateExp(reachedWave);
        rewardEssenceText.text = "+" + earnedEssence;
        rewardAetherText.text = "+" + earnedAether;
        rewardExpText.text = "+" + earnedExp;

        // 3. 로컬 데이터 업데이트
        // (기존 currentUser의 변수명에 맞춰 essence/aether 등으로 수정하세요)
        DataManager.instance.currentUser.essence += earnedEssence;
        DataManager.instance.currentUser.aether += earnedAether;
        DataManager.instance.currentUser.AddExp(earnedExp);

        // 4. 서버 저장
        DataManager.instance.SaveData();
        DataManager.instance.SaveDataImmediate();

        // 5. 연출 시작
        //StartCoroutine(ShowRewardSequence(earnedEssence, earnedAether));
    }

    // --- 보상 계산 공식 로직 ---
    private int CalculateEssence(int wave)
    {
        // (도달 웨이브 * 10) + (10단위 클리어마다 1, 2, 4, 8... 누적)
        int baseReward = wave * 10;
        int tenUnitCount = wave / 10;
        int bonus = (tenUnitCount > 0) ? (int)Mathf.Pow(2, tenUnitCount) - 1 : 0;
        return baseReward + bonus;
    }

    private int CalculateExp(int wave)
    {
        return (wave * 10) + ((int)(wave/10) * 120);
    }

    private int CalculateAether(int wave)
    {
        // 50클:1, 60클:2, 70클:4, 80클:8 + 라운드당 10% 확률 추가
        int baseAether = 0;
        if (wave >= 80) baseAether = 8;
        else if (wave >= 70) baseAether = 4;
        else if (wave >= 60) baseAether = 2;
        else if (wave >= 50) baseAether = 1;

        int randomBonus = 0;
        for (int i = 0; i < wave; i++)
        {
            if (Random.value < 0.1f) randomBonus++;
        }
        return baseAether + randomBonus;
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