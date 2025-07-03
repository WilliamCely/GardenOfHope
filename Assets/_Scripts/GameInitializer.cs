using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitializer : MonoBehaviour
{
    public Transform playerSpawnPoint;
    public PlantManager plantManager;

    // �NUEVO CAMPO! Asigna aqu� el prefab de tu jugador por defecto (ej. Player_Boy)
    public GameObject defaultPlayerPrefab;

    void Start()
    {
        GameObject instantiatedPlayer = null; // Variable para almacenar el jugador instanciado

        // Si se seleccion� un jugador en la pantalla de selecci�n
        if (PlayerSelectionUI.selectedPlayerPrefab != null)
        {
            Debug.Log($"GameInitializer: Intentando instanciar el jugador: {PlayerSelectionUI.selectedPlayerPrefab.name}");
            instantiatedPlayer = Instantiate(PlayerSelectionUI.selectedPlayerPrefab, playerSpawnPoint.position, Quaternion.identity);
        }
        // Si NO se seleccion� ning�n jugador (ej. si se inicia directamente la MainScene o hubo un error)
        else
        {
            Debug.LogWarning("No se seleccion� ning�n jugador. Instanciando jugador por defecto.");

            // Usamos el 'defaultPlayerPrefab' que asignaremos en el Inspector de este mismo GameInitializer
            if (defaultPlayerPrefab != null)
            {
                instantiatedPlayer = Instantiate(defaultPlayerPrefab, playerSpawnPoint.position, Quaternion.identity);
            }
            else
            {
                Debug.LogError("No hay prefab de jugador por defecto asignado en GameInitializer. No se pudo instanciar un jugador.");
            }
        }

        // Si se logr� instanciar un jugador y el PlantManager est� asignado
        if (instantiatedPlayer != null && plantManager != null)
        {
            plantManager.playerTransform = instantiatedPlayer.transform;
            Debug.Log($"PlantManager.playerTransform asignado autom�ticamente a: {instantiatedPlayer.name}");
        }
        // Si el PlantManager no est� asignado en GameInitializer
        else if (plantManager == null)
        {
            Debug.LogWarning("PlantManager no asignado en GameInitializer. No se pudo asignar el playerTransform autom�ticamente.");
        }
        // Si no se pudo instanciar ning�n jugador
        else if (instantiatedPlayer == null)
        {
            Debug.LogError("No se pudo instanciar ning�n jugador (ni el seleccionado ni el por defecto).");
        }
    }
}