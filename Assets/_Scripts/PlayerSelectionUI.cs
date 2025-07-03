using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cargar escenas
using UnityEngine.UI; // Necesario si usas botones est�ndar o Text
using TMPro; // Necesario si usas TextMeshProUGUI

public class PlayerSelectionUI : MonoBehaviour
{
    // Variables p�blicas para tus prefabs de jugador
    public GameObject playerPrefab1;
    public GameObject playerPrefab2;

    // Esta variable est�tica guardar� la selecci�n a trav�s de las escenas
    public static GameObject selectedPlayerPrefab;

    // Puedes a�adir m�s variables o una lista si tienes muchos jugadores
    // public List<GameObject> playerPrefabs;

    // M�todos que los botones de selecci�n de personaje llamar�n
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

    // Nuevo m�todo para el bot�n "Volver"
    public void GoToMainMenu()
    {
        Debug.Log("Volviendo al Men� Principal...");
        SceneManager.LoadScene("MainMenu"); // Aseg�rate de que "MainMenuScene" es el nombre exacto de tu escena de men� principal
    }
}