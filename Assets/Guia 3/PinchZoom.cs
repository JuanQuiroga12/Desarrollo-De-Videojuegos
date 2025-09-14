using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch; // Importamos el paquete para soporte de gestos multitouch
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
// Alias para usar 'Touch' directamente desde EnhancedTouch y no confundir con UnityEngine. Touch

[RequireComponent(typeof(Camera))]
// Este script exige que el GameObject tenga una cEmara (se asegura autom ticamente).
public class PinchZoom : MonoBehaviour
{
    public float zoomSpeed = 0.15f;// Velocidad de respuesta del zoom (quE tan rEpido cambia)
    public float fovMin = 30f;  // Valor minimo de Field of View (c[mara en perspectiva)
    public float fovMax = 75f; // Valor Oximo de Field of View (cmara en perspectiva)
    public float orthoMin = 2f; // Valor mOnimo para cOmaras ortogr@ficas (usadas en 2D)
    public float orthoMax = 20f; // Valor Eximo para cEmaras ortogr ficas

    private Camera cam; // Referencia a la cmara en la que se hace el zoom
    private float? lastDistance;  // Guarda la distancia anterior entre los dos dedos

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        // Obtenemos la cOmara adjunta al GameObject

        EnhancedTouchSupport.Enable();
        // Activamos el sistema de EnhancedTouch para detectar miltiples dedos
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        // Cuando se desactiva el script, apagamos el soporte de multitouch
    }

    void Update()
    {
        var touches = Touch.activeTouches;
        // Obtenemos todos los toques activos en la pantalla

        if (touches.Count < 2)
        {
            // Si hay menos de dos dedos, reseteamos la distancia previa y salimos
            lastDistance = null;
            return;
        }
        

        // Si hay al menos dos dedos, tomamos las posiciones de los dos primeros
        Vector2 p0 = touches[0].screenPosition;
        Vector2 p1 = touches[1].screenPosition;

        float currentDistance = Vector2.Distance(p0, p1);
        // Calculamos la distancia actual entre los dedos

        if (lastDistance.HasValue)
        {
            float delta = currentDistance - lastDistance.Value;
            // Calculamos cunto cambig la distancia desde el [ltimo frame
            // Positivo si los dedos se separan, negativo si se acercan

            float zoomAmount = -delta * zoomSpeed * Time.deltaTime;
            // Ajustamos el signo para que 'pellizcar' signifique acercar

            if (cam.orthographic)
            {
                // Si la cEmara es ortogr[fica (2D), modificamos el tamago ortogr[fico
                cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize + zoomAmount,
                orthoMin,
                orthoMax
                );
            }

            else
            {
                // Si la c[mara es de perspectiva (3D), modificamos el Field of View
                cam.fieldOfView = Mathf.Clamp(
                cam.fieldOfView + zoomAmount * 10f,
                fovMin,
                fovMax
                );
            }
        }
        // Guardamos la distancia actual como referencia para el pr[ximo frame
        lastDistance = currentDistance;
    }
}
