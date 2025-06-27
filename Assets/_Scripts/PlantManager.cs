using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using TMPro; // �IMPORTANTE! Necesario para TextMeshProUGUI

// Clase para definir los datos de una planta espec�fica
[System.Serializable]
public class PlantDefinition
{
    public string plantName;          // Nombre de la planta (ej. "Zanahoria")
    public GameObject seedPrefab;     // Prefab de la semilla (para referencia visual o futura instancia)
    public Sprite[] growthSprites;    // Sprites para cada etapa de crecimiento
    public float growthDuration;      // Tiempo total que tarda en crecer (en segundos)
    public Sprite harvestSprite;      // Sprite final al cosechar (opcional)

    // Nuevos campos para el inventario
    public string seedItemName;       // Nombre del �tem de la semilla en el inventario (ej. "Semilla de Zanahoria")
    public string harvestedItemName;  // Nombre del �tem cosechado en el inventario (ej. "Zanahoria")
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
    public string harvestedItemType; // Para saber qu� �tem se cosecha

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
                // Calcula la nueva etapa, asegur�ndose de no exceder el �ndice del array
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
    public Tile barrenGroundTile; // Tile para tierra �rida / sin arar

    [Header("UI Elementos")]
    public TMPro.TextMeshProUGUI infoText;     // Asignar desde el Inspector
    public TMPro.TextMeshProUGUI inventoryText; // �NUEVO! Para mostrar el inventario

    [Header("Tipos de Plantas")]
    public PlantDefinition[] plantDefinitions; // Array de definiciones de plantas

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
        if (defaultGroundTile == null || barrenGroundTile == null) Debug.LogError("Tiles de terreno (arado/�rido) no asignados.");
        if (infoText == null) Debug.LogError("Info Text (TextMeshProUGUI) no asignado en PlantManager. �Arrastra el objeto InfoText del Canvas al Inspector!");
        if (inventoryText == null) Debug.LogWarning("Inventory Text (TextMeshProUGUI) no asignado en PlantManager. Considera arrastrar un nuevo TextMeshProUGUI del Canvas al Inspector para ver el inventario."); // Mensaje para el nuevo UI

        // --- INICIALIZACI�N CR�TICA DEL TERRENO ---
        groundTilemap.ClearAllTiles(); // �Esta l�nea es CRUCIAL!

