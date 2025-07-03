using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cargar escenas
using UnityEngine.UI; // Necesario si usas botones estándar o Text
using TMPro; // Necesario si usas TextMeshProUGUI

public class PlayerSelectionUI : MonoBehaviour
{
    // Variables públicas para tus prefabs de jugador
    public GameObject playerPrefab1;
    public GameObject playerPrefab2;

    // Esta variable estática guardará la selección a través de las escenas
    public static GameObject selectedPlayerPrefab;

    // Puedes añadir más variables o una lista si tienes muchos jugadores
    // public List<GameObject> playerPrefabs;

    // Métodos que los botones de selección de personaje llamarán
    public void SelectPlayer1()
    {
        selectedPlayerPrefab = playerPrefab1; // Guarda el prefab del jugador 1
        Debug.Log("Jugador 1 seleccionado. Cargando escena principal...");
        SceneManager.LoadScene("MainScene"); // Carga tu escena de juego principal
    }

    public void SelectPlayer2()
    {
        selectedPlayerPrefab = playerPrefab2; // Guarda el prefab del jugador 2
        Debug.Log("Jugador 2 seleccionado. Cargando escena principal...");
        SceneManager.LoadScene("MainScene"); // Carga tu escena de juego principal
    }

    // Nuevo método para el botón "Volver"
    public void GoToMainMenu()
    {
        Debug.Log("Volviendo al Menú Principal...");
        SceneManager.LoadScene("MainMenu"); // Asegúrate de que "MainMenuScene" es el nombre exacto de tu escena de menú principal
    }
}