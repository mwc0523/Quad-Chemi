using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlayFab.ClientModels;

public class PlayFabAuthManager : MonoBehaviour
{
    public static PlayFabAuthManager Instance;

    [SerializeField] private GameObject privacyPanel;
    [SerializeField] private GameObject nicknamePanel;
    private const string PrivacyUrl = "https://www.notion.so/1685aef59a728015aff2cbdf5a7af552?source=copy_link"; //개인정보처리방침 링크

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
        privacyPanel.SetActive(false);
        nicknamePanel.SetActive(false);
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
                // 신규 유저라면 동의 패널 활성화
                privacyPanel.SetActive(true);
            }
            else
            {
                //privacyPanel.SetActive(true);
                // 기존 유저라면 바로 이동
                SceneManager.LoadScene("Lobby");
            }
        });
    }

    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError($"로그인 실패: {error.GenerateErrorReport()}");
    }

    public void OnClickOpenPrivacyNotion()
    {
        Application.OpenURL(PrivacyUrl);
    }

    // [동의 및 시작 버튼] 연결
    public void OnClickAgreeAndContinue()
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "setPrivacyAgreement", // 위에서 만든 JS 함수 이름
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request, OnCloudScriptSuccess, OnCloudScriptFailure);
    }

    private void OnCloudScriptSuccess(ExecuteCloudScriptResult result)
    {
        Debug.Log("ReadOnly Data에 동의 여부 저장 완료");
        privacyPanel.SetActive(false);
        nicknamePanel.SetActive(true);
    }

    private void OnCloudScriptFailure(PlayFabError error)
    {
        Debug.LogError("데이터 저장 실패: " + error.GenerateErrorReport());
        // 실패 시 다시 시도하거나 에러 메시지 표시
    }
}