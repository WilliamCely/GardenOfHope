using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cargar escenas
using UnityEngine.UI; // Necesario si usas Button, etc.
using TMPro; // Necesario si usas TextMeshProUGUI para los botones

public class MainMenuController : MonoBehaviour
{
    // Puedes asignar los botones directamente en el Inspector si quieres manipularlos en el script
    // public Button playButton;
    // public Button optionsButton;
    // public Button quitButton;

    void Start()
    {
        // Opcional: Asegurarse de que el cursor del rat�n sea visible y no est� bloqueado
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // M�todo que se llamar� cuando se haga clic en el bot�n "Jugar"
    public void PlayGame()
    {
        Debug.Log("Cargando juego...");
        // Aseg�rate de que "MainScene" sea el nombre de tu escena de juego principal
        SceneManager.LoadScene("MainScene"); // Reemplaza "MainScene" con el nombre de tu escena de juego
    }

    // M�todo que se llamar� cuando se haga clic en el bot�n "Opciones"
    public void OpenOptions()
    {
        Debug.Log("Abriendo opciones (no implementado a�n).");
        // Aqu� podr�as cargar otra escena de opciones, o activar/desactivar un panel de UI
    }

    // M�todo que se llamar� cuando se haga clic en el bot�n "Salir"
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit(); // Esto funciona en builds. En el editor, solo detiene la ejecuci�n.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Para detener la ejecuci�n en el editor
#endif
    }
}