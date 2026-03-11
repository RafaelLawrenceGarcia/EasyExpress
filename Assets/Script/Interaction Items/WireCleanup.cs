using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attached to every committed wire GameObject.
/// When the wire is destroyed (removed by the player), it clears the
/// occupied state on both ports, hides their heads, and destroys all strands.
/// </summary>
public class WireCleanup : MonoBehaviour
{
    public InspectableItem portA;
    public InspectableItem portB;

    [HideInInspector] public List<GameObject> strands = new List<GameObject>();

    void OnDestroy()
    {
        CleanPort(portA);
        CleanPort(portB);

        foreach (GameObject strand in strands)
            if (strand != null) Destroy(strand);
        strands.Clear();
    }

    void CleanPort(InspectableItem port)
    {
        if (port == null) return;
        port.isOccupied   = false;
        port.connectedTo  = null;
        port.attachedWire = null;
        if (port.wireHead != null)
        {
            port.wireHead.SetActive(false);
            WireHeadRemover remover = port.wireHead.GetComponent<WireHeadRemover>();
            if (remover != null) Destroy(remover);
            InspectableItem headItem = port.wireHead.GetComponent<InspectableItem>();
            if (headItem != null) Destroy(headItem);
        }
    }
}