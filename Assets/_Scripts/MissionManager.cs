using UnityEngine;
using TMPro; // Para TextMeshPro
using System.Collections.Generic;

[System.Serializable]
public class Mission
{
    public string missionName;
    public string description;
    public string plantTypeRequired; // Qué tipo de planta se necesita (ej: "Zanahoria")
    public int amountRequired; // Cantidad a cosechar
    public int currentProgress; // Progreso actual
    public bool isCompleted;

    public Mission(string name, string desc, string plantType, int amount)
    {
        missionName = name;
        description = desc;
        plantTypeRequired = plantType;
        amountRequired = amount;
        currentProgress = 0;
        isCompleted = false;
    }

    public void AdvanceProgress(string harvestedPlantType)
    {
        if (!isCompleted && harvestedPlantType == plantTypeRequired)
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

    public string GetStatusText()
    {
        if (isCompleted)
        {
            return $"✅ {missionName}: ¡Completada!";
        }
        else
        {
            return $"➡️ {missionName}: Cosecha {plantTypeRequired} ({currentProgress}/{amountRequired})";
        }
    }
}

public class MissionManager : MonoBehaviour
{
    public TMPro.TextMeshProUGUI missionObjectiveText; // Asignar desde el Inspector
    public List<Mission> activeMissions = new List<Mission>();

    // Referencia al PlantManager (para suscribirse a eventos)
    public PlantManager plantManager; // Asignar desde el Inspector

    void Start()
    {
        if (missionObjectiveText == null) Debug.LogError("Mission Objective Text (TextMeshProUGUI) no asignado en MissionManager.");
        if (plantManager == null) Debug.LogError("Plant Manager no asignado en MissionManager. ¡Asigna el GameManager!");

        // Suscribirse al evento de cosecha de plantas
        PlantManager.OnPlantHarvested += OnPlantHarvestedHandler;

        // Ejemplo de misión inicial
        AddMission(new Mission("Cosechar Zanahorias", "Cosecha 5 zanahorias para el mercado local.", "Zanahoria", 5));

        UpdateMissionUI(); // Muestra el estado inicial de la misión
    }

    void OnDestroy()
    {
        // Es importante desuscribirse para evitar errores cuando el objeto se destruye
        PlantManager.OnPlantHarvested -= OnPlantHarvestedHandler;
    }

    // Manejador del evento de cosecha
    void OnPlantHarvestedHandler(PlantData plant)
    {
        foreach (Mission mission in activeMissions)
        {
            mission.AdvanceProgress(plant.plantName);
        }
        UpdateMissionUI(); // Actualiza la UI cada vez que se avanza en una misión
    }

    public void AddMission(Mission newMission)
    {
        activeMissions.Add(newMission);
        UpdateMissionUI();
    }

    public void UpdateMissionUI()
    {
        if (missionObjectiveText != null)
        {
            string uiText = "Misiones:\n";
            if (activeMissions.Count == 0)
            {
                uiText += "No hay misiones activas.";
            }
            else
            {
                foreach (Mission mission in activeMissions)
                {
                    uiText += "- " + mission.GetStatusText() + "\n";
                }
            }
            missionObjectiveText.text = uiText;
        }
    }
}