using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;
    public UserProfile currentUser;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // [데이터 저장] 서버에 현재 currentUser 상태를 업로드
    public void SaveData()
    {
        string json = JsonUtility.ToJson(currentUser);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> {
                { "UserProfile", json }
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("서버 저장 완료!"),
            error => Debug.LogError("저장 실패: " + error.GenerateErrorReport())
        );
    }

    // [데이터 불러오기] 서버에서 데이터를 가져옴
    public void LoadData(System.Action onComplete = null)
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            if (result.Data != null && result.Data.ContainsKey("UserProfile"))
            {
                // 데이터가 있으면 덮어쓰기
                string json = result.Data["UserProfile"].Value;
                currentUser = JsonUtility.FromJson<UserProfile>(json);
                Debug.Log("기존 데이터 로드 완료");
            }
            else
            {
                // 신규 유저일 경우 초기 데이터 설정 후 서버에 첫 저장
                Debug.Log("신규 유저: 초기 데이터 생성 중...");
                currentUser = new UserProfile();
                SaveData();
            }
            onComplete?.Invoke();
        },
        error => Debug.LogError("로드 실패: " + error.GenerateErrorReport()));
    }
}