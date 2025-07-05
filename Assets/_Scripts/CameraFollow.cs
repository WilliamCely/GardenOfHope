using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // El jugador que la cámara seguirá
    public float smoothSpeed = 0.125f; // Qué tan suavemente la cámara se mueve
    public Vector3 offset; // Un desfase opcional para posicionar la cámara (ej. para ver más arriba o abajo)

    // LateUpdate se llama después de que todos los Updates se han ejecutado.
    // Esto asegura que el personaje se ha movido antes de que la cámara lo siga.
    void LateUpdate()
    {
        if (target == null)
        {
            // Si no hay un target asignado, busca al jugador instanciado
            // Esta es una forma simple, para juegos más complejos es mejor asignar directamente.
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("CameraFollow: Target (jugador) encontrado y asignado.");
            }
            else
            {
                // Debug.LogWarning("CameraFollow: No se encontró un GameObject con la etiqueta 'Player'.");
                return; // Sale del LateUpdate si no hay target
            }
        }

        // Calcula la posición deseada de la cámara
        Vector3 desiredPosition = target.position + offset;

        // Suaviza el movimiento de la cámara para que no sea instantáneo
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Opcional: Si quieres que la cámara mire al jugador (más común en 3D)
        // transform.LookAt(target);
    }
}