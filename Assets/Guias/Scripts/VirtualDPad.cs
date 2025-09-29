using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VirtualDPad : MonoBehaviour
{

    public TextMeshProUGUI directionText, touchcount;

    private Touch theTouch;
    private Vector2 touchStartPosition, touchEndPosition;
    private string direction;

    void Update()
    {
        if (Input.touchCount > 0)
        {
            theTouch = Input.GetTouch(0);

            if (theTouch.phase == UnityEngine.TouchPhase.Began)
            {
                touchStartPosition = theTouch.position;
            }

            else if (theTouch.phase == UnityEngine.TouchPhase.Moved || theTouch.phase == UnityEngine.TouchPhase.Ended)
            {
                touchEndPosition = theTouch.position;

                float x = touchEndPosition.x - touchStartPosition.x;
                float y = touchEndPosition.y - touchStartPosition.y;

                if (Mathf.Abs(x) == 0 && Mathf.Abs(y) == 0)
                {
                    direction = "Tapped";
                }

                else if (Mathf.Abs(x) > Mathf.Abs(y))
                {
                    direction = x > 0 ? "Right" : "Left";
                }

                else
                {
                    direction = y > 0 ? "Up" : "Down";
                }
            }
        }

        directionText.SetText(direction);
        touchcount.SetText(Input.touchCount.ToString());
    }
}
