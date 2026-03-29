using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

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
    bool isBossDie = false;
    bool isClear = false;

    [Header("UI 연결")]
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI elementStoneText;
    public TextMeshProUGUI T_summonButton;

    [Header("게임 설정")]
    public int maxRound = 100;
    public float roundDuration = 15f; // 1라운드당 15초
    public float bossRoundDuration = 60f; // 1라운드당 60초

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
        Time.timeScale = 1f;
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
        StartCoroutine(GoldMineRoutine());
    }

    void Update()
    {
        HandleInput();

        //타이머
        currentTime -= Time.deltaTime;
        if (currentTime <= 0f)
        {
            if (currentRound%10 != 0 || (currentRound % 10 == 0 && isBossDie)) {//보스 라운드가 아닐때
                isBossDie = false;
                NextRound();
            }
            else GameOver(); //보스 라운드일때
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
                // 1. 삭제될 유닛들을 즉시 비활성화 (이게 "두 마리 팔아야 하는 버그"를 잡는 핵심입니다)
                otherUnit.gameObject.SetActive(false);
                draggingUnit.gameObject.SetActive(false);

                // 2. 리스트에서 제거
                CardUIManager.instance.activeUnits.Remove(otherUnit);
                CardUIManager.instance.activeUnits.Remove(draggingUnit);

                // 3. 새 유닛 생성
                Transform tile = otherUnit.transform.parent;
                GameObject obj = Instantiate(unitBasePrefab, tile.position, Quaternion.identity, tile);
                obj.transform.localPosition = new Vector3(0, 0, -1);

                Unit newUnit = obj.GetComponent<Unit>();
                newUnit.SetUnit(result);
                if(CardUIManager.instance.HasCard(CardEffectID.Mid_ElementReverse)) AddCoin(5); //원소 역전 카드 효과
                OnUnitAdded(newUnit);

                // 4. 새 유닛을 리스트에 즉시 추가 (Start를 기다리지 않음)
                if (!CardUIManager.instance.activeUnits.Contains(newUnit))
                    CardUIManager.instance.activeUnits.Add(newUnit);

                // 5. 전체 스탯 갱신 (비활성화된 유닛들은 이제 계산에서 빠짐)
                CardUIManager.instance.RefreshAllUnitStats();

                // 6. 실제 파괴
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
                unitOnTile.transform.SetParent(originalTile);
                unitOnTile.transform.localPosition = new Vector3(0, 0, -1);
                draggingUnit.transform.SetParent(targetTile);
                draggingUnit.transform.localPosition = new Vector3(0, 0, -1);
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
                SpawnRandomUnit(targetTile, 0);
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
    public void SpawnRandomUnit(Transform parentTile, int isStrict)
    {
        // 2. 등급 가챠
        int rand = Random.Range(0, 10000);
        UnitData selectedData;

        if(isStrict == 0) { //제약 없이 소환
            if (rand < 9900) selectedData = LowPool[Random.Range(0, LowPool.Length)];
            else if (rand < 9990) selectedData = MiddlePool[Random.Range(0, MiddlePool.Length)];
            else selectedData = HighPool[Random.Range(0, HighPool.Length)];
        }
        else { //교환 등으로 인한 제약 발생
            Debug.Log("교환 요청!");
            selectedData = LowPool[Random.Range(0, LowPool.Length)];
        }
        // 3. 소환 및 데이터 주입
        GameObject unitObj = Instantiate(unitBasePrefab, parentTile.position, Quaternion.identity, parentTile);
        unitObj.transform.localPosition = new Vector3(0, 0, -1f);
        unitObj.GetComponent<Unit>().SetUnit(selectedData);
        OnUnitAdded(unitObj.GetComponent<Unit>());
    }

    // 빈자리를 찾아 신화급 유닛 1마리를 확정 소환하는 함수
    public void SpawnMythUnit()
    {
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
            UnitData selectedData = MythPool[Random.Range(0, MythPool.Length)];
            GameObject unitObj = Instantiate(unitBasePrefab, emptyTile.position, Quaternion.identity, emptyTile);
            unitObj.transform.localPosition = new Vector3(0, 0, -1f);

            Unit newUnit = unitObj.GetComponent<Unit>();
            newUnit.SetUnit(selectedData);

            // 소환되었으니 카드 효과 트리거 검사
            OnUnitAdded(newUnit);
        }
        else
        {
            Debug.LogWarning("신화급 유닛을 소환할 빈 공간이 없습니다!");
        }
    }

    // 유닛이 필드에 새롭게 등장(소환/합성)할 때마다 호출할 트리거 함수
    public void OnUnitAdded(Unit newUnit) {
        // 1. [신화의 재림] 카드 효과 처리
        if (CardUIManager.instance != null && CardUIManager.instance.HasCard(CardEffectID.Myth_MythRebirth))
        {
            // 새로 등장한 유닛이 신화급인지 확인 (MythPool 안에 데이터가 있는지 검사)
            bool isMythic = System.Array.Exists(MythPool, data => data == newUnit.data);
            if (isMythic)
            {
                Monster[] allMonsters = FindObjectsOfType<Monster>();
                foreach (var m in allMonsters)
                {
                    if (m != null && m.monsterType != MonsterType.Boss)
                    {
                        // 현재 체력의 99%를 깎음, 공격자는 신화급 유닛 자신
                        m.TakeDamage(m.hp * 0.99f, newUnit);
                    }
                }
            }
        }

        // 2. [초월] 카드 효과 처리
        if (CardUIManager.instance != null && CardUIManager.instance.HasCard(CardEffectID.Myth_AscensionTrigger))
        {
            string targetName = newUnit.data.unitName;

            // 방금 소환된 유닛과 같은 이름의 유닛이 16마리 이상인지 검사
            if (Unit.GetUnitCount(targetName) >= 16)
            {
                Unit[] allUnits = FindObjectsOfType<Unit>();
                int removedCount = 0;

                // 딱 16마리만 찾아서 파괴
                foreach (var u in allUnits)
                {
                    if (u.data != null && u.data.unitName == targetName)
                    {
                        // UI 리스트에서 제거 (기존 합성 로직과 포맷 맞춤)
                        if (CardUIManager.instance.activeUnits.Contains(u))
                            CardUIManager.instance.activeUnits.Remove(u);
                        u.transform.SetParent(null);
                        Destroy(u.gameObject);
                        removedCount++;

                        if (removedCount >= 16) break; // 16마리 채웠으면 중단
                    }
                }

                // 전체 스탯 한번 갱신해주고 신화급 2마리 소환
                CardUIManager.instance.RefreshAllUnitStats();
                SpawnMythUnit();
                SpawnMythUnit();
            }
        }
    }

















        // 다음 라운드로 넘어가는 함수
    void NextRound()
    {
        if (currentRound < maxRound)
        {
            currentRound++;
            if (currentRound%10 != 0) //보스 라운드가 아닐때
                currentTime = roundDuration; 
            else currentTime = bossRoundDuration; //보스라운드일때
            UpdateUI();

            spawner.StartSpawn();
            Debug.Log(currentRound + " 라운드 시작!");
        }
        else
        {
            // 100라운드까지 다 깼을 때
            currentTime = 0f;
            Debug.Log("오잉 이걸 니가 봤다면 뭔가 오류가 났다는 뜻인데");
        }
    }
    public void BossKilledSettingTime() {
        if(currentRound == 100) isClear = true;
        currentTime = 5f;
        isBossDie = true;
        UpdateUI();
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
        if (DataManager.instance != null) {
            DataManager.instance.currentUser.currentRunReachedWave = currentRound;
            DataManager.instance.currentUser.isCurrentRunClear = isClear;
        }
        SceneManager.LoadScene("GameOver");
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
        coinText.text = currentCoin + "";
        elementStoneText.text = currentElementStone + "";
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

    IEnumerator GoldMineRoutine() // 황금 광산 카드 효과 적용
    {
        // 10초를 기다리기 위한 캐싱 (성능 최적화)
        WaitForSeconds waitTenSeconds = new WaitForSeconds(10f);

        while (true)
        {
            yield return waitTenSeconds;

            // 황금 광산 카드 보유 시 원소석 1개 지급
            if (CardUIManager.instance != null && CardUIManager.instance.HasCard(CardEffectID.Myth_GoldMine))
            {
                AddElementStone(1);
            }
        }
    }


    public void DebugSummonByName()
    {
        string input = debugInputField.text;
        if (string.IsNullOrEmpty(input)) return;

        // --- [추가] 숫자 입력 확인: 1~100 사이의 정수인지 체크 ---
        if (int.TryParse(input, out int targetRound) && targetRound >= 1 && targetRound <= 100)
        {
            currentRound = targetRound;
            debugInputField.text = "";
            debugInputField.ActivateInputField();
            return; // 라운드 이동 후 함수 종료
        }

        // --- 기존 유닛 소환 로직 (숫자가 아니거나 범위를 벗어난 경우) ---
        UnitData targetData = FindUnitDataByName(input);

        if (targetData != null)
        {
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
                GameObject unitObj = Instantiate(unitBasePrefab, emptyTile.position, Quaternion.identity, emptyTile);
                unitObj.transform.localPosition = new Vector3(0, 0, -1f);
                unitObj.GetComponent<Unit>().SetUnit(targetData);
                OnUnitAdded(unitObj.GetComponent<Unit>());

                Debug.Log($"[Debug] {input} 소환 완료!");
                debugInputField.text = "";
                debugInputField.ActivateInputField();
            }
            else
            {
                Debug.LogWarning("배치할 빈 공간이 없습니다!");
            }
        }
        else
        {
            Debug.LogError($"{input}이라는 유닛을 찾을 수 없거나 올바른 라운드 숫자가 아닙니다.");
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
}