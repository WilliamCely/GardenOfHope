using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // El jugador que la c�mara seguir�
    public float smoothSpeed = 0.125f; // Qu� tan suavemente la c�mara se mueve
    public Vector3 offset; // Un desfase opcional para posicionar la c�mara (ej. para ver m�s arriba o abajo)

    // LateUpdate se llama despu�s de que todos los Updates se han ejecutado.
    // Esto asegura que el personaje se ha movido antes de que la c�mara lo siga.
    void LateUpdate()
    {
        if (target == null)
        {
            // Si no hay un target asignado, busca al jugador instanciado
            // Esta es una forma simple, para juegos m�s complejos es mejor asignar directamente.
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("CameraFollow: Target (jugador) encontrado y asignado.");
            }
            else
            {
                // Debug.LogWarning("CameraFollow: No se encontr� un GameObject con la etiqueta 'Player'.");
                return; // Sale del LateUpdate si no hay target
            }
        }

        // Calcula la posici�n deseada de la c�mara
        Vector3 desiredPosition = target.position + offset;

        // Suaviza el movimiento de la c�mara para que no sea instant�neo
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Opcional: Si quieres que la c�mara mire al jugador (m�s com�n en 3D)
        // transform.LookAt(target);
    }
}