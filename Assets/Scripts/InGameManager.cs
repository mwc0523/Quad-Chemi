using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;

[System.Serializable]
public struct MergeRecipe //유닛 조합표 구조체
{
    public UnitData materialA; // 재료 1 (예: 불네모)
    public UnitData materialB; // 재료 2 (예: 땅네모)
    public UnitData result;    // 결과물 (예: 용암네모)
}

public class InGameManager : MonoBehaviour
{
    public static InGameManager instance; // 어디서든 쉽게 접근할 수 있게 싱글톤으로 만듭니다.
    private Unit selectedUnit;
    Unit draggingUnit;
    Transform originalTile;
    bool isDragging = false;
    int summonFee = 20;

    [Header("UI 연결")]
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI elementStoneText;
    public TextMeshProUGUI T_summonButton;

    [Header("게임 설정")]
    public int maxRound = 100;
    public float roundDuration = 15f; // 1라운드당 15초

    [Header("현재 상태 (보기용)")]
    public int currentRound = 1;
    public float currentTime = 0f;
    public int currentCoin = 0; // 로비로 돌아가면 0으로 초기화될 인게임 전용 재화
    public int currentElementStone = 0; //원소석

    [Header("몬스터 관리")]
    public TextMeshProUGUI monsterCountText;
    public int currentMonsterCount = 0;
    public int maxMonsterLimit = 100; // 100마리 넘으면 게임오버
    public EnemySpawner spawner;

    [Header("소환 설정")]
    public GameObject unitBasePrefab; // 유닛 본체 프리팹 (Unit 스크립트가 붙은 사각형)
    public UnitData[] LowPool;    // 하급 유닛 도감 리스트
    public UnitData[] MiddlePool;      // 중급 유닛 도감 리스트
    public UnitData[] HighPool;    // 상급 유닛 도감 리스트
    public UnitData[] EpicPool;    // 서사급 유닛 도감 리스트
    public UnitData[] LegendPool;    // 전설급 유닛 도감 리스트
    public UnitData[] MythPool;    // 신화급 유닛 도감 리스트
    public MapManager mapManager;

    [Header("합성 레시피 (조합표)")]
    public MergeRecipe[] recipes;

    [Header("디버그 설정")]
    public TMPro.TMP_InputField debugInputField;



    void Awake()
    {
        // 싱글톤 세팅 (이 씬 안에서 InGameManager.instance 로 이 스크립트를 부를 수 있습니다)
        if (instance == null) instance = this;
    }

    void Start()
    {
        // 게임 시작 시 초기화
        currentRound = 1;
        currentTime = roundDuration;
        currentCoin = 50; // 코인 50개로 시작
        currentElementStone = 0;
        UpdateUI();
        spawner.StartSpawn();
    }

    void Update()
    {
        HandleInput();

        //타이머
        currentTime -= Time.deltaTime;
        if (currentTime <= 0f)
        {
            NextRound();
        }
        timerText.text = "0:" + ((int)currentTime).ToString("00");

    }

