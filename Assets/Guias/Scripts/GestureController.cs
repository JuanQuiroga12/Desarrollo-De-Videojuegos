using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class GestureController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform target;            // Objeto 3D a manipular
    public Camera cam;                  // Cámara para el vector cámara->objeto
    [SerializeField] private TextMeshProUGUI debugLabel;

    [Header("Sensibilidades")]
    [Tooltip("Grados de rotación Y por píxel horizontal")]
    public float rotationSpeedDegPerPixel = 0.20f;
    [Tooltip("Unidades del mundo en Y por píxel vertical")]
    public float moveYUnitsPerPixel = 0.005f;
    [Tooltip("Unidades hacia/desde la cámara por píxel de gesto (zoom)")]
    public float zoomUnitsPerPixel = 0.1f;

    [Header("Límites (opcionales)")]
    public bool clampY = true;
    public float minY = -10f, maxY = 10f;

    [Tooltip("Limitar distancia cámara->objeto")]
    public bool clampDistanceToCamera = false;
    public float minDistance = 0.5f, maxDistance = 10f;

    [Header("Gestos")]
    [Tooltip("Zona muerta para activar el zoom (en píxeles)")]
    public float twoFingerDeadzonePx = 8f;
    [Tooltip("Usar también la regla direccional (izq<- / der->)")]
    public bool directionalZoomRule = true;

    // Estado 1 dedo
    private Vector2 t0Prev;
    private bool t0HasPrev;

    // Estado 2 dedos (pinch incremental)
    private bool pinchActive;
    private float prevPinchDistPx;      // distancia entre dedos en el frame anterior
    private Vector2 aPrev, bPrev;       // para regla direccional

    private void Reset() { cam = Camera.main; }

    private void Update()
    {
        if (!target) return;

        int count = Input.touchCount;

        if (count == 0)
        {
            t0HasPrev = false;
            pinchActive = false;
            SetDebug("");
            return;
        }

        // ======= 1 DEDO: rotar Y o mover en Y =======
        if (count == 1)
        {
            Touch t0 = Input.GetTouch(0);
            if (IsTouchOverUI(t0)) return;

            if (t0.phase == UnityEngine.TouchPhase.Began)
            {
                t0Prev = t0.position;
                t0HasPrev = true;
                return;
            }

            if (t0HasPrev && (t0.phase == UnityEngine.TouchPhase.Moved || t0.phase == UnityEngine.TouchPhase.Stationary))
            {
                Vector2 delta = t0.position - t0Prev;
                t0Prev = t0.position;

                if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                {
                    // Rotación SOLO en Y
                    float degreesY = delta.x * rotationSpeedDegPerPixel;
                    target.Rotate(0f, degreesY, 0f, Space.World);
                    SetDebug($"RotateY: {degreesY:F2}°");
                }
                else
                {
                    // Movimiento SOLO en Y
                    float dy = delta.y * moveYUnitsPerPixel;
                    Vector3 pos = target.position + Vector3.up * dy;
                    if (clampY) pos.y = Mathf.Clamp(pos.y, minY, maxY);
                    target.position = pos;
                    SetDebug($"MoveY: {dy:F3}u (y={target.position.y:F2})");
                }
            }

            if (t0.phase == UnityEngine.TouchPhase.Ended || t0.phase == UnityEngine.TouchPhase.Canceled)
                t0HasPrev = false;

            // Si venías de pinch, lo cerramos
            pinchActive = false;
            return;
        }

        // ======= 2 DEDOS: ZOOM INCREMENTAL Y SIN RE-CENTRAR =======
        Touch a = Input.GetTouch(0);
        Touch b = Input.GetTouch(1);
        if (IsTouchOverUI(a) || IsTouchOverUI(b)) return;

        float currPinchDistPx = Vector2.Distance(a.position, b.position);

        // Primer frame del pinch: inicializa y NO mueve
        if (!pinchActive || a.phase == UnityEngine.TouchPhase.Began || b.phase == UnityEngine.TouchPhase.Began)
        {
            pinchActive = true;
            prevPinchDistPx = currPinchDistPx;
            aPrev = a.position;
            bPrev = b.position;
            SetDebug("Pinch start");
            return; // evita salto inicial
        }

        // Delta de este frame respecto al frame anterior (no al inicio)
        float deltaPx = currPinchDistPx - prevPinchDistPx;

        // Zona muerta
        if (Mathf.Abs(deltaPx) < twoFingerDeadzonePx)
        {
            prevPinchDistPx = currPinchDistPx;
            aPrev = a.position; bPrev = b.position;
            SetDebug("Pinch (deadzone)");
            return;
        }

        // Signo básico (pinch clásico por frame)
        float sign = Mathf.Sign(deltaPx);

        if (directionalZoomRule)
        {
            // Direccional por frame: izq->izq & der->der => acercar (+), contrario => alejar (-)
            Vector2 leftPrev = aPrev.x <= bPrev.x ? aPrev : bPrev;
            Vector2 rightPrev = aPrev.x <= bPrev.x ? bPrev : aPrev;
            Vector2 leftNow = (leftPrev == aPrev) ? a.position : b.position;
            Vector2 rightNow = (rightPrev == bPrev) ? b.position : a.position;

            float leftDx = leftNow.x - leftPrev.x;
            float rightDx = rightNow.x - rightPrev.x;

            if (Mathf.Abs(leftDx) > 0.01f && Mathf.Abs(rightDx) > 0.01f)
            {
                if (leftDx < 0f && rightDx > 0f) sign = +1f;      // expandir
                else if (leftDx > 0f && rightDx < 0f) sign = -1f; // contraer
            }
        }

        // Convertir delta de píxeles a unidades de mundo (paso incremental)
        float stepWorld = Mathf.Abs(deltaPx) * zoomUnitsPerPixel;

        // Distancia actual y dirección a lo largo del RAYO cámara->objeto (conserva offset X/Y)
        Vector3 camPos = cam.transform.position;
        Vector3 dir = (target.position - camPos).normalized;
        float currentDistance = Vector3.Distance(camPos, target.position);

        float desiredDistance = currentDistance + sign * stepWorld;

        if (clampDistanceToCamera)
            desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);

        Vector3 newPos = camPos + dir * desiredDistance;
        target.position = newPos;

        // Actualizar históricos
        prevPinchDistPx = currPinchDistPx;
        aPrev = a.position; bPrev = b.position;

        SetDebug((sign > 0 ? "Zoom In" : "Zoom Out") + $" → d={desiredDistance:F2}");
    }

    // -------- Utilidades --------
    private bool IsTouchOverUI(Touch t)
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject(t.fingerId);
    }

    private void SetDebug(string msg)
    {
        if (debugLabel) debugLabel.text = msg;
    }
}
