using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Puedes a�adir referencias a otros managers aqu� si los necesitas para un control centralizado
    // Por ejemplo:
    // public PlantManager plantManager;
    // public MissionManager missionManager;
    // ...

    void Start()
    {
        // Aqu� podr�as inicializar otros sistemas o cargar el juego
        // Ejemplo:
        // plantManager = GetComponent<PlantManager>();
        // missionManager = GetComponent<MissionManager>();
    }

    void Update()
    {
        // L�gica global del juego que no pertenece a un manager espec�fico
    }
}