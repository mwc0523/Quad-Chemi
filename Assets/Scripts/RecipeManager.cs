using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RecipeManager : MonoBehaviour
{
    [Header("UI 패널")]
    public GameObject recipeBoardPanel;
    public GameObject recipeDetailPanel;

    [Header("자동 생성 설정")]
    public GameObject slotPrefab;
    public Transform gridParent;

    [Header("트리 생성 설정")]
    public GameObject recipeSlotPrefab;
    public Transform treeParent;

    [Header("간선 설정")]
    public GameObject linePrefab;

    [Header("트리 설정")]
    public RectTransform lineContainer;

    private Coroutine lineRoutine;
    private bool isInitialized = false;

    // 선 그리기 정보를 저장할 리스트
    private class LineData
    {
        public Transform parentSlot;
        public Transform childSlot;
    }
    private List<LineData> pendingLines = new List<LineData>();

    void Start()
    {
        recipeBoardPanel.SetActive(false);
        recipeDetailPanel.SetActive(false);
    }

    public void ShowRecipeDetail(UnitData targetUnit)
    {
        if (targetUnit == null) return;
        if (lineRoutine != null) StopCoroutine(lineRoutine);

        recipeDetailPanel.SetActive(true);

        // 1. 기존 노드 및 선 삭제 (LineContainer는 제외!)
        foreach (Transform child in treeParent)
        {
            // 만약 이 자식이 LineContainer라면 지우지 말고 패스!
            if (child == lineContainer) continue;

            Destroy(child.gameObject);
        }

        // 선 바구니(LineContainer) '내부'에 있는 선들만 따로 비워줌
        if (lineContainer != null)
        {
            foreach (Transform line in lineContainer) Destroy(line.gameObject);
        }

        pendingLines.Clear();
        CreateTreeNode(targetUnit, treeParent);
        lineRoutine = StartCoroutine(DrawAllLinesRoutine());
    }

    GameObject CreateTreeNode(UnitData unit, Transform parent)
    {
        // [NRE 방지] 데이터나 부모가 없으면 즉시 중단
        if (unit == null || parent == null) return null;

        // 1. nodeGroup 생성
        GameObject nodeGroup = new GameObject(unit.unitName + "_Group");
        nodeGroup.transform.SetParent(parent, false);

        RectTransform rt = nodeGroup.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup vGroup = nodeGroup.AddComponent<VerticalLayoutGroup>();
        vGroup.childAlignment = TextAnchor.UpperCenter;
        vGroup.spacing = 60;
        vGroup.childControlWidth = true; vGroup.childControlHeight = true;
        vGroup.childForceExpandWidth = false; vGroup.childForceExpandHeight = false;

        // 2. RecipeSlot 생성
        GameObject slotObj = Instantiate(recipeSlotPrefab, nodeGroup.transform);
        RecipeSlot slotScript = slotObj.GetComponent<RecipeSlot>();
        if (slotScript != null) slotScript.Setup(unit);

        // 3. 재료 확인 및 재귀 생성
        UnitData matA, matB;
        if (GetRecipeMaterials(unit, out matA, out matB))
        {
            GameObject childrenContainer = new GameObject("Children");
            childrenContainer.transform.SetParent(nodeGroup.transform, false);

            RectTransform childRt = childrenContainer.AddComponent<RectTransform>();
            HorizontalLayoutGroup hGroup = childrenContainer.AddComponent<HorizontalLayoutGroup>();
            hGroup.childAlignment = TextAnchor.UpperCenter;
            hGroup.spacing = 40;
            hGroup.childControlWidth = true; hGroup.childControlHeight = true;
            hGroup.childForceExpandWidth = false; hGroup.childForceExpandHeight = false;

            // 자식 노드들을 생성하고 그 결과값을 가져옴
            GameObject childNodeA = CreateTreeNode(matA, childrenContainer.transform);
            GameObject childNodeB = CreateTreeNode(matB, childrenContainer.transform);

            // 선 그리기 예약 (자식의 첫 번째 자식인 Slot과 연결)
            if (childNodeA != null && childNodeA.transform.childCount > 0)
                pendingLines.Add(new LineData { parentSlot = slotObj.transform, childSlot = childNodeA.transform.GetChild(0) });

            if (childNodeB != null && childNodeB.transform.childCount > 0)
                pendingLines.Add(new LineData { parentSlot = slotObj.transform, childSlot = childNodeB.transform.GetChild(0) });
        }

        return nodeGroup;
    }

    IEnumerator DrawAllLinesRoutine()
    {
        // 레이아웃이 완전히 정렬될 때까지 한 프레임 대기
        yield return new WaitForEndOfFrame();

        if (lineContainer == null) yield break;

        foreach (var lineData in pendingLines)
        {
            if (lineData.parentSlot == null || lineData.childSlot == null) continue;

            GameObject lineObj = Instantiate(linePrefab, lineContainer);
            lineObj.transform.SetAsFirstSibling(); // 선을 유닛 뒤로 보냄

            DrawLineBetween(lineData.parentSlot.GetComponent<RectTransform>(),
                            lineData.childSlot.GetComponent<RectTransform>(),
                            lineObj.GetComponent<RectTransform>());
        }
    }

    void DrawLineBetween(RectTransform start, RectTransform end, RectTransform line)
    {
        if (start == null || end == null || line == null) return;

        Vector2 startPos = lineContainer.InverseTransformPoint(start.position);
        Vector2 endPos = lineContainer.InverseTransformPoint(end.position);

        Vector2 dir = endPos - startPos;
        line.anchoredPosition = startPos;
        line.sizeDelta = new Vector2(dir.magnitude, 6f);
        line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        line.pivot = new Vector2(0, 0.5f);
    }

    public bool GetRecipeMaterials(UnitData targetUnit, out UnitData matA, out UnitData matB)
    {
        matA = null; matB = null;
        if (InGameManager.instance == null || InGameManager.instance.recipes == null) return false;

        foreach (var recipe in InGameManager.instance.recipes)
        {
            if (recipe.result == targetUnit)
            {
                matA = recipe.materialA;
                matB = recipe.materialB;
                return true;
            }
        }
        return false;
    }

    // --- 이하 버튼 관련 함수는 기존과 동일하게 유지하거나 필요 시 복사 ---
    public void OnClickOpenRecipeBoard()
    {
        if (InGameManager.instance == null) return;
        recipeBoardPanel.SetActive(!recipeBoardPanel.activeSelf);
        if (recipeBoardPanel.activeSelf)
        {
            recipeDetailPanel.SetActive(false);
            if (!isInitialized) GenerateAllSlots();
        }
    }

    void GenerateAllSlots()
    {
        var mgr = InGameManager.instance;
        if (mgr == null) return;
        AddPoolToGrid(mgr.LowPool); AddPoolToGrid(mgr.MiddlePool); AddPoolToGrid(mgr.HighPool);
        AddPoolToGrid(mgr.EpicPool); AddPoolToGrid(mgr.LegendPool); AddPoolToGrid(mgr.MythPool);
        isInitialized = true;
    }

    void AddPoolToGrid(UnitData[] pool)
    {
        if (pool == null) return;
        foreach (UnitData data in pool)
        {
            if (data == null) continue;
            GameObject go = Instantiate(slotPrefab, gridParent);
            go.GetComponent<RecipeSlot>().Setup(data);
        }
    }

    public void OnClickCloseBoard() { recipeBoardPanel.SetActive(false); recipeDetailPanel.SetActive(false); }
    public void OnClickCloseDetail() { recipeDetailPanel.SetActive(false); }
}