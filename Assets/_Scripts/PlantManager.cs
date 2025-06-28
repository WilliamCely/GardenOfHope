using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEngine.InputSystem; // Necesario para el nuevo Input System
using TMPro; // ¡IMPORTANTE! Necesario para TextMeshProUGUI

// Clase para definir los datos de una planta específica
[System.Serializable]
public class PlantDefinition
{
    public string plantName;          // Nombre de la planta (ej. "Zanahoria")
    public GameObject seedPrefab;     // Prefab de la semilla (para referencia visual o futura instancia)
    public Sprite[] growthSprites;    // Sprites para cada etapa de crecimiento
    public float growthDuration;      // Tiempo total que tarda en crecer (en segundos)
    public Sprite harvestSprite;      // Sprite final al cosechar (opcional)

    // Nuevos campos para el inventario
    public string seedItemName;       // Nombre del ítem de la semilla en el inventario (ej. "Semilla de Zanahoria")
    public string harvestedItemName;  // Nombre del ítem cosechado en el inventario (ej. "Zanahoria")
}

[System.Serializable]
public class PlantData
{
    public string plantName;
    public Sprite[] growthStages;
    public int currentStage;
    public float growthTime;
    public float currentGrowthProgress;
    public bool isPlanted;
    public bool isReadyToHarvest;
    public Vector3Int gridPosition;
    public string harvestedItemType; // Para saber qué ítem se cosecha

    public PlantData(Vector3Int pos)
    {
        gridPosition = pos;
        isPlanted = false;
        isReadyToHarvest = false;
        currentStage = 0;
        currentGrowthProgress = 0f;
        plantName = "";
        growthStages = null;
        growthTime = 0f;
        harvestedItemType = "";
    }

    public void AdvanceGrowth(float deltaTime)
    {
        if (!isReadyToHarvest && isPlanted)
        {
            currentGrowthProgress += deltaTime;
            if (growthTime > 0 && currentGrowthProgress >= growthTime)
            {
                currentGrowthProgress = growthTime;
                isReadyToHarvest = true;
            }

            if (growthStages != null && growthStages.Length > 0)
            {
                float stageProgress = currentGrowthProgress / growthTime;
                // Calcula la nueva etapa, asegurándose de no exceder el índice del array
                int newStage = Mathf.FloorToInt(stageProgress * growthStages.Length);
                currentStage = Mathf.Min(newStage, growthStages.Length - 1);
            }
        }
    }
}

