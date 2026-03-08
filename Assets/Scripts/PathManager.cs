using UnityEngine;
using System.Collections.Generic;

public class PathManager : MonoBehaviour
{
    public List<Transform> waypoints = new List<Transform>();

    // Scene 뷰에서 경로를 선으로 보여줍니다. (디버깅용)
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }

        // 마지막 포인트와 첫 포인트 연결 (8자 순환 확인용)
        if (waypoints[waypoints.Count - 1] != null && waypoints[0] != null)
            Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
    }
}