using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using TMPro;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class GestureControllerNewInputSystem : MonoBehaviour
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
    private float prevPinchDistPx;
    private Vector2 aPrev, bPrev;

    private void Awake()
    {
        // Asegurar que la cámara esté asignada
        if (cam == null)
            cam = Camera.main;
    }

    private void OnEnable()
    {
        // CRÍTICO: Activar Enhanced Touch Support
        TouchSimulation.Enable();
        EnhancedTouchSupport.Enable();

        Debug.Log("GestureController: Enhanced Touch habilitado");
    }

    private void OnDisable()
    {
        TouchSimulation.Disable();
        EnhancedTouchSupport.Disable();

        Debug.Log("GestureController: Enhanced Touch deshabilitado");
    }

    private void Update()
    {
        if (!target)
        {
            if (debugLabel) debugLabel.text = "Sin target!";
            return;
        }

        // Usar el sistema de Touch del Input System
        var touches = Touch.activeTouches;
        int count = touches.Count;

        // Debug para móvil
        if (debugLabel)
        {
            debugLabel.text = $"Toques: {count}";
        }

        if (count == 0)
        {
            t0HasPrev = false;
            pinchActive = false;
            return;
        }

        // ======= 1 DEDO: rotar Y o mover en Y =======
        if (count == 1)
        {
            var t0 = touches[0];

            // Verificar si el toque está sobre UI
            if (IsPointerOverUI(t0.screenPosition))
            {
                SetDebug("Sobre UI");
                return;
            }

            if (t0.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                t0Prev = t0.screenPosition;
                t0HasPrev = true;
                SetDebug("Toque iniciado");
                return;
            }

            if (t0HasPrev && (t0.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                             t0.phase == UnityEngine.InputSystem.TouchPhase.Stationary))
            {
                Vector2 delta = t0.screenPosition - t0Prev;
                t0Prev = t0.screenPosition;

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

            if (t0.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                t0.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                t0HasPrev = false;
                SetDebug("Toque terminado");
            }

            pinchActive = false;
            return;
        }

        // ======= 2 DEDOS: ZOOM INCREMENTAL =======
        if (count >= 2)
        {
            var a = touches[0];
            var b = touches[1];

            if (IsPointerOverUI(a.screenPosition) || IsPointerOverUI(b.screenPosition))
            {
                SetDebug("Pinch sobre UI");
                return;
            }

            float currPinchDistPx = Vector2.Distance(a.screenPosition, b.screenPosition);

            // Primer frame del pinch: inicializa y NO mueve
            if (!pinchActive || a.phase == UnityEngine.InputSystem.TouchPhase.Began ||
                b.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                pinchActive = true;
                prevPinchDistPx = currPinchDistPx;
                aPrev = a.screenPosition;
                bPrev = b.screenPosition;
                SetDebug("Pinch iniciado");
                return;
            }

            // Delta de este frame respecto al frame anterior
            float deltaPx = currPinchDistPx - prevPinchDistPx;

            // Zona muerta
            if (Mathf.Abs(deltaPx) < twoFingerDeadzonePx)
            {
                prevPinchDistPx = currPinchDistPx;
                aPrev = a.screenPosition;
                bPrev = b.screenPosition;
                SetDebug("Pinch (zona muerta)");
                return;
            }

            // Signo básico (pinch clásico por frame)
            float sign = Mathf.Sign(deltaPx);

            if (directionalZoomRule)
            {
                // Direccional por frame
                Vector2 leftPrev = aPrev.x <= bPrev.x ? aPrev : bPrev;
                Vector2 rightPrev = aPrev.x <= bPrev.x ? bPrev : aPrev;
                Vector2 leftNow = (leftPrev == aPrev) ? a.screenPosition : b.screenPosition;
                Vector2 rightNow = (rightPrev == bPrev) ? b.screenPosition : a.screenPosition;

                float leftDx = leftNow.x - leftPrev.x;
                float rightDx = rightNow.x - rightPrev.x;

                if (Mathf.Abs(leftDx) > 0.01f && Mathf.Abs(rightDx) > 0.01f)
                {
                    if (leftDx < 0f && rightDx > 0f) sign = +1f;      // expandir
                    else if (leftDx > 0f && rightDx < 0f) sign = -1f; // contraer
                }
            }

            // Convertir delta de píxeles a unidades de mundo
            float stepWorld = Mathf.Abs(deltaPx) * zoomUnitsPerPixel;

            // Distancia actual y dirección
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
            aPrev = a.screenPosition;
            bPrev = b.screenPosition;

            SetDebug((sign > 0 ? "Zoom In" : "Zoom Out") + $" → d={desiredDistance:F2}");
        }
    }

    // Utilidad para verificar si el pointer está sobre UI
    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        // Para debugging, mostrar qué elementos UI se detectan
        if (EventSystem.current != null)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = screenPosition;

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            // Debug: mostrar qué elementos UI se detectan
            if (results.Count > 0)
            {
                Debug.Log($"UI detectada: {results[0].gameObject.name}");
                SetDebug($"UI: {results[0].gameObject.name}");
            }

            return results.Count > 0;
        }

        return false;
    }

    private void SetDebug(string msg)
    {
        if (debugLabel)
        {
            debugLabel.text = msg;
        }

        // También log para debugging en build
        Debug.Log($"Gesture: {msg}");
    }
}