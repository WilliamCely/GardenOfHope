using UnityEngine;
using TMPro; // Para TextMeshPro
using System.Collections.Generic;
using System.Linq; // Para usar .Any() y .Remove() en Listas

[System.Serializable]
public enum MissionType { Harvest, Plant, Water }

[System.Serializable]
public class Mission
{
    public string missionName;
    public string description;
    public string plantTypeRequired; // Qué tipo de planta se necesita (ej: "Zanahoria"). Vacío para "cualquier tipo" en riego.
    public int amountRequired; // Cantidad a cosechar/plantar/regar
    public int currentProgress; // Progreso actual
    public bool isCompleted;
    public MissionType type;
    public string rewardSeedType;
    public int rewardAmount;
    public bool rewardClaimed;

    // Constructor actualizado con los nuevos campos
    public Mission(string name, string desc, string plantType, int amount, MissionType missionType, string rewardSeed, int rewardAmt)
    {
        missionName = name;
        description = desc;
        plantTypeRequired = plantType;
        amountRequired = amount;
        currentProgress = 0;
        isCompleted = false;
        type = missionType;
        rewardSeedType = rewardSeed;
        rewardAmount = rewardAmt;
        rewardClaimed = false;
    }

    // Método para avanzar el progreso de la misión basado en la acción y la planta
    public void AdvanceProgress(PlantData plantData, MissionType actionType)
    {
        if (!isCompleted && type == actionType)
        {
            bool relevantAction = false;

            switch (actionType)
            {
                case MissionType.Harvest:
                    if (plantData.harvestedItemType == plantTypeRequired)
                    {
                        relevantAction = true;
                    }
                    break;
                case MissionType.Plant:
                    if (plantData.plantName == plantTypeRequired)
                    {
                        relevantAction = true;
                    }
                    break;
                case MissionType.Water:
                    if (string.IsNullOrEmpty(plantTypeRequired) || plantData.plantName == plantTypeRequired)
                    {
                        relevantAction = true;
                    }
                    break;
            }

            if (relevantAction)
            {
                currentProgress++;
                if (currentProgress >= amountRequired)
                {
                    currentProgress = amountRequired;
                    isCompleted = true;
                    Debug.Log($"¡Misión '{missionName}' completada!");
                }
                else
                {
                    Debug.Log($"Progreso de misión '{missionName}': {currentProgress}/{amountRequired}");
                }
            }
        }
    }

    public string GetStatusText()
    {
        if (isCompleted)
        {
            if (rewardClaimed)
            {
                return $"✅ {missionName}: Completada (Recompensa reclamada)";
            }
            else
            {
                return $"✅ {missionName}: Completada (Recompensa: {rewardAmount} {rewardSeedType})";
            }
        }
        else
        {
            string actionText = "";
            switch (type)
            {
                case MissionType.Harvest:
                    actionText = $"Cosecha {plantTypeRequired}";
                    break;
                case MissionType.Plant:
                    actionText = $"Siembra {plantTypeRequired}";
                    break;
                case MissionType.Water:
                    actionText = $"Riega {(string.IsNullOrEmpty(plantTypeRequired) ? "plantas" : plantTypeRequired)}";
                    break;
            }
            return $"➡️ {missionName}: {actionText} ({currentProgress}/{amountRequired})";
        }
    }
}

public class MissionManager : MonoBehaviour
{
    public TMPro.TextMeshProUGUI missionObjectiveText;
    public PlantManager plantManager; // <-- ¡Asegúrate de asignar esto en el Inspector!

    [Header("Configuración de Misiones")]
    public int maxActiveMissions = 3; // Cuántas misiones pueden estar activas a la vez
    public float timeBetweenNewMissions = 30f; // Tiempo en segundos para intentar activar una nueva misión

    private List<Mission> availableMissions = new List<Mission>(); // Todas las misiones posibles
    public List<Mission> activeMissions = new List<Mission>();    // Misiones actualmente en progreso
    private List<Mission> completedMissions = new List<Mission>(); // Misiones que ya se completaron y se les dio recompensa

    private float newMissionTimer;

