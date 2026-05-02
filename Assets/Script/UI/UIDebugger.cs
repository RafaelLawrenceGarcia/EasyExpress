using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIDebugger : MonoBehaviour
{
    void Update()
    {
        // When you click the left mouse button...
        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            
            if (results.Count > 0)
            {
                // This will print EXACTLY what invisible thing your mouse just hit!
                Debug.Log("<color=yellow>YOUR MOUSE JUST CLICKED ON: </color>" + results[0].gameObject.name);
            }
            else
            {
                Debug.Log("<color=red>YOUR MOUSE CLICKED ON NOTHING (UI is completely ignoring the mouse)</color>");
            }
        }
    }
}