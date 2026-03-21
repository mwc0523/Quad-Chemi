using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;

public class NicknamePanelManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField nicknameInputField;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmClick);
        statusText.text = "닉네임을 입력해주세요 (2~8자)";
    }

    private void OnConfirmClick()
    {
        string nickname = nicknameInputField.text.Trim();

        // 1. 예외 처리 검사
        if (!IsValidNickname(nickname)) return;

        // 2. 버튼 비활성화 (중복 클릭 방지)
        confirmButton.interactable = false;
        statusText.text = "닉네임 설정 중...";

        // 3. PlayFab 디스플레이 네임 업데이트
        UpdateNicknameOnServer(nickname);
    }

    private bool IsValidNickname(string nickname)
    {
        // 글자 수 체크
        if (nickname.Length < 2 || nickname.Length > 8)
        {
            statusText.text = "<color=red>닉네임은 2~8자 사이여야 합니다.</color>";
            return false;
        }

        // 특수문자 및 공백 체크 (한글, 영문, 숫자만 허용)
        if (!Regex.IsMatch(nickname, @"^[0-9a-zA-Z가-힣]*$"))
        {
            statusText.text = "<color=red>특수문자나 공백은 사용할 수 없습니다.</color>";
            return false;
        }

        return true;
    }

    private void UpdateNicknameOnServer(string newNickname)
    {
        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = newNickname
        };

        PlayFabClientAPI.UpdateUserTitleDisplayName(request,
            result =>
            {
                // PlayFab 시스템 이름 변경 성공 시, 우리쪽 DataManager 데이터도 갱신
                DataManager.instance.currentUser.nickname = result.DisplayName;
                DataManager.instance.SaveData();

                Debug.Log("닉네임 설정 완료!");
                SceneManager.LoadScene("Lobby");
            },
            error =>
            {
                confirmButton.interactable = true;
                // 이름 중복 등의 에러 처리
                if (error.Error == PlayFabErrorCode.NameNotAvailable)
                    statusText.text = "<color=red>이미 사용 중인 닉네임입니다.</color>";
                else
                    statusText.text = "<color=red>에러 발생: " + error.ErrorMessage + "</color>";
            }
        );
    }
}