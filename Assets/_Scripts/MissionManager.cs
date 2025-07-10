using UnityEngine;
using TMPro; // Para TextMeshPro
using System.Collections.Generic;
using System.Linq; // Para usar .Any() y .Remove() en Listas
using System; // <--- Importante: Para usar System.Random

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
                    // Compara con el tipo de ítem cosechado
                    if (plantData.harvestedItemType == plantTypeRequired)
                    {
                        relevantAction = true;
                    }
                    break;
                case MissionType.Plant:
                    // Compara con el nombre de la planta
                    if (plantData.plantName == plantTypeRequired)
                    {
                        relevantAction = true;
                    }
                    break;
                case MissionType.Water:
                    // Si plantTypeRequired está vacío, aplica a cualquier planta; de lo contrario, solo a la específica.
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
    public int maxActiveMissions = 2; // <--- CAMBIADO a 2

    private List<Mission> availableMissions = new List<Mission>(); // Todas las misiones posibles
    public List<Mission> activeMissions = new List<Mission>();    // Misiones actualmente en progreso

    private System.Random rng; // <--- CORRECCIÓN: Especifica System.Random

    void Start()
    {
        if (missionObjectiveText == null) Debug.LogError("Mission Objective Text (TextMeshProUGUI) no asignado en MissionManager.");
        if (plantManager == null) Debug.LogError("Plant Manager no asignado en MissionManager. ¡Asigna el objeto con el script PlantManager!");

        rng = new System.Random(); 

        PlantManager.OnPlantHarvested += OnPlantHarvestedHandler;
        PlantManager.OnPlantPlanted += OnPlantPlantedHandler;
        PlantManager.OnPlantWatered += OnPlantWateredHandler;

        // --- Definir TODAS las misiones posibles aquí ---
        availableMissions.Add(new Mission("Cosechar Zanahorias I", "Cosecha 5 Zanahorias para el mercado local.", "Zanahoria", 5, MissionType.Harvest, "Semilla de Zanahoria", 4));
        availableMissions.Add(new Mission("Sembrar Tomates I", "Siembra 3 plantas de Tomate.", "Tomate", 3, MissionType.Plant, "Semilla de Tomate", 3));
        availableMissions.Add(new Mission("Regar Cultivos Variados I", "Riega 10 plantas de cualquier tipo.", "", 10, MissionType.Water, "Semilla de Girasol", 6));
        availableMissions.Add(new Mission("Regar Zanahorias Específicas I", "Riega 5 plantas de Zanahoria.", "Zanahoria", 5, MissionType.Water, "Semilla de Zanahoria", 5));
        availableMissions.Add(new Mission("Cosechar Uvas I", "Cosecha 7 Uvas del viñedo.", "Uva", 7, MissionType.Harvest, "Semilla de Uva", 5));
        availableMissions.Add(new Mission("Sembrar Plátanos I", "Siembra 2 Plátanos exóticos.", "Platano", 2, MissionType.Plant, "Semilla de Platano", 3));
        availableMissions.Add(new Mission("Cosechar Girasoles I", "Cosecha 3 Girasoles para aceite.", "Girasol", 3, MissionType.Harvest, "Semilla de Girasol", 5));
        availableMissions.Add(new Mission("Regar Tomates I", "Riega 4 plantas de Tomate.", "Tomate", 4, MissionType.Water, "Semilla de Tomate", 6));
        availableMissions.Add(new Mission("Cosechar Tulipanes II", "Cosecha 10 Tulipanes.", "Tulipan", 10, MissionType.Harvest, "Semilla de Tulipan", 6));
        availableMissions.Add(new Mission("Sembrar Tulipanes", "Siembra 5 plantas de Tulipan para Halloween.", "Tulipan", 5, MissionType.Plant, "Semilla de Tulipan", 5));

        ShuffleMissions(); // Mezcla todas las misiones al inicio
        ActivateInitialMissions(); // Activa las primeras 2 misiones (o maxActiveMissions)

        UpdateMissionUI();
    }

    void OnDestroy()
    {
        PlantManager.OnPlantHarvested -= OnPlantHarvestedHandler;
        PlantManager.OnPlantPlanted -= OnPlantPlantedHandler;
        PlantManager.OnPlantWatered -= OnPlantWateredHandler;
    }

    // El Update ya no necesita el timer de nuevas misiones, las misiones se activan al completar
    void Update()
    {
        // La activación de nuevas misiones se maneja en CheckMissionCompletionAndReward
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

                if (plantManager != null)
                {
                    plantManager.PlayMissionCompleteSound();
                }

                // Intentar activar una nueva misión inmediatamente después de completar esta
                TryActivateNewMission();
            }
        }
    }

    // Mezclar la lista de misiones disponibles
    void ShuffleMissions()
    {
        int n = availableMissions.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1); // Usa la instancia de System.Random
            Mission value = availableMissions[k];
            availableMissions[k] = availableMissions[n];
            availableMissions[n] = value;
        }
    }

    void ActivateInitialMissions()
    {
        // Activa misiones hasta el límite o hasta que no queden disponibles
        for (int i = 0; i < maxActiveMissions && availableMissions.Count > 0; i++)
        {
            AddRandomMissionToActive(); 
        }
    }

    // Añadir una misión aleatoria a las activas
    void AddRandomMissionToActive()
    {
        if (availableMissions.Count > 0)
        {
            int randomIndex = rng.Next(availableMissions.Count); 
            Mission newMission = availableMissions[randomIndex]; 
            availableMissions.RemoveAt(randomIndex); 

            activeMissions.Add(newMission);
            Debug.Log($"¡Nueva misión activada aleatoriamente: {newMission.missionName}!");
        }
        else
        {
            Debug.LogWarning("No quedan misiones disponibles para activar. Considera rellenar la lista o implementar un final de juego.");
        }
    }

    // Intentar activar una nueva misión aleatoria, limpiando las completadas primero
    void TryActivateNewMission()
    {
        // Primero, limpia las misiones que ya fueron completadas y recompensadas
        activeMissions.RemoveAll(m => m.isCompleted && m.rewardClaimed);

        // Si hay espacio para una nueva misión Y hay misiones disponibles
        if (activeMissions.Count < maxActiveMissions && availableMissions.Count > 0)
        {
            AddRandomMissionToActive(); // Añade una misión aleatoria
            UpdateMissionUI(); // Actualiza la UI después de añadir
        }
        else if (availableMissions.Count == 0 && activeMissions.All(m => m.isCompleted && m.rewardClaimed))
        {
            Debug.Log("Todas las misiones han sido completadas y sus recompensas reclamadas. Puedes implementar lógica para generar nuevas misiones o un final.");
            // Si todas las misiones han sido completadas y reclamadas, puedes reiniciar la lista de misiones
            // o cargar más misiones si tienes un pool más grande.
            // Ejemplo de reiniciar:
            // availableMissions.Clear();
            // // Vuelve a añadir todas las misiones originales
            // availableMissions.Add(new Mission("Cosechar Zanahorias I", "Cosecha 5 Zanahorias para el mercado local.", "Zanahoria", 5, MissionType.Harvest, "Semilla de Zanahoria", 2));
            // availableMissions.Add(new Mission("Sembrar Tomates I", "Siembra 3 plantas de Tomate.", "Tomate", 3, MissionType.Plant, "Semilla de Tomate", 1));
            // // ... y así sucesivamente para todas tus misiones
            // ShuffleMissions();
            // TryActivateNewMission(); // Intenta activar una nueva ahora que hay disponibles
        }
        UpdateMissionUI(); 
    }

    public void UpdateMissionUI()
    {
        if (missionObjectiveText != null)
        {
            string uiText = "Misiones Activas:\n";
            // Solo muestra las misiones que NO han sido completadas y reclamadas de la lista activa
            var currentVisibleMissions = activeMissions.Where(m => !(m.isCompleted && m.rewardClaimed)).ToList();

            if (currentVisibleMissions.Count == 0)
            {
                uiText += "No hay misiones activas en este momento.";
            }
            else
            {
                foreach (Mission mission in currentVisibleMissions)
                {
                    uiText += "- " + mission.GetStatusText() + "\n";
                }
            }

            // Muestra misiones que están completadas pero aún no se han retirado de la lista activa
            var completedUnclaimed = activeMissions.Where(m => m.isCompleted && !m.rewardClaimed).ToList();
            if (completedUnclaimed.Any())
            {
                uiText += "\n¡Misiones Completadas (Recompensa Otorgada!):\n";
                foreach (var mission in completedUnclaimed)
                {
                    uiText += "- " + mission.GetStatusText() + "\n";
                }
            }

            missionObjectiveText.text = uiText;
        }
    }
}