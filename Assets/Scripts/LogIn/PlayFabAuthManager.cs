using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayFabAuthManager : MonoBehaviour
{
    public static PlayFabAuthManager Instance;

    [SerializeField] private GameObject nicknamePanel;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // [게스트 로그인] 기기 고유 ID를 사용하여 로그인
    public void LoginWithGuest()
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier, // 기기 고유값 사용
            CreateAccount = true // 계정이 없으면 새로 생성
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    // [미래의 나에게: 구글 연동 함수]
    public void LoginWithGoogle(string googleToken)
    {
        // 나중에 구글 SDK 연동 후 이 부분을 호출하게 됩니다.
        Debug.Log("구글 로그인은 추후 구글 SDK 설정 후 구현 예정입니다.");
        /*
        var request = new LoginWithGoogleAccountRequest {
            ServerAuthCode = googleToken,
            CreateAccount = true
        };
        PlayFabClientAPI.LoginWithGoogleAccount(request, OnLoginSuccess, OnLoginFailure);
        */
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log($"<color=green>로그인 성공!</color> 유저 ID: {result.PlayFabId}");
        // 로그인이 성공하면 바로 데이터를 불러옵니다.
        DataManager.instance.LoadData((isNewUser) =>
        {
            if (isNewUser)
            {
                // 신규 유저라면 닉네임 패널 활성화
                nicknamePanel.SetActive(true);
            }
            else
            {
                // 기존 유저라면 바로 이동
                SceneManager.LoadScene("Lobby");
            }
        });
    }

    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError($"로그인 실패: {error.GenerateErrorReport()}");
    }
}