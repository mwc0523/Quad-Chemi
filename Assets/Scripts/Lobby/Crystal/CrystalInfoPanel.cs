using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용 권장

public class CrystalInfoPanel : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI primaryStatText;
    public TextMeshProUGUI secondaryStatText;
    public TextMeshProUGUI allStatText;
    private CrystalPieceData currentData;
    public Button rotateButton;

    public void SetupAndShow(CrystalPieceData data)
    {
        currentData = data;
        gameObject.SetActive(true);
        nameText.gameObject.SetActive(true);
        primaryStatText.gameObject.SetActive(true);
        secondaryStatText.gameObject.SetActive(true);
        allStatText.gameObject.SetActive(false);
        rotateButton.gameObject.SetActive(true);

        // 1. 이름 세팅 (예: "전설급 물의 결정")
        string gradeStr = GetGradeString(data.grade);
        string elementStr = GetElementString(data.element);
        nameText.text = $"{gradeStr} {elementStr}의 결정";
        nameText.color = Color.Lerp(CrystalPieceUI.GetElementColor(data.element), Color.white, 0.7f); // 글자색을 원소색으로

        // 2. 능력치 텍스트 파싱 (기획서 반영)
        primaryStatText.text = GetPrimaryStatText(data);
        secondaryStatText.text = GetSecondaryStatText(data);

        // 서브 스탯이 없으면(하, 중, 상 등급) 텍스트 숨기기
        secondaryStatText.gameObject.SetActive(data.grade >= CrystalGrade.Epic);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    // --- 기획서 기반 텍스트 매핑 ---
    private string GetPrimaryStatText(CrystalPieceData data)
    {
        return data.element switch
        {
            CrystalElement.Fire => $"전체 공격력 증가 <color=#FFD700>{data.primaryValue}%</color>",
            CrystalElement.Water => $"공격 시 0.5초간 적 이동속도 <color=#FFD700>{data.primaryValue}%</color> 감소",
            CrystalElement.Earth => $"모든 스턴 지속 시간 <color=#FFD700>{data.primaryValue}초</color> 증가",
            CrystalElement.Air => $"전체 공격 속도 증가 <color=#FFD700>{data.primaryValue}%</color>",
            CrystalElement.Prism => $"인접한 모든 원소 결정 효과 <color=#FFD700>{data.primaryValue}배</color> 적용",
            _ => "능력치 없음"
        };
    } 

    private string GetSecondaryStatText(CrystalPieceData data)
    {
        if (data.grade < CrystalGrade.Epic) return ""; // 에픽 미만은 두 번째 옵션 없음

        return data.element switch
        {
            CrystalElement.Fire => $"평타 공격마다 적에게 추가 데미지 <color=#FFD700>{data.secondaryValue}%</color>",
            CrystalElement.Water => $"스킬 사용 확률 <color=#FFD700>{data.secondaryValue}%</color> 증가",
            CrystalElement.Earth => $"치명타 피해량 <color=#FFD700>{data.secondaryValue}%</color> 증가",
            CrystalElement.Air => $"치명타 확률 <color=#FFD700>{data.secondaryValue}%</color> 증가",
            CrystalElement.Prism => "", // 프리즘은 서브스탯 없음
            _ => ""
        };
    }

    // 편의용 한글 변환
    private string GetGradeString(CrystalGrade grade)
    {
        return grade switch
        {
            CrystalGrade.Low => "하급",
            CrystalGrade.Middle => "중급",
            CrystalGrade.High => "상급",
            CrystalGrade.Epic => "서사급",
            CrystalGrade.Legend => "전설급",
            CrystalGrade.Myth => "신화급",
            _ => ""
        };
    }

    private string GetElementString(CrystalElement element)
    {
        return element switch
        {
            CrystalElement.Fire => "불",
            CrystalElement.Water => "물",
            CrystalElement.Earth => "땅",
            CrystalElement.Air => "공기",
            CrystalElement.Prism => "프리즘",
            _ => "무속성"
        };
    }

    public void ShowAllStat() {
        gameObject.SetActive(true);
        nameText.gameObject.SetActive(false);
        primaryStatText.gameObject.SetActive(false);
        secondaryStatText.gameObject.SetActive(false);
        allStatText.gameObject.SetActive(true);
        rotateButton.gameObject.SetActive(false);

        if (InGameCrystalManager.Instance == null)
        {
            Debug.LogError("InGameCrystalManager 인스턴스를 찾을 수 없습니다!");
            return;
        }

        // 2. 텍스트 UI가 연결되어 있는지 확인 (변수명은 본인 코드에 맞게 수정)
        if (allStatText == null)
        {
            Debug.LogError("allStatText가 인스펙터에서 연결되지 않았습니다!");
            return;
        }

        allStatText.text = InGameCrystalManager.Instance.GetLobbyBuffSummary();
    }

    public void OnRotateButtonClicked()
    {
        if (currentData == null) return;

        // 1. 회전 값 변경 (0 -> 1 -> 2 -> 3 -> 0)
        currentData.rotationCount = (currentData.rotationCount + 1) % 4;

        // 2. 서버/로컬 데이터 저장
        DataManager.instance.SaveData();

        // 인벤토리 전체 갱신 (배치용 프리뷰 등 포함)
        if (CrystalUIManager.Instance != null)
        {
            CrystalUIManager.Instance.RefreshInventory();
        }

        Debug.Log($"조각 회전 완료: 현재 {currentData.rotationCount * 90}도");
    }
}