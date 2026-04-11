using UnityEngine;
using UnityEngine.Advertisements;
using System;

public class AdManager : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    public static AdManager instance;

    [Header("유니티 애즈 게임 ID (대시보드에서 복사)")]
    public string androidGameId = "6088047";
    public bool testMode = true; // 출시할 때는 false로 변경!

    private string adUnitId = "Rewarded_Android"; // 기본 보상형 광고 ID
    private Action onRewardCallback; // 광고 시청 완료 후 실행할 함수 보관용

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        // 애즈 초기화 시작
        Advertisement.Initialize(androidGameId, testMode, this);
    }

    // 광고 시청 요청 함수 (다른 스크립트에서 호출)
    public void ShowRewardedAd(Action onSuccess)
    {
        onRewardCallback = onSuccess;
        Debug.Log("광고 로드 중...");
        Advertisement.Load(adUnitId, this); // 먼저 광고를 로드합니다.
    }

    // --- 로드 리스너 ---
    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log("광고 로드 완료. 화면에 띄웁니다.");
        Advertisement.Show(adUnitId, this);
    }
    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message) { Debug.Log("광고 로드 실패"); }

    // --- 시청 리스너 ---
    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        if (placementId == adUnitId && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log("광고 끝까지 시청 완료! 보상을 지급합니다.");
            onRewardCallback?.Invoke(); // 성공 시 보상 함수 실행
        }
    }
    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message) { Debug.Log("광고 재생 실패"); }
    public void OnUnityAdsShowStart(string placementId) { }
    public void OnUnityAdsShowClick(string placementId) { }

    // --- 초기화 리스너 ---
    public void OnInitializationComplete() { Debug.Log("유니티 애즈 초기화 완료"); }
    public void OnInitializationFailed(UnityAdsInitializationError error, string message) { }
}