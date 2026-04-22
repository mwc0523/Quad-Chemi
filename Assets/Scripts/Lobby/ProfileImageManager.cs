using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ProfileImageManager : MonoBehaviour
{
    [Header("UI UI References")]
    [SerializeField] private GameObject profilePopup;    // 프로필 선택 팝업창 (기본 비활성)
    [SerializeField] private Transform contentParent;     // 스크롤뷰의 Content 객체
    [SerializeField] private GameObject itemPrefab;      // 위에서 만든 ProfileItem 프리펩
    [SerializeField] private Image currentProfileIcon;    // 로비 상단에 표시되는 현재 프사

    private void Start()
    {
        profilePopup.SetActive(false);
        // 데이터가 이미 로드되어 있다면 초기 아이콘 설정
        if (DataManager.instance.isDataLoaded) UpdateCurrentIcon();
        else DataManager.instance.OnDataLoaded += UpdateCurrentIcon;
    }

    // [B_Profile 버튼에 연결]
    public void OnClickProfileButton()
    {
        profilePopup.SetActive(true);
        RefreshList();
    }

    public void OnClickProfileCloseButton()
    {
        profilePopup.SetActive(false);
    }

    // 스크롤뷰 목록 생성
    private void RefreshList()
    {
        // 기존 목록 제거 (풀링을 사용하지 않는 경우)
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // DataManager의 유닛 데이터를 순회하며 프리펩 생성
        foreach (var unit in DataManager.instance.allUnitTemplates)
        {
            GameObject obj = Instantiate(itemPrefab, contentParent);
            ProfileItem item = obj.GetComponent<ProfileItem>();
            item.SetData(unit, this);
        }
    }

    // [ProfileItem에서 클릭 시 호출]
    public void OnSelectProfile(string unitID)
    {
        // 1. 데이터 변경
        DataManager.instance.currentUser.profileIconName = unitID;
        DataManager.instance.SaveData();

        // 3. UI 업데이트 및 팝업 닫기
        UpdateCurrentIcon();
        profilePopup.SetActive(false);
    }

    private void UpdateCurrentIcon()
    {
        string iconName = DataManager.instance.currentUser.profileIconName;
        if (string.IsNullOrEmpty(iconName)) return;

        UnitData data = DataManager.instance.allUnitTemplates.Find(u => u.unitName == iconName);
        if (data != null)
        {
            currentProfileIcon.sprite = data.unitSprite;
        }
    }
}