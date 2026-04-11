using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CrystalCell : MonoBehaviour, IPointerClickHandler
{
    public int cellIndex;
    public bool isUnlocked;
    public bool isOccupied;

    [Header("UI Components")]
    public Image cellImage; // ❗ UIManager에서 접근할 수 있도록 public으로 변경
    [SerializeField] private GameObject unlockButtonObj;
    [SerializeField] private TextMeshProUGUI priceText;

    [Header("Colors")]
    [SerializeField] private Color lockColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color unlockColor = Color.white;

    private Canvas _canvas;
    private GraphicRaycaster _raycaster;

    void Awake()
    {
        // 6 & 7번 조건: 가려짐 및 클릭 씹힘 방지를 위한 캔버스 자동 추가
        _canvas = GetComponent<Canvas>();
        if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();

        _raycaster = GetComponent<GraphicRaycaster>();
        if (_raycaster == null) _raycaster = gameObject.AddComponent<GraphicRaycaster>();
    }

    void Start()
    {
        CloseUnlockButton(); // 1. 시작 시 무조건 숨기기
    }

    void Update()
    {
        // 5. 버튼 외 다른 곳 클릭 시 닫기
        if (unlockButtonObj != null && unlockButtonObj.activeSelf)
        {
            if (Input.GetMouseButtonDown(0))
            {
                PointerEventData eventData = new PointerEventData(EventSystem.current);
                eventData.position = Input.mousePosition;
                var results = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                bool clickedSelf = false;
                foreach (var result in results)
                {
                    // 나 자신(CrystalCell)이나 내 자식(Button)을 클릭한 거라면 무시
                    if (result.gameObject == gameObject || result.gameObject.transform.IsChildOf(transform))
                    {
                        clickedSelf = true;
                        break;
                    }
                }

                if (!clickedSelf) CloseUnlockButton();
            }
        }
    }

    public void SetUnlock(bool unlock)
    {
        isUnlocked = unlock;
        if (cellImage != null)
            cellImage.color = isUnlocked ? unlockColor : lockColor;

        if (isUnlocked) CloseUnlockButton();
    }

    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return;

        if (isUnlocked)
        {
            // 열린 칸: 크리스탈 회수
            CrystalPieceData piece = CrystalUIManager.Instance.GetPieceAtCell(cellIndex);
            if (piece != null) CrystalUIManager.Instance.RemovePiece(piece);
        }
        else
        {
            // 2. 닫힌 칸: 버튼 열기 토글
            if (unlockButtonObj.activeSelf) CloseUnlockButton();
            else OpenUnlockButton();
        }
    }

    private void OpenUnlockButton()
    {
        // 4. 해당 버튼 제외 다른 버튼 닫기
        CrystalUIManager.Instance.RegisterOpenedCell(this);

        unlockButtonObj.SetActive(true);

        // 6. 버튼이 다른 이미지 뒤로 숨겨지지 않게 최상위로 올림
        if (_canvas != null)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 100;
        }

        // 2. 텍스트 설정
        int price = CrystalUIManager.Instance.GetCurrentUnlockPrice();
        if (priceText != null) priceText.text = $"칸 오픈\n{price}";
    }

    public void CloseUnlockButton()
    {
        if (unlockButtonObj != null) unlockButtonObj.SetActive(false);

        if (_canvas != null)
        {
            _canvas.overrideSorting = false;
            _canvas.sortingOrder = 0;
        }
    }

    // ⭐ 3번 조건: 이 함수를 인스펙터의 Button OnClick 이벤트에 연결해주세요!
    public void OnClickUnlockButton()
    {
        if (CrystalUIManager.Instance.TryUnlockCell(cellIndex))
        {
            CloseUnlockButton();
        }
    }
}