using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Puedes añadir referencias a otros managers aquí si los necesitas para un control centralizado
    // Por ejemplo:
    // public PlantManager plantManager;
    // public MissionManager missionManager;
    // ...

    void Start()
    {
        // Aquí podrías inicializar otros sistemas o cargar el juego
        // Ejemplo:
        // plantManager = GetComponent<PlantManager>();
        // missionManager = GetComponent<MissionManager>();
    }

    void Update()
    {
        // Lógica global del juego que no pertenece a un manager específico
    }
}