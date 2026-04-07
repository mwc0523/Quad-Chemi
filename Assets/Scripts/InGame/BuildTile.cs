using UnityEngine;
using System.Collections;

public class BuildTile : MonoBehaviour
{
    public bool isSealed = false; // 화산/공허 봉인 여부
    public int sealRemainRound = 0; // 남은 봉인 라운드

    [Header("시각 효과")]
    public GameObject sealEffect; // 봉인 시 보여줄 이펙트 (얼음이나 쇠사슬 등)
    public GameObject warningEffectPrefab; // 공허 제거 경고용 빨간 원 프리맵

    private GameObject currentWarningEffect;

    // 타일에 유닛이 들어오거나 이미 있는 경우 체크
    public void CheckUnitStatus()
    {
        Unit unit = GetComponentInChildren<Unit>();
        if (unit != null)
        {
            unit.isStunned = isSealed; // 봉인 여부에 따라 스턴 설정
        }
    }

    public void SetSeal(int duration)
    {
        isSealed = true;
        sealRemainRound = duration;

        if (sealEffect != null)
        {
            if (sealEffect.scene.name != null) // 씬에 이미 있는 경우
            {
                sealEffect.SetActive(true);
            }
            else // 프리팹인 경우
            {
                GameObject eff = Instantiate(sealEffect, transform.position, Quaternion.identity, transform);
                eff.name = "ActiveSealEffect"; // 이름 변경 (구분용)
                eff.transform.localPosition = new Vector3(0, 0, -2f);
                eff.SetActive(true); // 생성 즉시 활성화
                sealEffect = eff;
            }
        }

        // 봉인 즉시 타일에 있는 유닛 상태 업데이트
        CheckUnitStatus();
    }

    public void ReduceSeal()
    {
        if (sealRemainRound > 0) sealRemainRound--;
        if (sealRemainRound <= 0)
        {
            isSealed = false;
            if (sealEffect != null) sealEffect.SetActive(false);
            CheckUnitStatus(); //봉인 풀릴때 한번더 체크
        }
    }

    public IEnumerator VoidWarningRoutine(float duration)
    {
        if (warningEffectPrefab == null) yield break;

        currentWarningEffect = Instantiate(warningEffectPrefab, transform.position, Quaternion.identity, transform);
        currentWarningEffect.transform.localPosition = new Vector3(0, 0, -2f);

        SpriteRenderer warningSR = currentWarningEffect.GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        Vector3 initialScale = currentWarningEffect.transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration; // 0에서 1로 진행

            // 1. 점점 커지는 연출 (시간이 갈수록 최대 1.5배까지)
            float scaleMultiplier = 0.2f + (progress * 0.5f);

            // 2. 깜빡거리는 연출 (Sin 함수 사용)
            // progress를 곱해서 시간이 갈수록 깜빡임 속도를 빠르게 조절 가능
            float blinkSpeed = 5f + (progress * 15f); // 뒤로 갈수록 빨라짐
            float alpha = 0.3f + Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed)) * 0.7f;

            // 값 적용
            currentWarningEffect.transform.localScale = initialScale * scaleMultiplier;
            if (warningSR != null)
            {
                Color c = warningSR.color;
                c.a = alpha;
                warningSR.color = c;
            }

            yield return null; // 다음 프레임까지 대기
        }

        // --- 유닛 제거 로직 ---
        Unit unit = GetComponentInChildren<Unit>();
        if (unit != null)
        {
            if (CardUIManager.instance.activeUnits.Contains(unit))
                CardUIManager.instance.activeUnits.Remove(unit);

            // 제거 이펙트가 있다면 여기서 실행 (예: 파티클)
            Destroy(unit.gameObject);
            CardUIManager.instance.RefreshAllUnitStats();
            Debug.Log("공허에 의해 유닛이 소멸되었습니다.");
        }

        if (currentWarningEffect != null) Destroy(currentWarningEffect);
    }
}