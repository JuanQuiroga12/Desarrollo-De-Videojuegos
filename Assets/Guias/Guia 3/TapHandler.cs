using UnityEngine;
using UnityEngine.InputSystem;
// Importamos UnityEngine para trabajar en Unity
// e InputSystem para usar el nuevo sistema de entrada (toques, botones, etc.)

public class TapHandler : MonoBehaviour
{
    [Header("Arrastra desde el .inputactions")]
    public InputActionReference tapAction;
    // Accign que detecta el toque (Tap). Se configura en el asset de Input Actions.

    public InputActionReference pointerPositionAction;
    // Accign que nos da la posicign del dedo (o mouse) en la pantalla.

    [Header("Opcional")]
    public Camera cam;
    // cmara a usar para convertir la posician en pantalla a un rayo.
    // Si no se asigna, usar Camera.main.

    public LayerMask raycastLayers = ~0;
    // Permite filtrar en que capas queremos detectar el toque.
    // ~0 significa ftodas las capasT.

    void OnEnable()
    {

        // Se ejecuta cuando el objeto se activa en la escena.

        if (tapAction != null)
        {
            tapAction.action.performed += OnTap;
            // Nos suscribimos al evento "performed" para ejecutar el matodo OnTap.
            tapAction.action.Enable();
            // Activamos la accign para que empiece a escuchar entradas.
        }

        if (pointerPositionAction != null)
            pointerPositionAction.action.Enable();
        // Tambiin activamos la accion que da la posicign del dedo.
    }

    void OnDisable()
    {
        // Se ejecuta cuando el objeto se desactiva o destruye.

        if (tapAction != null)
        {
            tapAction.action.performed -= OnTap;
            // Importante: nos desuscribimos del evento para evitar errores o fugas de memoria.
            tapAction.action.Disable();
            // Desactivamos la accien para liberar recursos.
        }
        if (pointerPositionAction != null)
            pointerPositionAction.action.Disable();
        // Igual desactivamos la accign de posicign.
    }
    private void OnTap(InputAction.CallbackContext ctx)
    {
        // Este mitodo se ejecuta cada vez que ocurre un "tap" en la pantalla.

        Camera cameraToUse = cam != null ? cam : Camera.main;
        // Usamos la cimara asignada, o si es nula, la cimara principal.

        if (cameraToUse == null) return;
        // Si no hay c[mara disponible, salimos sin hacer nada.

        Vector2 screenPos = pointerPositionAction.action.ReadValue<Vector2>();
        // Leemos la posicien del toque en coordenadas de pantalla (pixeles).

        Ray ray = cameraToUse.ScreenPointToRay(screenPos);
        // Creamos un rayo desde la cOmara hacia donde el usuario tocE

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, raycastLayers))
        {
            // Si el rayo golpea un objeto en la escena dentro de 1000 unidades y en las capas permitidas:

            Debug.Log($"Tap sobre: {hit.collider.gameObject.name}");
            // Mostramos en consola el nombre del objeto tocado.

            // Opcional: ejemplo para crear una esfera en el punto tocado
            // GameObject. CreatePrimitive(PrimitiveType. Sphere). transform.position = hit.point;
        }

        else
        {
            // Si el toque no golpea nada en la escena:
            Debug.Log("Tap en vadio (no golpeg nada).");

        }
    }
}