    void Start()
    {
        if (missionObjectiveText == null) Debug.LogError("Mission Objective Text (TextMeshProUGUI) no asignado en MissionManager.");
        if (plantManager == null) Debug.LogError("Plant Manager no asignado en MissionManager. ¡Asigna el objeto con el script PlantManager!");

        // Suscribirse a los eventos del PlantManager
        PlantManager.OnPlantHarvested += OnPlantHarvestedHandler;
        PlantManager.OnPlantPlanted += OnPlantPlantedHandler;
        PlantManager.OnPlantWatered += OnPlantWateredHandler;

        // --- Definir TODAS las misiones posibles aquí ---
        availableMissions.Add(new Mission("Cosechar Zanahorias I", "Cosecha 5 zanahorias para el mercado local.", "Zanahoria", 5, MissionType.Harvest, "Semilla de Zanahoria", 2));
        availableMissions.Add(new Mission("Sembrar Tomates I", "Siembra 3 plantas de tomate.", "Tomate", 3, MissionType.Plant, "Semilla de Tomate", 1));
        availableMissions.Add(new Mission("Regar Cultivos Variados I", "Riega 10 plantas de cualquier tipo.", "", 10, MissionType.Water, "Semilla de Girasol", 3));
        availableMissions.Add(new Mission("Regar Zanahorias Específicas I", "Riega 5 plantas de zanahoria.", "Zanahoria", 5, MissionType.Water, "Semilla de Zanahoria", 1));
        availableMissions.Add(new Mission("Cosechar Uvas I", "Cosecha 7 uvas del viñedo.", "Uva", 7, MissionType.Harvest, "Semilla de Uva", 3));
        availableMissions.Add(new Mission("Sembrar Plátanos I", "Siembra 2 plátanos exóticos.", "Platano", 2, MissionType.Plant, "Semilla de Platano", 1));
        availableMissions.Add(new Mission("Cosechar Girasoles I", "Cosecha 3 girasoles para aceite.", "Girasol", 3, MissionType.Harvest, "Semilla de Girasol", 2));
        availableMissions.Add(new Mission("Regar Tomates I", "Riega 4 plantas de tomate.", "Tomate", 4, MissionType.Water, "Semilla de Tomate", 1));
        // Puedes añadir más misiones aquí: "Cosechar Zanahorias II", "Sembrar Tomates II", etc. con mayores cantidades y/o recompensas
        // availableMissions.Add(new Mission("Cosechar Zanahorias II", "Cosecha 10 zanahorias para el chef.", "Zanahoria", 10, MissionType.Harvest, "Semilla de Zanahoria", 5));
        // -----------------------------------------------------

        ShuffleMissions(); // Mezcla las misiones al inicio
        ActivateInitialMissions(); // Activa algunas misiones al inicio del juego

        newMissionTimer = timeBetweenNewMissions; // Inicializa el temporizador
        UpdateMissionUI();
    }

    void OnDestroy()
    {
        PlantManager.OnPlantHarvested -= OnPlantHarvestedHandler;
        PlantManager.OnPlantPlanted -= OnPlantPlantedHandler;
        PlantManager.OnPlantWatered -= OnPlantWateredHandler;
    }

    void Update()
    {
        // Solo intenta activar una nueva misión si hay espacio y misiones disponibles
        if (activeMissions.Count < maxActiveMissions && availableMissions.Count > 0)
        {
            newMissionTimer -= Time.deltaTime;
            if (newMissionTimer <= 0)
            {
                TryActivateNewMission();
                newMissionTimer = timeBetweenNewMissions; // Reinicia el temporizador
            }
        }
    }

    void OnPlantHarvestedHandler(PlantData plant)
    {
        Debug.Log($"MisionManager: Planta {plant.plantName} cosechada. Actualizando misiones de cosecha.");
        UpdateActiveMissions(plant, MissionType.Harvest);
        UpdateMissionUI();
    }

    void OnPlantPlantedHandler(PlantData plant)
    {
        Debug.Log($"MisionManager: Planta {plant.plantName} plantada. Actualizando misiones de siembra.");
        UpdateActiveMissions(plant, MissionType.Plant);
        UpdateMissionUI();
    }

    void OnPlantWateredHandler(PlantData plant)
    {
        Debug.Log($"MisionManager: Planta {plant.plantName} regada. Actualizando misiones de riego.");
        UpdateActiveMissions(plant, MissionType.Water);
        UpdateMissionUI();
    }

    // Método auxiliar para actualizar las misiones activas y verificar recompensas
    void UpdateActiveMissions(PlantData plant, MissionType actionType)
    {
        // Usamos ToList() para poder modificar activeMissions mientras iteramos
        foreach (Mission mission in activeMissions.ToList())
        {
            if (!mission.isCompleted) // Solo si la misión aún no está completada
            {
                mission.AdvanceProgress(plant, actionType);
                CheckMissionCompletionAndReward(mission);
            }
        }
    }

