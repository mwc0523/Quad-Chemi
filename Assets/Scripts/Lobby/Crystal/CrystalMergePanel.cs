using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrystalMergePanel : MonoBehaviour
{
    [Header("UI 연결")]
    public Image[] mergeSlots; // 3개의 네모 이미지 연결 (Image, Image (1), Image (2))
    public Button mergeButton; // B_FinalMerge 연결

    [Header("결과 UI 연결")]
    public GameObject resultPanel;     // 합성 결과창 패널
    public TextMeshProUGUI resultText;            // "상급 불 결정 획득!" 등 출력
    public Button resultCloseButton;   // 결과창 닫기 버튼


    private List<CrystalPieceData> selectedMaterials = new List<CrystalPieceData>();

    void Start()
    {
        UpdateUI();
        mergeButton.onClick.AddListener(OnMergeButtonClicked);
        if (resultPanel != null) resultPanel.SetActive(false);
        gameObject.SetActive(false);
    }

    // 인벤토리에서 조각을 클릭했을 때 호출할 함수
    public void TrySelectMaterial(CrystalPieceData crystal)
    {
        // 1. 신화급은 합성 불가
        if (crystal.grade == CrystalGrade.Myth)
        {
            ShowReason("신화급 조각은 합성할 수 없습니다");
            return;
        }

        // 2. 이미 선택된 조각이면 선택 해제
        if (selectedMaterials.Contains(crystal))
        {
            selectedMaterials.Remove(crystal);
            UpdateUI();
            return;
        }

        // 3. 슬롯이 꽉 찼으면 무시
        if (selectedMaterials.Count >= 3) return;

        // 4. 동일 등급, 동일 원소 검사 (첫 번째 선택된 조각 기준)
        if (selectedMaterials.Count > 0)
        {
            var target = selectedMaterials[0];
            if (target.grade != crystal.grade || target.element != crystal.element)
            {
                ShowReason("등급과 원소가 동일한 결정만 합성 가능합니다");
                return;
            }
        }

        // 조건 통과 시 추가
        selectedMaterials.Add(crystal);
        UpdateUI();
    }

    private void UpdateUI()
    {
        for (int i = 0; i < mergeSlots.Length; i++)
        {
            if (i < selectedMaterials.Count)
            {
                mergeSlots[i].gameObject.SetActive(true);
                mergeSlots[i].color = CrystalPieceUI.GetElementColor(selectedMaterials[i].element);
            }
            else
            {
                mergeSlots[i].gameObject.SetActive(false); // 빈 슬롯 숨기기 (혹은 빈 슬롯용 이미지로 대체)
            }
        }

        // 3개가 다 모였을 때만 합성 버튼 활성화
        mergeButton.interactable = (selectedMaterials.Count == 3);
    }

    private void OnMergeButtonClicked()
    {
        // 1. 검증: 재료가 정확히 3개인지 확인
        if (selectedMaterials.Count != 3) return;

        // 2. 결과물 정보 결정
        CrystalElement elem = selectedMaterials[0].element;
        CrystalGrade currentGrade = selectedMaterials[0].grade;
        CrystalGrade resultGrade;
        bool isUpgraded;

        // [문제 2 해결] 50% 확률로 상위 등급 혹은 동일 등급 결정
        float upgradeRoll = Random.value; // 0.0 ~ 1.0 사이 값
        if (upgradeRoll < 0.5f) {
            resultGrade = CrystalDatabase.GetNextGrade(currentGrade);
            isUpgraded = true;
        }
        else {
            resultGrade = currentGrade;
            isUpgraded = false;
        }


        // [문제 1 해결] 등급에 맞는 고유 모양 인덱스 범위 가져오기
        var range = CrystalDatabase.GetIndexRange(resultGrade);
        // Random.Range(int min, int max)는 max가 exclusive이므로 +1을 해줍니다.
        int randomShapeIndex = Random.Range(range.start, range.end + 1);

        // 3. 유저 데이터 반영
        var inventory = DataManager.instance.currentUser.crystalInventory;

        // 사용한 재료 3개 삭제
        foreach (var mat in selectedMaterials)
        {
            inventory.Remove(mat);
        }

        QuestManager.instance.OnQuestProgress("weekly_crystal", 1);
        // 결정된 등급과 모양으로 새 조각 생성 및 인벤토리 추가
        CrystalPieceData newCrystal = new CrystalPieceData(randomShapeIndex, elem, resultGrade);
        inventory.Add(newCrystal);

        ShowResult(newCrystal, isUpgraded);
        selectedMaterials.Clear();
        UpdateUI();

        // 5. 서버 저장 및 인벤토리 UI 갱신
        DataManager.instance.SaveData();

        if (CrystalUIManager.Instance != null)
        {
            CrystalUIManager.Instance.RefreshInventory();
        }

        //Debug.Log($"최종 획득: {resultGrade} 등급 {elem} 결정 (모양 인덱스: {randomShapeIndex})");
    }

    private void ShowResult(CrystalPieceData resultData, bool isUpgraded)
    {
        
        if (resultPanel == null) return;
        resultPanel.SetActive(true);
        string upgradeMsg = isUpgraded ? "<color=yellow>[합성 성공!]</color>\n" : "[합성 실패]\n";
        resultText.text = $" {upgradeMsg}{resultData.grade} {resultData.element} 결정 획득!";
    }

    private void ShowReason(string t) {
        resultPanel.SetActive(true);
        resultText.text = t;
    }

    public void CloseResultPanel()
    {
        resultPanel.SetActive(false);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}