using System;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;
    public UserProfile currentUser;

    // --- 지연 저장 관련 변수 ---
    private bool isDirty = false;       // 데이터 변경 여부
    private Coroutine saveCoroutine;    // 저장 대기 코루틴
    private float saveDelay = 3.0f;     // 3초 대기 후 저장
    // -------------------------


    [Header("모든 유닛 데이터베이스")]
    public List<UnitData> allUnitTemplates;
    [Header("모든 조합법 데이터베이스")]
    public List<MergeRecipe> allRecipes;

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
        isDirty = true;

        // 이미 대기 중인 예약이 있다면 취소하고 새로 카운트 (연속 클릭 대응)
        if (saveCoroutine != null)
        {
            StopCoroutine(saveCoroutine);
        }

        saveCoroutine = StartCoroutine(DelayedSaveRoutine());
    }

    private System.Collections.IEnumerator DelayedSaveRoutine()
    {
        yield return new WaitForSeconds(saveDelay);

        if (isDirty)
        {
            SaveDataInternal();
        }
    }

    // 실제 PlayFab API를 호출하는 내부 함수
    private void SaveDataInternal()
    {
        isDirty = false;
        string json = JsonUtility.ToJson(currentUser);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> {
                { "UserProfile", json }
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result =>
            {
                Debug.Log("<color=green>서버 저장 완료!</color>");
                saveCoroutine = null;
            },
            error =>
            {
                Debug.LogError("저장 실패: " + error.GenerateErrorReport());
                // 실패 시 다시 저장 시도하거나 처리가 필요할 수 있음
                if (error.Error == PlayFabErrorCode.DataUpdateRateExceeded)
                {
                    Debug.LogWarning("속도 제한 걸림 - 잠시 후 다시 시도합니다.");
                    SaveData(); // 다시 예약
                }
            }
        );
    }

    // 게임 종료 시나 씬 전환 시 강제 저장이 필요할 때 사용
    public void SaveDataImmediate()
    {
        if (isDirty)
        {
            if (saveCoroutine != null) StopCoroutine(saveCoroutine);
            SaveDataInternal();
        }
    }

    // [데이터 불러오기] 서버에서 데이터를 가져옴
    public void LoadData(Action<bool> onComplete = null)
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            bool isNewUser = false;
            if (result.Data != null && result.Data.ContainsKey("UserProfile"))
            {
                // 데이터가 있으면 덮어쓰기
                string json = result.Data["UserProfile"].Value;
                currentUser = JsonUtility.FromJson<UserProfile>(json);
                CheckAndAddMissingUnits(); //유닛 리스트 업데이트

                if (currentUser.nickname == "Player") isNewUser = true; //신규 유저
                Debug.Log("기존 데이터 로드 완료");
            }
            else
            {
                // 신규 유저일 경우 초기 데이터 설정 후 서버에 첫 저장
                Debug.Log("신규 유저: 초기 데이터 생성 중...");
                currentUser = new UserProfile();
                foreach (var template in allUnitTemplates)
                {
                    currentUser.unitList.Add(new UnitSaveData(template.unitName));
                }
                isNewUser = true;
                SaveData();
            }
            onComplete?.Invoke(isNewUser);
        },
        error => Debug.LogError("로드 실패: " + error.GenerateErrorReport()));
    }

    private void CheckAndAddMissingUnits() //게임 업데이트로 새 유닛이 생겼을 떄 기존 유저 리스트에도 넣어주는 함수
    {
        foreach (var template in allUnitTemplates)
        {
            // 리스트에 해당 ID가 없으면 추가
            if (!currentUser.unitList.Exists(u => u.unitID == template.unitName))
            {
                currentUser.unitList.Add(new UnitSaveData(template.unitName));
            }
        }
    }
}