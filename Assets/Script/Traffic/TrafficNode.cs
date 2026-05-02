using UnityEngine;
using System.Collections.Generic;

public class TrafficNode : MonoBehaviour
{
    // Drag other nodes here to tell the car where it can go next
    public List<TrafficNode> nextNodes;

    private void OnDrawGizmos()
    {
        // This draws lines in the editor so you can see your roads!
        Gizmos.color = Color.yellow;
        foreach (TrafficNode node in nextNodes)
        {
            if(node != null)
                Gizmos.DrawLine(transform.position, node.transform.position);
        }
    }
}