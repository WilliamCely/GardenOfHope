using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cargar escenas
using UnityEngine.UI; // Necesario si usas botones estándar o Text (aunque TMPro es mejor para texto)
using TMPro; // Necesario si usas TextMeshProUGUI y TMP_InputField

public class PlayerSelectionUI : MonoBehaviour
{
    // Variables públicas para tus prefabs de jugador
    public GameObject playerPrefab1; // Asigna en el Inspector
    public GameObject playerPrefab2; // Asigna en el Inspector

    // Esta variable estática guardará la selección a través de las escenas
    public static GameObject selectedPlayerPrefab;

    // --- NUEVO: Campo para el nombre del jugador ---
    public TMP_InputField playerNameInputField; // Asigna tu Input Field de TextMeshPro en el Inspector

    // Constante para la clave de PlayerPrefs (para guardar el nombre)
    public const string PlayerNameKey = "PlayerName";
    public const string SelectedPlayerPrefabKey = "SelectedPlayerPrefab"; // Para guardar el nombre del prefab seleccionado

    void Start()
    {
        // Opcional: Cargar el último nombre guardado si existe y asignarlo al InputField
        if (playerNameInputField != null && PlayerPrefs.HasKey(PlayerNameKey))
        {
            playerNameInputField.text = PlayerPrefs.GetString(PlayerNameKey);
        }
        else if (playerNameInputField != null)
        {
            // Puedes poner un nombre predeterminado si no hay ninguno guardado
            playerNameInputField.text = "Nuevo Jugador";
        }
    }

    // Método para seleccionar el jugador 1, guardar el nombre y cargar la escena
    public void SelectPlayer1()
    {
        selectedPlayerPrefab = playerPrefab1; // Guarda el prefab del jugador 1
        SavePlayerSelectionAndName(); // <--- Llama al nuevo método para guardar
        Debug.Log("Jugador 1 seleccionado. Cargando escena principal...");
        SceneManager.LoadScene("MainScene"); // Carga tu escena de juego principal
    }

    // Método para seleccionar el jugador 2, guardar el nombre y cargar la escena
    public void SelectPlayer2()
    {
        selectedPlayerPrefab = playerPrefab2; // Guarda el prefab del jugador 2
        SavePlayerSelectionAndName(); // <--- Llama al nuevo método para guardar
        Debug.Log("Jugador 2 seleccionado. Cargando escena principal...");
        SceneManager.LoadScene("MainScene"); // Carga tu escena de juego principal
    }

    // --- NUEVO: Método centralizado para guardar la selección y el nombre ---
    private void SavePlayerSelectionAndName()
    {
        // Guardar el nombre del jugador
        string playerName = "Jugador Anónimo"; // Valor predeterminado
        if (playerNameInputField != null)
        {
            playerName = playerNameInputField.text;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = "Jugador Anónimo"; // Nombre predeterminado si el usuario no escribe nada
                Debug.LogWarning("El nombre del jugador estaba vacío. Se usará 'Jugador Anónimo'.");
            }
        }
        PlayerPrefs.SetString(PlayerNameKey, playerName);
        Debug.Log($"Nombre del jugador guardado: {playerName}");

        // Guardar el nombre del prefab del jugador seleccionado (no el GameObject completo)
        if (selectedPlayerPrefab != null)
        {
            PlayerPrefs.SetString(SelectedPlayerPrefabKey, selectedPlayerPrefab.name);
            Debug.Log($"Prefab del jugador seleccionado guardado: {selectedPlayerPrefab.name}");
        }
        else
        {
            Debug.LogWarning("No se ha seleccionado ningún personaje. No se guardará el prefab.");
        }

        PlayerPrefs.Save(); // Guarda los cambios en PlayerPrefs
    }

    // Método para el botón "Volver"
    public void GoToMainMenu()
    {
        Debug.Log("Volviendo al Menú Principal...");
        SceneManager.LoadScene("MainMenu"); // Asegúrate de que "MainMenu" es el nombre exacto de tu escena de menú principal
    }
}