    private void HandleInput()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int unitLayer = LayerMask.GetMask("Unit");
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 0f, unitLayer);

            if (hit.collider != null)
            {
                draggingUnit = hit.collider.GetComponent<Unit>();

                if (draggingUnit != null)
                {
                    StartDrag(draggingUnit);
                }
            }
            else
            {
                ClearSelection();
            }
        }

        if (isDragging)
        {
            DragUnit(mousePos);

            if (Input.GetMouseButtonUp(0))
            {
                DropUnit(mousePos);
            }
        }
    }

    private void StartDrag(Unit unit)
    {
        SelectUnit(unit);
        isDragging = true;
        draggingUnit = unit;

        originalTile = unit.transform.parent;

        unit.transform.SetParent(null);

        SpriteRenderer sr = unit.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = 100;
    }

    private void DragUnit(Vector2 mousePos)
    {
        Vector3 pos = mousePos;
        pos.z = -1f;

        draggingUnit.transform.position = pos;
    }

    void DropUnit(Vector2 mousePos)
    {
        isDragging = false;

        SpriteRenderer sr = draggingUnit.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = 0;

        Collider2D[] hits = Physics2D.OverlapCircleAll(mousePos, 0.1f);

        Transform targetTile = null;
        Unit otherUnit = null;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("BuildTile"))
                targetTile = hit.transform;

            Unit u = hit.GetComponent<Unit>();
            if (u != null && u != draggingUnit)
            {
                otherUnit = u;
                targetTile = u.transform.parent;
            }
        }

        // 합성
        if (otherUnit != null)
        {
            UnitData result = GetMergeResult(draggingUnit.data, otherUnit.data);

            if (result != null)
            {
                Transform tile = otherUnit.transform.parent;

                GameObject obj = Instantiate(unitBasePrefab, tile.position, Quaternion.identity, tile);
                obj.transform.localPosition = new Vector3(0, 0, -1);

                obj.GetComponent<Unit>().SetUnit(result);

                Destroy(otherUnit.gameObject);
                Destroy(draggingUnit.gameObject);

                draggingUnit = null;
                return;
            }
        }

        // 이동
        if (targetTile != null)
        {
            Unit unitOnTile = targetTile.GetComponentInChildren<Unit>();

            if (unitOnTile == null)
            {
                draggingUnit.transform.SetParent(targetTile);
                draggingUnit.transform.localPosition = new Vector3(0, 0, -1);
            }
            else
            {
                ReturnUnit();
            }
        }
        else
        {
            ReturnUnit();
        }

        draggingUnit = null;
    }

    private void ReturnUnit()
    {
        draggingUnit.transform.SetParent(originalTile);
        draggingUnit.transform.localPosition = new Vector3(0, 0, -1);
    }

    public void OnClickSummonButton()
    {
        if (currentCoin >= summonFee)
        {
            Transform targetTile = null;

            // MapManager가 미리 골라놓은 '배치 타일 리스트'만 검사합니다.
            foreach (Transform tile in mapManager.buildTiles)
            {
                // 자식이 없으면(= 유닛이 소환되지 않은 빈자리면) 바로 선택!
                if (tile.childCount == 0)
                {
                    targetTile = tile;
                    break;
                }
            }

            if (targetTile != null)
            {
                currentCoin -= summonFee;
                summonFee += 2; //2씩 증가
                UpdateUI();
                SpawnRandomUnit(targetTile);
            }
            else
            {
                Debug.Log("더 이상 배치할 공간이 없습니다!");
            }
        }
    }
    public UnitData GetMergeResult(UnitData a, UnitData b) // 유닛 합칠때 레시피 검사
    {
        foreach (var recipe in recipes)
        {
            // 재료 A, B가 순서에 상관없이 일치하는지 확인
            bool match1 = (recipe.materialA == a && recipe.materialB == b);
            bool match2 = (recipe.materialA == b && recipe.materialB == a);

            if (match1 || match2)
            {
                return recipe.result;
            }
        }
        return null; // 레시피에 없으면 null 반환
    }
    void SpawnRandomUnit(Transform parentTile)
    {
        // 2. 등급 가챠
        int rand = Random.Range(0, 10000);
        UnitData selectedData;

        if (rand < 9900) selectedData = LowPool[Random.Range(0, LowPool.Length)];
        else if (rand < 9990) selectedData = MiddlePool[Random.Range(0, MiddlePool.Length)];
        else selectedData = HighPool[Random.Range(0, HighPool.Length)];

        // 3. 소환 및 데이터 주입
        GameObject unitObj = Instantiate(unitBasePrefab, parentTile.position, Quaternion.identity, parentTile);
        unitObj.transform.localPosition = new Vector3(0, 0, -1f);
        unitObj.GetComponent<Unit>().SetUnit(selectedData);
    }

    // 다음 라운드로 넘어가는 함수
    void NextRound()
    {
        if (currentRound < maxRound)
        {
            currentRound++;
            currentTime = roundDuration; // 시간 다시 15초로 꽉 채우기
            UpdateUI();

            spawner.StartSpawn();
            Debug.Log(currentRound + " 라운드 시작!");
        }
        else
        {
            // 100라운드까지 다 깼을 때
            currentTime = 0f;
            Debug.Log("게임 클리어! 보스 등장 또는 로비로 이동");
        }
    }
    public void OnMonsterSpawned()
    {
        currentMonsterCount++;
        UpdateMonsterUI();
        if (currentMonsterCount >= maxMonsterLimit) GameOver();
    }
    public void OnMonsterDestroyed()
    {
        currentMonsterCount--;
        UpdateMonsterUI();
    }
    void UpdateMonsterUI()
    {
        monsterCountText.text = $"{currentMonsterCount} / {maxMonsterLimit}";
    }
    void GameOver()
    {
        Time.timeScale = 0; // 게임 일시정지
        Debug.Log("GAME OVER!");
        // 여기에 게임오버 UI 띄우기
    }
    // 코인 획득 (몹을 잡았을 때 부를 함수)
    public void AddCoin(int amount)
    {
        currentCoin += amount;
        UpdateUI();
    }
    public void AddElementStone(int amount)
    {
        currentElementStone += amount;
        UpdateUI();
    }

    // 화면의 글자들을 새로고침하는 함수
    void UpdateUI()
    {
        roundText.text = "Round " + currentRound;
        coinText.text = currentCoin + " C";
        elementStoneText.text = currentElementStone + " E";
        T_summonButton.text = "네모 소환\n" + summonFee.ToString() + " C";
    }

    // 유닛이 클릭되었을 때 호출되는 함수
    public void SelectUnit(Unit unit)
    {
        // 이미 선택된 유닛이 있다면 그 유닛의 사거리를 끕니다.
        if (selectedUnit != null)
        {
            selectedUnit.ShowRange(false);
        }

        // 새로운 유닛을 선택하고 사거리를 켭니다.
        selectedUnit = unit;
        selectedUnit.ShowRange(true);
        InGameUIManager.instance.ShowUnitInfo(unit);
    }

    // 선택 해제 함수
    public void ClearSelection()
    {
        if (selectedUnit != null)
        {
            selectedUnit.ShowRange(false);
            selectedUnit = null;
        }
        InGameUIManager.instance.HideUnitInfo();
    }


    public void DebugSummonByName()
    {
        string unitName = debugInputField.text;
        if (string.IsNullOrEmpty(unitName)) return;

        // 1. 모든 풀(Low~Myth)을 뒤져서 이름이 일치하는 UnitData 찾기
        UnitData targetData = FindUnitDataByName(unitName);

        if (targetData != null)
        {
            // 2. 빈 타일 찾기 (기존 OnClickSummonButton 로직 활용)
            Transform emptyTile = null;
            foreach (Transform tile in mapManager.buildTiles)
            {
                if (tile.childCount == 0)
                {
                    emptyTile = tile;
                    break;
                }
            }

            if (emptyTile != null)
            {
                // 3. 소환
                GameObject unitObj = Instantiate(unitBasePrefab, emptyTile.position, Quaternion.identity, emptyTile);
                unitObj.transform.localPosition = new Vector3(0, 0, -1f);
                unitObj.GetComponent<Unit>().SetUnit(targetData);

                Debug.Log($"[Debug] {unitName} 소환 완료!");
                debugInputField.text = ""; // 입력창 초기화
                debugInputField.ActivateInputField(); // 다시 바로 입력할 수 있게 포커스
            }
            else
            {
                Debug.LogWarning("배치할 빈 공간이 없습니다!");
            }
        }
        else
        {
            Debug.LogError($"{unitName}이라는 유닛을 찾을 수 없습니다. (대소문자/공백 확인)");
        }
    }

    // 모든 풀에서 이름을 대조하는 헬퍼 함수
    private UnitData FindUnitDataByName(string n)
    {
        // 모든 풀을 하나의 리스트로 체크 (편의상)
        UnitData[][] allPools = { LowPool, MiddlePool, HighPool, EpicPool, LegendPool, MythPool };

        foreach (var pool in allPools)
        {
            foreach (var data in pool)
            {
                if (data != null && data.unitName.Contains(n)) return data;
            }
        }
        return null;
    }
    //테스트용 버튼
    public void GoToLobby()
    {
        SceneManager.LoadScene("Lobby");
    }
}