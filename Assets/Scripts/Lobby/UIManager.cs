using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Page Panels")]
    public GameObject[] pages; // 0:Shop, 1:Character, 2:MainMenu, 3:Equipment, 4:Special
    public CharacterPanelManager characterPanelManager;

    [Header("Top Bar UI")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI nicknameText;
    public TextMeshProUGUI ticketText;
    public TextMeshProUGUI essenceText; //정수
    public TextMeshProUGUI aetherText; //에테르

    void Start()
    {
        OpenPage(2); // 게임 시작 시 메인 화면(로비) 오픈
        RefreshTopBar(); // 초기 데이터 화면에 표시
    }

    public void OpenPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(false);
        }
        if(index == 2) RefreshTopBar();
        else if (index == 1) characterPanelManager.RefreshPanel();
        pages[index].SetActive(true);
    }

    // DataManager의 실제 데이터를 읽어와서 UI를 갱신
    public void RefreshTopBar()
    {
        if (DataManager.instance == null) return;

        UserProfile data = DataManager.instance.currentUser;

        levelText.text = data.playerLevel.ToString();
        nicknameText.text = data.nickname;

        // "10/10" 형태로 표시
        ticketText.text = $"{data.ticket}/10";

        // 정수가 길어질 수 있으니 보기 좋게 콤마 추가 (예: 1,000,000)
        essenceText.text = data.essence.ToString("N0");
        aetherText.text = data.aether.ToString("N0");
    }

    public void GoToInGame()
    {
        if(DataManager.instance.currentUser.ticket >= 1) {
            DataManager.instance.currentUser.ticket -= 1;
            DataManager.instance.SaveData();
            SceneManager.LoadScene("InGame");
        }
        else
        {
            Debug.Log("티켓 부족");
        }
    }
}