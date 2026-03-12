using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Page Panels")]
    public GameObject[] pages; // 0:상점, 1:유닛, 2:홈, 3:강화, 4:전능

    [Header("Top Bar Data")]
    public TextMeshProUGUI nicknameText;
    public TextMeshProUGUI ticketText;
    public TextMeshProUGUI gemText;
    public TextMeshProUGUI amethystText;




    void Start() //시작시
    {
        OpenPage(2); //메인화면 오픈
    }

    public void OpenPage(int index) //하단 버튼 클릭시 페이지 열기
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(false);
        }
        pages[index].SetActive(true);
    }

    public void UpdateTopBar(string name, int ticket, 
        int gem, int amethyst) //상단 바 아이템 획득 반영
    {
        nicknameText.text = name;
        ticketText.text = ticket.ToString();
        gemText.text = gem.ToString();
        amethystText.text = amethyst.ToString();
    }

    public void GoToInGame() //게임 시작 버튼
    {
        SceneManager.LoadScene("InGame");
    }
}