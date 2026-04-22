using UnityEngine;
using UnityEngine.UI;

public class ProfileItem : MonoBehaviour
{
    [SerializeField] private Image unitIcon;
    [SerializeField] private Image unitSelectImage;
    private string unitID;
    private ProfileImageManager manager;

    // 프리펩 생성 시 호출되어 데이터를 세팅함
    public void SetData(UnitData data, ProfileImageManager manager)
    {
        this.unitID = data.unitName;
        this.manager = manager;
        this.unitIcon.sprite = data.unitSprite;
        unitSelectImage.gameObject.SetActive(false);
        if(unitID == DataManager.instance.currentUser.profileIconName) unitSelectImage.gameObject.SetActive(true);
    }

    // 프리펩 내의 버튼에 연결할 함수
    public void OnClickItem()
    {
        unitSelectImage.gameObject.SetActive(true);
        manager.OnSelectProfile(unitID);
    }
}