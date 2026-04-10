using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;


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

    private List<StatModifier> tideModifiers = new List<StatModifier>();

    [Header("해일 연출 설정")]
    public GameObject tidePrefab; // 해일 프리팹
    public float spawnXOffset = 15f; // 해일이 시작될 화면 밖 거리
    public float upperYPos = 2f;    // 상부 타일 라인의 Y축 중앙값
    public float lowerYPos = -2f;   // 하부 타일 라인의 Y축 중앙값

    [Header("시작 안내 패널")]
    public GameObject startPanel;
    public TextMeshProUGUI startTitleText;
    public TextMeshProUGUI startContentText;

    void Awake()
    {
        Time.timeScale = 1f;
        // 싱글톤 세팅 (이 씬 안에서 InGameManager.instance 로 이 스크립트를 부를 수 있습니다)
        if (instance == null) instance = this;
    }

    void Start()
    {
        BackgroundManager.instance.ChangeBackground(DataManager.instance.currentUser.selectedTheme); //배경 변경
        // 게임 시작 시 초기화
        currentRound = 1;
        currentTime = roundDuration;
        currentCoin = 50; // 코인 50개로 시작
        currentElementStone = 0;
        UpdateUI();
        ShowStartNoticePanel();
    }

    void ShowStartNoticePanel()
    {
        Time.timeScale = 0f;
        startPanel.SetActive(true);

        int themeIdx = DataManager.instance.currentUser.selectedTheme;
        int stageIdx = DataManager.instance.currentUser.selectedStage;

        // 0. 인덱스 안전장치 (혹시 모를 에러 방지)
        themeIdx = Mathf.Clamp(themeIdx, 0, 4);
        // 스테이지는 1~5로 들어오므로, 배열 인덱스(0~4)로 쓰기 위해 -1을 함
        int stageArrIdx = Mathf.Clamp(stageIdx - 1, 0, 4);

        string[] themeNames = { "바위산", "숲", "바다", "화산", "공허" };

        string[] themeEffects = {
        "- 가장 기본이 되는 지형입니다.",
        "- 적이 초당 1%의 체력을 회복합니다.\n- 적 처치 시 1회, 본체의 1/3 체력을 갖는 2마리로 분열합니다.",
        "- 매 라운드 밀물/썰물이 발생합니다.\n- 상부/하부 구역 네모의 공격속도가 1라운드 동안 40% 감소합니다.",
        "- 3라운드마다 필드 랜덤 3칸이 2라운드 동안 봉인됩니다.\n- 봉인된 칸의 네모는 공격 및 이동이 불가합니다.",
        "- 적이 초당 1%의 체력 회복\n- 적 처치 시 1회, 본체 1/3 체력의 2마리로 분열\n- 매 라운드 밀물/썰물이 발생\n- 상부/하부 구역 네모 공격속도가 1라운드 동안 40% 감소\n- 3라운드마다 필드 랜덤 3칸이 2라운드 동안 봉인\n- 봉인된 칸의 네모는 공격 및 이동 불가\n- 매 라운드 10% 확률로 15초 경고 후 필드 5칸 제거"
        };

        string[] stageEffects = {
        "- 기본 난이도입니다.",
        "- 적의 이동속도가 30% 증가합니다.",
        "- 소환 비용 상승량이 40% 확률로 3코인으로 증가합니다. (기본 2)",
        "- 적 처치 시 획득 코인이 40% 확률로 1코인으로 감소합니다. (기본 2)",
        "- 적 이동속도 30% 증가\n- 소환 비용 상승량 증가 확률 적용 (40% 확률로 +3)\n- 적 처치 코인 감소 확률 적용 (40% 확률로 1코인)\n- 보스가 5초마다 맵의 랜덤 위치로 이동"
        };

        startTitleText.text = $"<color=#FFD700>{themeNames[themeIdx]} / {stageIdx}단계</color>";

        startContentText.text = $"<b><color=#FF6666>[테마 효과]</color></b>\n{themeEffects[themeIdx]}\n\n" +
                                $"<b><color=#6666FF>[스테이지 시련]</color></b>\n{stageEffects[stageArrIdx]}";
    }

    public void OnClickStartPanel()
    {
        startPanel.SetActive(false);

        // SpeedControl에서 설정한 배속으로 시작 (위에서 수정한대로 1배속)
        Time.timeScale = SpeedControl.GetFast();

        // 이제 게임 시작
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
                Unit target = hit.collider.GetComponent<Unit>();
                if (target != null)
                {
                    // ★ 추가: 봉인된 타일의 유닛인지 확인
                    BuildTile bt = target.GetComponentInParent<BuildTile>();
                    if (bt != null && bt.isSealed)
                    {
                        Debug.Log("이 유닛은 봉인되어 움직일 수 없습니다!");
                        return; // 드래그 시작 방지
                    }

                    draggingUnit = target;
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
        if (sr != null) sr.sortingOrder = 0;

        Collider2D[] hits = Physics2D.OverlapCircleAll(mousePos, 0.1f);

        Transform targetTile = null;
        Unit otherUnit = null;

        // 1. 목표 타일 및 그 위의 유닛 탐색
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

        // 2. [추가] 봉인 체크: 목표 타일이 봉인되어 있다면 즉시 되돌리기
        if (targetTile != null)
        {
            BuildTile bt = targetTile.GetComponent<BuildTile>();
            if (bt != null && bt.isSealed)
            {
                Debug.Log("봉인된 타일입니다! 이동/합성 불가.");
                ReturnUnit();
                draggingUnit = null;
                return; // 여기서 함수를 종료하여 아래 합성/이동 로직이 실행되지 않게 함
            }
        }

        // 3. 합성 로직 (봉인이 아님이 확인된 경우에만 실행됨)
        if (otherUnit != null)
        {
            UnitData result = GetMergeResult(draggingUnit.data, otherUnit.data);

            if (result != null)
            {
                otherUnit.gameObject.SetActive(false);
                draggingUnit.gameObject.SetActive(false);

                CardUIManager.instance.activeUnits.Remove(otherUnit);
                CardUIManager.instance.activeUnits.Remove(draggingUnit);

                Transform tile = otherUnit.transform.parent;
                GameObject obj = Instantiate(unitBasePrefab, tile.position, Quaternion.identity, tile);
                obj.transform.localPosition = new Vector3(0, 0, -1);

                Unit newUnit = obj.GetComponent<Unit>();
                newUnit.SetUnit(result);
                if (CardUIManager.instance.HasCard(CardEffectID.Mid_ElementReverse)) AddCoin(5);
                OnUnitAdded(newUnit);

                if (!CardUIManager.instance.activeUnits.Contains(newUnit))
                    CardUIManager.instance.activeUnits.Add(newUnit);

                CardUIManager.instance.RefreshAllUnitStats();

                Destroy(otherUnit.gameObject);
                Destroy(draggingUnit.gameObject);
                draggingUnit = null;
                return;
            }
        }

        // 4. 이동 로직 (봉인이 아님이 확인된 경우에만 실행됨)
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

                BuildTile bt = unitOnTile.GetComponentInParent<BuildTile>();
                if (bt != null) bt.CheckUnitStatus();
            }

            BuildTile newTileBT = draggingUnit.GetComponentInParent<BuildTile>();
            if (newTileBT != null) newTileBT.CheckUnitStatus();
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
                Unit unitOnTile = tile.GetComponentInChildren<Unit>();
                if (unitOnTile == null)
                {
                    targetTile = tile;
                    break;
                }
            }

            if (targetTile != null)
            {
                currentCoin -= summonFee;

                // 기본 상승량은 2
                int increaseAmount = 2;

                // 3, 5 스테이지라면 40% 확률로 상승량이 3으로 변경
                int stage = DataManager.instance.currentUser.selectedStage;
                if (stage == 3 || stage == 5)
                {
                    if (Random.value < 0.4f)
                    {
                        increaseAmount = 3;
                        Debug.Log("비용 추가 상승 발생! (+3)");
                    }
                }

                summonFee += increaseAmount;

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
            Unit unitOnTile = tile.GetComponentInChildren<Unit>();
            if (unitOnTile == null)
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
        BuildTile bt = newUnit.GetComponentInParent<BuildTile>();
        if (bt != null)
        {
            bt.CheckUnitStatus();
        }
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
        if (currentRound >= maxRound) return;

        currentRound++;
        currentTime = (currentRound % 10 == 0) ? bossRoundDuration : roundDuration;
        UpdateUI();
        spawner.StartSpawn();

        // 테마 기믹 실행
        ApplyThemeGimmicks();
    }
    void ApplyThemeGimmicks()
    {
        int theme = DataManager.instance.currentUser.selectedTheme;

        // 1. 바다 & 공허 (밀물/썰물: 공속 90% 감소)
        if (theme == 2 || theme == 4)
        {
            HandleTideEffect();
        }

        // 2. 화산 & 공허 (3라운드마다 3칸 봉인)
        if (theme == 3 || theme == 4)
        {
            // 기존 봉인 감소
            foreach (var tile in mapManager.buildTileScripts) tile.ReduceSeal();

            // 3라운드마다 새 봉인
            if (currentRound % 3 == 0)
            {
                SealRandomTiles(3, 2);
            }
        }

        // 3. 공허 전용 (10% 확률로 5칸 제거)
        if (theme == 4)
        {
            if (Random.value < 0.1f)
            {
                StartCoroutine(VoidDestructionSequence(5, 15f));
            }
        }
    }

    // --- 기믹 세부 구현 함수들 ---

    void HandleTideEffect()
    {
        // 1. 모든 활성화된 유닛으로부터 "TideSystem" 소스의 모든 모디파이어를 제거
        // (이렇게 하면 이전 라운드에 어디에 있었든 디버프가 완전히 초기화됩니다)
        foreach (var unit in CardUIManager.instance.activeUnits)
        {
            if (unit != null && unit.combatStats != null)
            {
                unit.combatStats.RemoveModifiersFromSource("TideSystem");
            }
        }

        // 2. 현재 라운드에 맞는 구역 설정
        // 짝수 라운드: 위쪽(0~7), 홀수 라운드: 아래쪽(8~15)
        bool isHighTide = (currentRound % 2 == 0);

        if (tidePrefab != null)
        {
            Vector3 spawnPos;
            Vector3 moveDir;

            if (isHighTide) // 상부: 왼쪽 -> 오른쪽
            {
                spawnPos = new Vector3(-spawnXOffset, upperYPos, -5f);
                moveDir = Vector2.right;
            }
            else // 하부: 오른쪽 -> 왼쪽
            {
                spawnPos = new Vector3(spawnXOffset, lowerYPos, -5f);
                moveDir = Vector2.left;
            }

            GameObject tide = Instantiate(tidePrefab, spawnPos, Quaternion.identity);
            tide.GetComponent<TideEffect>().Setup(moveDir);
        }

        int startIdx = isHighTide ? 0 : 8;
        int endIdx = isHighTide ? 8 : 16;

        // 3. 해당 구역의 타일에 있는 유닛에게만 새로 디버프 부여
        for (int i = startIdx; i < endIdx; i++)
        {
            // mapManager.buildTileScripts가 정확히 16개(0~15)라고 가정
            if (i >= mapManager.buildTileScripts.Count) break;

            BuildTile bt = mapManager.buildTileScripts[i];
            Unit unit = bt.GetComponentInChildren<Unit>();

            if (unit != null)
            {
                // source를 "TideSystem"으로 박아서 생성
                unit.combatStats.AddModifier(new StatModifier(
                    StatType.AttackSpeed,
                    -0.4f,
                    StatModifierType.PercentAdd,
                    "TideSystem"
                ));
            }
        }
    }

    void SealRandomTiles(int count, int duration)
    {
        // 1. 리스트가 비어있는지 먼저 체크 (방어적 프로그래밍)
        if (mapManager.buildTileScripts == null || mapManager.buildTileScripts.Count == 0)
        {
            Debug.LogWarning("봉인할 타일 리스트가 비어있습니다!");
            return;
        }

        List<BuildTile> candidates = new List<BuildTile>(mapManager.buildTileScripts);

        // 2. 안전한 Fisher-Yates 셔플
        int n = candidates.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            BuildTile value = candidates[k];
            candidates[k] = candidates[n];
            candidates[n] = value;
        }

        // 3. 앞에서부터 count만큼 봉인 실행
        int actualCount = Mathf.Min(count, candidates.Count);
        for (int i = 0; i < actualCount; i++)
        {
            if (candidates[i] != null)
            {
                candidates[i].SetSeal(duration);
                candidates[i].CheckUnitStatus();
            }
        }
    }

    IEnumerator VoidDestructionSequence(int count, float delay)
    {
        // 1. 방어적 프로그래밍: 리스트가 비어있는지 확인
        if (mapManager.buildTileScripts == null || mapManager.buildTileScripts.Count == 0)
        {
            Debug.LogWarning("제거할 타일 리스트가 비어있습니다!");
            yield break;
        }

        // 2. 인덱스 리스트 생성
        int tileCount = mapManager.buildTileScripts.Count;
        List<int> indices = new List<int>();
        for (int i = 0; i < tileCount; i++)
        {
            indices.Add(i);
        }

        // 3. 안전한 Fisher-Yates 셔플 (무한 루프 방지)
        for (int i = tileCount - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            int tmp = indices[i];
            indices[i] = indices[r];
            indices[r] = tmp;
        }

        // 4. 결정된 인덱스들로 경고 루틴 실행
        int actualCount = Mathf.Min(count, indices.Count);
        for (int i = 0; i < actualCount; i++)
        {
            int targetIndex = indices[i];

            // 인덱스가 유효한지 한 번 더 확인 후 실행
            if (targetIndex >= 0 && targetIndex < mapManager.buildTileScripts.Count)
            {
                BuildTile tile = mapManager.buildTileScripts[targetIndex];
                if (tile != null)
                {
                    StartCoroutine(tile.VoidWarningRoutine(delay));
                }
            }
        }

        yield break;
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
                Unit unitOnTile = tile.GetComponentInChildren<Unit>();
                if (unitOnTile == null)
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