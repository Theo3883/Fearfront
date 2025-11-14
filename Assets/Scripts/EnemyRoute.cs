using UnityEngine;

public class EnemyRoute : MonoBehaviour
{
    private Transform[] waypoints;

    private void OnEnable()
    {
        GetWaypointsFromChildren();
    }

    private void GetWaypointsFromChildren()
    {
        waypoints = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            waypoints[i] = transform.GetChild(i);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (transform.childCount == 0)
            return;

        Gizmos.color = Color.cyan;
        
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            Transform child1 = transform.GetChild(i);
            Transform child2 = transform.GetChild(i + 1);
            if (child1 != null && child2 != null)
            {
                Gizmos.DrawLine(child1.position, child2.position);
            }
        }

        Gizmos.color = Color.green;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                Gizmos.DrawSphere(child.position, 0.3f);
            }
        }
    }

    public Transform[] GetWaypoints()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            GetWaypointsFromChildren();
        }
        return waypoints;
    }

    public bool IsValid()
    {
        return transform.childCount > 0;
    }
}
