using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class TouchPhase : MonoBehaviour
{
    public TextMeshProUGUI PhaseDisplayText;
    private Touch theTouch;
    private float timeTouchEnded;
    private float displayTime = .5f;

    // Update is called once per frame
    void Update()
    {

        if (Input.touchCount > 0)
        {
            theTouch = Input.GetTouch(0);
            PhaseDisplayText.SetText(theTouch.phase.ToString());
            if (theTouch.phase == UnityEngine.TouchPhase.Ended)
            {
                timeTouchEnded = Time.time;
            }
        }

        else if (Time.time - timeTouchEnded > displayTime)
        {
            PhaseDisplayText.SetText("");
        }
    }
}