        if (groundTilemap.cellBounds.size.x == 0 || groundTilemap.cellBounds.size.y == 0)
        {
            Debug.LogWarning("Ground Tilemap no tiene Cell Bounds definidos (no hay tiles pintados en el editor). Inicializando un �rea de 20x20 para pruebas."); //
            for (int x = -10; x < 10; x++) // Ejemplo de rango manual
            {
                for (int y = -10; y < 10; y++) // Ejemplo de rango manual
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
        // --- FIN INICIALIZACI�N CR�TICA ---

        // --- INICIALIZACI�N DEL INVENTARIO ---
        // Puedes ajustar las cantidades iniciales aqu�
        inventory["Semilla de Zanahoria"] = 5;
        inventory["Zanahoria"] = 0; // Productos cosechados
        inventory["Semilla de Tomate"] = 3;
        inventory["Tomate"] = 0;
        inventory["Semilla de Rosa"] = 2;
        inventory["Rosa"] = 0;
        // --- Puedes a�adir m�s tipos de semillas o productos aqu� ---
        // Ejemplo:
        // inventory["Semilla de Manzana"] = 1;
        // inventory["Manzana"] = 0;
        // inventory["Semilla de Tulip�n"] = 4;
        // inventory["Tulip�n"] = 0;
        // -----------------------------------------------------------

        UpdateInventoryUI(); // Actualiza el UI del inventario al inicio
    }

    void Update()
    {
        // Nuevo sistema de entrada para detectar clic izquierdo
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector3Int clickedCell = groundTilemap.WorldToCell(mouseWorldPos);

            // Solo interact�a si la celda existe en el groundTilemap
            if (groundTilemap.HasTile(clickedCell))
            {
                HandleCellInteraction(clickedCell);
            }
        }

        // Avanzar el crecimiento de las plantas
        List<Vector3Int> cellsToUpdate = new List<Vector3Int>(plantedAreas.Keys);
        foreach (var cellPosition in cellsToUpdate)
        {
            PlantData plant = plantedAreas[cellPosition];
            if (plant.isPlanted && !plant.isReadyToHarvest)
            {
                plant.AdvanceGrowth(Time.deltaTime);
                plantedAreas[cellPosition] = plant; // Actualiza la PlantData en el diccionario
                UpdatePlantSprite(plant); // Actualiza el sprite visual de la planta
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
            else // La celda existe en plantedAreas pero no hay planta (tierra arada vac�a)
            {
                // Sembrar la primera semilla disponible (o la que se seleccione en el futuro)
                // Aqu�, podr�as a�adir l�gica para permitir al jugador elegir qu� sembrar.
                // Por ahora, intenta sembrar Zanahoria, si no hay, Tomate, y si no, Rosa.
                if (TrySeedPlant(cellPosition, "Semilla de Zanahoria")) return;
                if (TrySeedPlant(cellPosition, "Semilla de Tomate")) return;
                if (TrySeedPlant(cellPosition, "Semilla de Rosa")) return;
                // ... puedes a�adir m�s TrySeedPlant aqu� para otras semillas ...

                DisplayMessage("No hay semillas disponibles de los tipos predeterminados para sembrar o no tienes ninguna.");
            }
        }
        else // La celda no est� en plantedAreas, significa que no ha sido arada a�n
        {
            TillSoil(cellPosition);
        }
    }

    void TillSoil(Vector3Int cellPosition)
    {
        // Solo permite arar tierra �rida (barrenGroundTile)
        if (groundTilemap.GetTile(cellPosition) == barrenGroundTile)
        {
            groundTilemap.SetTile(cellPosition, defaultGroundTile); // Cambia a tierra arada
            DisplayMessage("�Tierra arada!");
            // A�ade la celda al diccionario, inicializada como vac�a
            plantedAreas.Add(cellPosition, new PlantData(cellPosition));
        }
        // Si el tile es defaultGroundTile, significa que ya est� arada, no se debe arar de nuevo
        else if (groundTilemap.GetTile(cellPosition) == defaultGroundTile)
        {
            DisplayMessage("Esta tierra ya est� arada.");
        }
    }

    // Nuevo m�todo para intentar sembrar una planta de un tipo espec�fico
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
                newPlant.harvestedItemType = plantDefToPlant.harvestedItemName; // Asigna el tipo de �tem cosechado

                plantedAreas[cellPosition] = newPlant;
                UpdatePlantSprite(newPlant);

                inventory[seedInventoryName]--; // Decrementa la cantidad de semillas
                UpdateInventoryUI(); // Actualiza el UI del inventario
                DisplayMessage($"�Sembrada {newPlant.plantName}!");
                return true;
            }
        }
        return false; // No se pudo sembrar por falta de semillas o la celda no est� lista
    }

    void HarvestPlant(Vector3Int cellPosition, PlantData plant)
    {
        if (plant.isReadyToHarvest)
        {
            plantTilemap.SetTile(cellPosition, null); // Elimina el sprite de la planta
            groundTilemap.SetTile(cellPosition, defaultGroundTile); // Asegura que el fondo sea tierra arada

            // A�ade el producto cosechado al inventario
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
                UpdateInventoryUI(); // Actualiza el UI del inventario
                DisplayMessage($"�Cosechada {plant.plantName}! Has obtenido {harvestedItem}.");
            }
            else
            {
                DisplayMessage($"�Cosechada {plant.plantName}! (No se especific� un �tem de cosecha)");
            }

            // Reestablece la celda a un estado "arado y vac�o", listo para una nueva siembra
            plant.isPlanted = false;
            plant.isReadyToHarvest = false;
            plant.currentStage = 0;
            plant.currentGrowthProgress = 0f;
            plant.plantName = "";
            plant.growthStages = null;
            plant.harvestedItemType = ""; // Limpia el tipo de �tem cosechado

            // Conserva la PlantData en el diccionario, solo actualiza su estado.
            plantedAreas[cellPosition] = plant;
        }
        else
        {
            DisplayMessage($"La planta de {plant.plantName} a�n no est� lista para cosechar.");
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
            plantTilemap.SetTile(plant.gridPosition, null); // Si no est� plantada o no tiene sprites, borra el tile
        }
    }

    // M�todos para mostrar y limpiar mensajes en el UI
    void DisplayMessage(string message)
    {
        if (infoText != null)
        {
            infoText.text = message;
            CancelInvoke("ClearMessage");
            Invoke("ClearMessage", 3f); // El mensaje desaparece despu�s de 3 segundos
        }
    }

    void ClearMessage()
    {
        if (infoText != null)
        {
            infoText.text = "";
        }
    }

    // --- NUEVO M�TODO PARA ACTUALIZAR EL UI DEL INVENTARIO ---
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
    // --- FIN NUEVO M�TODO ---
}