    public void CheckMissionCompletionAndReward(Mission mission)
    {
        if (mission.isCompleted && !mission.rewardClaimed)
        {
            if (plantManager != null && !string.IsNullOrEmpty(mission.rewardSeedType) && mission.rewardAmount > 0)
            {
                plantManager.AddSeedsToInventory(mission.rewardSeedType, mission.rewardAmount);
                mission.rewardClaimed = true;
                Debug.Log($"¡Recompensa de misión para '{mission.missionName}' otorgada: {mission.rewardAmount} {mission.rewardSeedType}!");

                // ¡AQUÍ ES DONDE REPRODUCIMOS EL SONIDO DE MISIÓN COMPLETADA!
                if (plantManager != null)
                {
                    plantManager.PlayMissionCompleteSound();
                }

                // Una vez completada y reclamada la recompensa, la movemos a misiones completadas
                // No la eliminamos de activeMissions aquí directamente, se gestiona en UpdateMissionUI o en TryActivateNewMission
                // para que no se vea duplicada o eliminada abruptamente si no la queremos ver más.
            }
        }
    }

    // --- NUEVO: Mezclar la lista de misiones disponibles ---
    void ShuffleMissions()
    {
        System.Random rng = new System.Random();
        int n = availableMissions.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Mission value = availableMissions[k];
            availableMissions[k] = availableMissions[n];
            availableMissions[n] = value;
        }
    }

    // --- NUEVO: Activar un número inicial de misiones ---
    void ActivateInitialMissions()
    {
        // Activa misiones hasta el límite o hasta que no queden disponibles
        for (int i = 0; i < maxActiveMissions && availableMissions.Count > 0; i++)
        {
            TryActivateNewMission(); // Llama al método que activa una misión
        }
    }

    // --- NUEVO: Intentar activar una nueva misión aleatoria ---
    void TryActivateNewMission()
    {
        // Limpiar misiones completadas que ya reclamaron recompensa de la lista activa
        // para hacer espacio para nuevas misiones.
        activeMissions.RemoveAll(m => m.isCompleted && m.rewardClaimed);

        if (activeMissions.Count < maxActiveMissions && availableMissions.Count > 0)
        {
            // Selecciona la primera misión de la lista barajada (o una al azar si lo prefieres)
            // Usar la primera es bueno si quieres un flujo más "lineal" pero aún aleatorio por el shuffle inicial.
            Mission newMission = availableMissions[0];
            availableMissions.RemoveAt(0); // Quita la misión de la lista de disponibles

            activeMissions.Add(newMission);
            Debug.Log($"¡Nueva misión activada: {newMission.missionName}!");
            UpdateMissionUI(); // Actualiza la UI para mostrar la nueva misión
        }
        else if (availableMissions.Count == 0 && activeMissions.All(m => m.isCompleted && m.rewardClaimed))
        {
            Debug.Log("Todas las misiones han sido completadas y sus recompensas reclamadas. Puedes implementar lógica para generar nuevas misiones o un final.");
            // Aquí podrías recargar las misiones, generar misiones infinitas, o indicar un fin del juego/contenido.
            // Por ejemplo, para un juego simple, podrías reiniciar la lista de availableMissions y volver a barajar.
            // availableMissions.AddRange(allPossibleMissionsDefinedSomewhere); // Donde allPossibleMissions es una copia de las misiones iniciales
            // ShuffleMissions();
        }
    }

    public void UpdateMissionUI()
    {
        if (missionObjectiveText != null)
        {
            string uiText = "Misiones Activas:\n";
            if (activeMissions.Count == 0)
            {
                uiText += "No hay misiones activas en este momento.";
            }
            else
            {
                foreach (Mission mission in activeMissions)
                {
                    uiText += "- " + mission.GetStatusText() + "\n";
                }
            }

            // Opcional: Mostrar misiones completadas que aún no han reclamado recompensa
            var completedUnclaimed = activeMissions.Where(m => m.isCompleted && !m.rewardClaimed).ToList();
            if (completedUnclaimed.Any())
            {
                uiText += "\n¡Misiones Completadas (Reclama Recompensa!):\n";
                foreach (var mission in completedUnclaimed)
                {
                    uiText += "- " + mission.GetStatusText() + "\n";
                }
            }

            missionObjectiveText.text = uiText;
        }
    }
}