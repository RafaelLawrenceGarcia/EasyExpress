using UnityEngine;

/// <summary>
/// Attached to every committed wire GameObject.
/// When the wire is destroyed (removed by the player), it clears the
/// occupied state on both ports it was connected to.
/// </summary>
public class WireCleanup : MonoBehaviour
{
    public InspectableItem portA;
    public InspectableItem portB;

    void OnDestroy()
    {
        if (portA != null)
        {
            portA.isOccupied   = false;
            portA.connectedTo  = null;
            portA.attachedWire = null;
        }
        if (portB != null)
        {
            portB.isOccupied   = false;
            portB.connectedTo  = null;
            portB.attachedWire = null;
        }
    }
}