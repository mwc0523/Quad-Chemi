using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RewardPopupUI : MonoBehaviour
{
    [System.Serializable]
    public struct RewardGroup
    {
        public GameObject obj;        // 아이콘+텍스트 부모 오브젝트
        public TextMeshProUGUI text; // 보상 수량 텍스트
    }

    public RewardGroup essence;
    public RewardGroup aether;
    public RewardGroup ticket;

    // 패널 켜기
    public void Show(int eAmount, int aAmount, int tAmount)
    {
        gameObject.SetActive(true);

        // 수량이 0보다 큰 것만 표시
        essence.obj.SetActive(eAmount > 0);
        if (eAmount > 0) essence.text.text = $"+{eAmount}";

        aether.obj.SetActive(aAmount > 0);
        if (aAmount > 0) aether.text.text = $"+{aAmount}";

        ticket.obj.SetActive(tAmount > 0);
        if (tAmount > 0) ticket.text.text = $"+{tAmount}";
    }

    // 화면 아무 곳이나 클릭하면 호출될 함수 (Full-Screen Button에 연결)
    public void OnClickClose()
    {
        gameObject.SetActive(false);
    }
}