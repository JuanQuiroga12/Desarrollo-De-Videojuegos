using UnityEngine;
using UnityEngine.InputSystem;
// Importamos UnityEngine para trabajar en Unity y el nuevo sistema de entrada InputSystem.

public class PlayerMover : MonoBehaviour
{

    [Header("Accign Move (Vector2) desde el .inputactions")]
    public InputActionReference moveAction;
    // Referencia a la acciin 'Move' que se configurg en el asset de Input Actions.
    // Devuelve un Vector2 con la direccign (-1 .. 1 en x e y).

    [Header("Movimiento")]
    public float speed = 3.5f;
    // Velocidad de movimiento del objeto.

    public bool moveInCameraPlane = true;
    // Si es true, el movimiento se calcula relativo a la cEmara principal(util para juegos en 3ra persona).

    void OnEnable()
    {
        // Cuando el objeto se activa, habilitamos la acciin para que empiece a recibir valores.
        if (moveAction != null) moveAction.action.Enable();
    }

    void OnDisable()
    {
        // Cuando el objeto se desactiva, deshabilitamos la acciin para liberar recursos.
        if (moveAction != null) moveAction.action.Disable();
    }

    void Update()
    {

        // Este mitodo se llama cada frame.
        if (moveAction == null) return; // Si no hay accign asignada, salimos.

        Vector2 input = moveAction.action.ReadValue<Vector2>();
        // Leemos el valor de la accign 'Move' (ejes X e Y del joystick o stick virtual).

        Vector3 dir = new Vector3(input.x, 0f, input.y);
        // Convertimos el Vector2 en un Vector3 para mover en el plano XZ (horizontal).

        if (moveInCameraPlane && Camera.main)
        {
            // Si queremos que el movimiento sea relativo a la cmara principal:

            // Direcciones de la cimara en el plano horizontal (ignoramos el eje Y).
            Vector3 camFwd = Camera.main.transform.forward;
            camFwd.y = 0f; camFwd.Normalize();

            Vector3 camRight = Camera.main.transform.right;
            camRight.y = 0f; camRight.Normalize();

            // Ajustamos la direcciln del movimiento segin la orientacign de la cmara.
            dir = camFwd * dir.z + camRight * dir.x;
        }


        // Movemos el objeto multiplicando por la velocidad y el tiempo entre frames (para suavidad).
        transform.position += dir * speed * Time.deltaTime;
    }
}
            
