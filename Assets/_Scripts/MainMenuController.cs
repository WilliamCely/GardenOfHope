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
        // Opcional: Asegurarse de que el cursor del ratón sea visible y no esté bloqueado
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // Método que se llamará cuando se haga clic en el botón "Jugar"
    public void PlayGame()
    {
        Debug.Log("Cargando juego...");
        // Asegúrate de que "MainScene" sea el nombre de tu escena de juego principal
        SceneManager.LoadScene("MainScene"); // Reemplaza "MainScene" con el nombre de tu escena de juego
    }

    // Método que se llamará cuando se haga clic en el botón "Opciones"
    public void OpenOptions()
    {
        Debug.Log("Abriendo opciones (no implementado aún).");
        // Aquí podrías cargar otra escena de opciones, o activar/desactivar un panel de UI
    }

    // Método que se llamará cuando se haga clic en el botón "Salir"
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit(); // Esto funciona en builds. En el editor, solo detiene la ejecución.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Para detener la ejecución en el editor
#endif
    }
}