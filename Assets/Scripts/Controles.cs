using UnityEngine;

public class Controles : MonoBehaviour
{
    public GameObject Rabbit;

    public void RotateLeft()
    {
        Rabbit.transform.Rotate(0.0f, 10.0f, 0.0f,Space.Self);
    }
    public void RotateRight()
    {
        Rabbit.transform.Rotate(0.0f, -10.0f, 0.0f, Space.Self);
    }

    public void TranslateUp()
    {
        Rabbit.transform.Translate(Vector3.up * Time.deltaTime * 10, Space.World);
    }

    public void TranslateDown()
    {
        Rabbit.transform.Translate(Vector3.down * Time.deltaTime * 10, Space.World);
    }

    public void TranslateLeft()
    {
        Rabbit.transform.Translate(Vector3.left * Time.deltaTime * 10, Space.World);
    }

    public void TranslateRight()
    {
        Rabbit.transform.Translate(Vector3.right * Time.deltaTime * 10, Space.World);
    }

    public void Scale(float magnitud)
    {
        Vector3 changerscale = new Vector3(magnitud, magnitud, magnitud);

        Rabbit.transform.localScale += changerscale;
    }
}
