using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class DamageMeterManager : MonoBehaviour
{
    public GameObject entryPrefab;
    public Transform contentParent;
    private bool isUIActive = false;
    public RectTransform panelRect;
    private float updateInterval = 0.5f; // 0.5초마다 갱신
    private float timer = 0f;

    // 매 프레임 혹은 짧은 주기마다 UI 갱신
    void Update()
    {

        if (!isUIActive) return; // UI가 닫혀있으면 계산 안 함

        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            UpdateUI();
            timer = 0f;
        }
    }

    void UpdateUI()
    {
        // 1. 필드 유닛 가져오기 및 정렬
        var activeUnits = FindObjectsOfType<Unit>()
            .OrderByDescending(u => u.stats.totalDamage)
            .ToList();

        if (activeUnits.Count == 0) return;

        // 2. 전체 데미지 총합 계산
        float totalDamageSum = activeUnits.Sum(u => u.stats.totalDamage);

        // 3. UI 항목 개수 조절 (중요: 여기서 개수를 먼저 맞춥니다)

        // 부족하면 생성
        while (contentParent.childCount < activeUnits.Count)
        {
            GameObject newEntry = Instantiate(entryPrefab, contentParent);

            // 생성 직후 좌표와 스케일을 강제로 초기화하여 부모(Content) 안에 안착시킴
            RectTransform rect = newEntry.GetComponent<RectTransform>();
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            // 추가: UI가 레이아웃 그룹에 의해 정렬되도록 함
            newEntry.SetActive(true);
        }

        // 많으면 삭제 (리스트의 끝에서부터 하나씩 제거)
        if (contentParent.childCount > activeUnits.Count)
        {
            int diff = contentParent.childCount - activeUnits.Count;
            for (int i = 0; i < diff; i++)
            {
                // GetChild(0)을 삭제하면 인덱스가 꼬일 수 있으므로 마지막 자식을 삭제합니다.
                Transform lastChild = contentParent.GetChild(contentParent.childCount - 1);

                // 중요: Destroy는 다음 프레임에 삭제되므로, 
                // 로직에서 제외하기 위해 부모 관계를 먼저 끊어버립니다.
                lastChild.SetParent(null);
                Destroy(lastChild.gameObject);
            }
        }

        // 4. 데이터 업데이트
        for (int i = 0; i < activeUnits.Count; i++)
        {
            DamageEntry entry = contentParent.GetChild(i).GetComponent<DamageEntry>();
            if (entry == null) continue;

            float ratio = (totalDamageSum > 0) ? (activeUnits[i].stats.totalDamage / totalDamageSum) : 0;

            // 데이터 전달
            entry.SetData(
                activeUnits[i].data.unitName,
                activeUnits[i].data.unitSprite,
                activeUnits[i].stats.totalDamage,
                activeUnits[i].stats.killCount,
                ratio
            );
        }
    }


    public void ToggleDamageMeter()
    {
        isUIActive = !isUIActive;

        //위치 이동
        float targetX = isUIActive ? 0 : 600f; // 패널 너비만큼 이동
        panelRect.anchoredPosition = new Vector2(targetX, panelRect.anchoredPosition.y);
    }
}