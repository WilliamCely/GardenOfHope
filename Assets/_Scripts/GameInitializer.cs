using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitializer : MonoBehaviour
{
    public Transform playerSpawnPoint;
    public PlantManager plantManager;

    // ¡NUEVO CAMPO! Asigna aquí el prefab de tu jugador por defecto (ej. Player_Boy)
    public GameObject defaultPlayerPrefab;

    void Start()
    {
        GameObject instantiatedPlayer = null; // Variable para almacenar el jugador instanciado

        // Si se seleccionó un jugador en la pantalla de selección
        if (PlayerSelectionUI.selectedPlayerPrefab != null)
        {
            Debug.Log($"GameInitializer: Intentando instanciar el jugador: {PlayerSelectionUI.selectedPlayerPrefab.name}");
            instantiatedPlayer = Instantiate(PlayerSelectionUI.selectedPlayerPrefab, playerSpawnPoint.position, Quaternion.identity);
        }
        // Si NO se seleccionó ningún jugador (ej. si se inicia directamente la MainScene o hubo un error)
        else
        {
            Debug.LogWarning("No se seleccionó ningún jugador. Instanciando jugador por defecto.");

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

        // Si se logró instanciar un jugador y el PlantManager está asignado
        if (instantiatedPlayer != null && plantManager != null)
        {
            plantManager.playerTransform = instantiatedPlayer.transform;
            Debug.Log($"PlantManager.playerTransform asignado automáticamente a: {instantiatedPlayer.name}");
        }
        // Si el PlantManager no está asignado en GameInitializer
        else if (plantManager == null)
        {
            Debug.LogWarning("PlantManager no asignado en GameInitializer. No se pudo asignar el playerTransform automáticamente.");
        }
        // Si no se pudo instanciar ningún jugador
        else if (instantiatedPlayer == null)
        {
            Debug.LogError("No se pudo instanciar ningún jugador (ni el seleccionado ni el por defecto).");
        }
    }
}