public class PlantManager : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Tilemap plantTilemap;
    public Tile defaultGroundTile; // Tile para tierra arada
    public Tile barrenGroundTile; // Tile para tierra árida / sin arar

    [Header("UI Elementos")]
    public TMPro.TextMeshProUGUI infoText;     // Asignar desde el Inspector
    public TMPro.TextMeshProUGUI inventoryText; // Para mostrar el inventario

    [Header("Tipos de Plantas")]
    public PlantDefinition[] plantDefinitions; // Array de definiciones de plantas

    [Header("Referencias del Juego")]
    public Transform playerTransform; // ¡NUEVO! Asigna el objeto Player aquí en el Inspector

    // --- INVENTARIO ---
    private Dictionary<string, int> inventory = new Dictionary<string, int>();
    // --- FIN INVENTARIO ---

    private Camera mainCamera;
    private Dictionary<Vector3Int, PlantData> plantedAreas = new Dictionary<Vector3Int, PlantData>();

    // Eventos para que el MissionManager se suscriba
    public delegate void PlantEventHandler(PlantData plant);
    public static event PlantEventHandler OnPlantHarvested;

    void Start()
    {
        mainCamera = Camera.main;

        if (groundTilemap == null) Debug.LogError("Ground Tilemap no asignado en PlantManager.");
        if (plantTilemap == null) Debug.LogError("Plant Tilemap no asignado en PlantManager.");
        if (defaultGroundTile == null || barrenGroundTile == null) Debug.LogError("Tiles de terreno (arado/árido) no asignados.");
        if (infoText == null) Debug.LogError("Info Text (TextMeshProUGUI) no asignado en PlantManager. ¡Arrastra el objeto InfoText del Canvas al Inspector!");
        if (inventoryText == null) Debug.LogWarning("Inventory Text (TextMeshProUGUI) no asignado en PlantManager. Considera arrastrar un nuevo TextMeshProUGUI del Canvas al Inspector para ver el inventario.");
        if (playerTransform == null) Debug.LogError("Player Transform no asignado en PlantManager. ¡Arrastra el objeto Player del Hierarchy al Inspector!"); // ¡NUEVO!

        // --- INICIALIZACIÓN CRÍTICA DEL TERRENO ---
        groundTilemap.ClearAllTiles();

        if (groundTilemap.cellBounds.size.x == 0 || groundTilemap.cellBounds.size.y == 0)
        {
            Debug.LogWarning("Ground Tilemap no tiene Cell Bounds definidos (no hay tiles pintados en el editor). Inicializando un área de 20x20 para pruebas.");
            for (int x = -10; x < 10; x++)
            {
                for (int y = -10; y < 10; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    groundTilemap.SetTile(pos, barrenGroundTile);
                }
            }
        }
        else
        {
            foreach (var pos in groundTilemap.cellBounds.allPositionsWithin)
            {
                groundTilemap.SetTile(pos, barrenGroundTile);
            }
        }
        // --- FIN INICIALIZACIÓN CRÍTICA ---

        // --- INICIALIZACIÓN DEL INVENTARIO ---
        inventory["Semilla de Zanahoria"] = 5;
        inventory["Zanahoria"] = 0;
        inventory["Semilla de Tomate"] = 3;
        inventory["Tomate"] = 0;
        inventory["Semilla de Rosa"] = 2;
        inventory["Rosa"] = 0;
        UpdateInventoryUI();
    }

    void Update()
    {
        // --- CAMBIO DE LÓGICA DE INTERACCIÓN ---
        // Ahora se usa la tecla espacio y la posición del jugador
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (playerTransform != null)
            {
                // Convierte la posición del jugador a la posición de la celda en la cuadrícula
                Vector3Int playerCellPosition = groundTilemap.WorldToCell(playerTransform.position);

                // Solo interactúa si la celda existe en el groundTilemap
                if (groundTilemap.HasTile(playerCellPosition))
                {
                    HandleCellInteraction(playerCellPosition);
                }
                else
                {
                    DisplayMessage("El jugador no está sobre una celda de terreno válida.");
                }
            }
            else
            {
                Debug.LogError("Player Transform no está asignado en PlantManager. No se puede interactuar con el terreno.");
            }
        }

        // --- Fin CAMBIO DE LÓGICA DE INTERACCIÓN ---

        // Avanzar el crecimiento de las plantas (esta lógica se mantiene igual)
        List<Vector3Int> cellsToUpdate = new List<Vector3Int>(plantedAreas.Keys);
        foreach (var cellPosition in cellsToUpdate)
        {
            PlantData plant = plantedAreas[cellPosition];
            if (plant.isPlanted && !plant.isReadyToHarvest)
            {
                plant.AdvanceGrowth(Time.deltaTime);
                plantedAreas[cellPosition] = plant;
                UpdatePlantSprite(plant);
            }
        }
    }

    void HandleCellInteraction(Vector3Int cellPosition)
    {
        if (plantedAreas.ContainsKey(cellPosition))
        {
            PlantData plant = plantedAreas[cellPosition];
            if (plant.isPlanted && plant.isReadyToHarvest)
            {
                HarvestPlant(cellPosition, plant);
            }
            else if (plant.isPlanted)
            {
                DisplayMessage($"Planta de {plant.plantName} creciendo: {plant.currentGrowthProgress:F1}/{plant.growthTime:F1}");
            }
            else // La celda existe en plantedAreas pero no hay planta (tierra arada vacía)
            {
                // Intenta sembrar Zanahoria, si no hay, Tomate, y si no, Rosa.
                if (TrySeedPlant(cellPosition, "Semilla de Zanahoria")) return;
                if (TrySeedPlant(cellPosition, "Semilla de Tomate")) return;
                if (TrySeedPlant(cellPosition, "Semilla de Rosa")) return;

                DisplayMessage("No hay semillas disponibles de los tipos predeterminados para sembrar o no tienes ninguna.");
            }
        }
        else // La celda no está en plantedAreas, significa que no ha sido arada aún
        {
            TillSoil(cellPosition);
        }
    }

    void TillSoil(Vector3Int cellPosition)
    {
        if (groundTilemap.GetTile(cellPosition) == barrenGroundTile)
        {
            groundTilemap.SetTile(cellPosition, defaultGroundTile);
            DisplayMessage("¡Tierra arada!");
            plantedAreas.Add(cellPosition, new PlantData(cellPosition));
        }
        else if (groundTilemap.GetTile(cellPosition) == defaultGroundTile)
        {
            DisplayMessage("Esta tierra ya está arada.");
        }
    }

    bool TrySeedPlant(Vector3Int cellPosition, string seedInventoryName)
    {
        PlantDefinition plantDefToPlant = null;
        foreach (var def in plantDefinitions)
        {
            if (def.seedItemName == seedInventoryName)
            {
                plantDefToPlant = def;
                break;
            }
        }

        if (plantDefToPlant != null && inventory.ContainsKey(seedInventoryName) && inventory[seedInventoryName] > 0)
        {
            if (plantedAreas.ContainsKey(cellPosition) && !plantedAreas[cellPosition].isPlanted)
            {
                PlantData newPlant = plantedAreas[cellPosition];
                newPlant.plantName = plantDefToPlant.plantName;
                newPlant.growthStages = plantDefToPlant.growthSprites;
                newPlant.growthTime = plantDefToPlant.growthDuration;
                newPlant.isPlanted = true;
                newPlant.isReadyToHarvest = false;
                newPlant.currentStage = 0;
                newPlant.currentGrowthProgress = 0f;
                newPlant.harvestedItemType = plantDefToPlant.harvestedItemName;

                plantedAreas[cellPosition] = newPlant;
                UpdatePlantSprite(newPlant);

                inventory[seedInventoryName]--;
                UpdateInventoryUI();
                DisplayMessage($"¡Sembrada {newPlant.plantName}!");
                return true;
            }
        }
        return false;
    }

    void HarvestPlant(Vector3Int cellPosition, PlantData plant)
    {
        if (plant.isReadyToHarvest)
        {
            plantTilemap.SetTile(cellPosition, null);
            groundTilemap.SetTile(cellPosition, defaultGroundTile);

            string harvestedItem = plant.harvestedItemType;
            if (!string.IsNullOrEmpty(harvestedItem))
            {
                if (inventory.ContainsKey(harvestedItem))
                {
                    inventory[harvestedItem]++;
                }
                else
                {
                    inventory.Add(harvestedItem, 1);
                }
                UpdateInventoryUI();
                DisplayMessage($"¡Cosechada {plant.plantName}! Has obtenido {harvestedItem}.");
            }
            else
            {
                DisplayMessage($"¡Cosechada {plant.plantName}! (No se especificó un ítem de cosecha)");
            }

            plant.isPlanted = false;
            plant.isReadyToHarvest = false;
            plant.currentStage = 0;
            plant.currentGrowthProgress = 0f;
            plant.plantName = "";
            plant.growthStages = null;
            plant.harvestedItemType = "";

            plantedAreas[cellPosition] = plant;
        }
        else
        {
            DisplayMessage($"La planta de {plant.plantName} aún no está lista para cosechar.");
        }
    }

    void UpdatePlantSprite(PlantData plant)
    {
        if (plant.isPlanted && plant.growthStages != null && plant.growthStages.Length > 0)
        {
            Tile plantVisualTile = ScriptableObject.CreateInstance<Tile>();
            plantVisualTile.sprite = plant.growthStages[plant.currentStage];
            plantTilemap.SetTile(plant.gridPosition, plantVisualTile);
        }
        else
        {
            plantTilemap.SetTile(plant.gridPosition, null);
        }
    }

    void DisplayMessage(string message)
    {
        if (infoText != null)
        {
            infoText.text = message;
            CancelInvoke("ClearMessage");
            Invoke("ClearMessage", 3f);
        }
    }

    void ClearMessage()
    {
        if (infoText != null)
        {
            infoText.text = "";
        }
    }

    void UpdateInventoryUI()
    {
        if (inventoryText != null)
        {
            string inventoryString = "Inventario:\n";
            foreach (var item in inventory)
            {
                inventoryString += $"{item.Key}: {item.Value}\n";
            }
            inventoryText.text = inventoryString;
        }
    }
}