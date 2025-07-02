﻿using UnityEngine;
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
        isPlanted = false; // Inicialmente no plantada
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

            if (growthStages != null && growthStages.Length > 0 && growthTime > 0) // Añadida comprobación growthTime > 0
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
    public TMPro.TextMeshProUGUI infoText;      // Asignar desde el Inspector
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
    public static event PlantEventHandler OnPlantHarvested; // ¡Este es el evento!

    void Start()
    {
        mainCamera = Camera.main;

        if (groundTilemap == null) Debug.LogError("Ground Tilemap no asignado en PlantManager.");
        if (plantTilemap == null) Debug.LogError("Plant Tilemap no asignado en PlantManager.");
        if (defaultGroundTile == null || barrenGroundTile == null) Debug.LogError("Tiles de terreno (arado/árido) no asignados.");
        if (infoText == null) Debug.LogError("Info Text (TextMeshProUGUI) no asignado en PlantManager. ¡Arrastra el objeto InfoText del Canvas al Inspector!");
        if (inventoryText == null) Debug.LogWarning("Inventory Text (TextMeshProUGUI) no asignado en PlantManager. Considera arrastrar un nuevo TextMeshProUGUI del Canvas al Inspector para ver el inventario.");
        if (playerTransform == null) Debug.LogError("Player Transform no asignado en PlantManager. ¡Arrastra el objeto Player del Hierarchy al Inspector!");

        // --- INICIALIZACIÓN CRÍTICA DEL TERRENO ---
        // Asegúrate de que el Tilemap exista y sea accesible
        if (groundTilemap != null)
        {
            groundTilemap.ClearAllTiles();

            // Si el Tilemap tiene bounds definidos (ya se pintaron tiles en el editor)
            if (groundTilemap.cellBounds.size.x > 0 && groundTilemap.cellBounds.size.y > 0)
            {
                foreach (var pos in groundTilemap.cellBounds.allPositionsWithin)
                {
                    // Asegúrate de que solo asignas a celdas que realmente existen y son accesibles
                    if (groundTilemap.GetTile(pos) == null) // Solo si la celda está vacía
                    {
                        groundTilemap.SetTile(pos, barrenGroundTile);
                    }
                }
            }
            else
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
        }
        else
        {
            Debug.LogError("Ground Tilemap es null al inicio, no se puede inicializar el terreno.");
        }
        // --- FIN INICIALIZACIÓN CRÍTICA ---

        // --- INICIALIZACIÓN DEL INVENTARIO ---
        inventory["Semilla de Zanahoria"] = 5;
        inventory["Zanahoria"] = 0;
        inventory["Semilla de Tomate"] = 3;
        inventory["Tomate"] = 0;
        inventory["Semilla de Uva"] = 3;
        inventory["Uva"] = 0;
        inventory["Semilla de Platano"] = 3;
        inventory["Platano"] = 0;
        inventory["Semilla de Girasol"] = 3;
        inventory["Girasol"] = 0;
        inventory["Semilla de Tulipan"] = 3;
        inventory["Tulipan"] = 0;
        UpdateInventoryUI();
    }

    void Update()
    {
        // Debug.Log("Update method called."); // Verifica que Update se está ejecutando
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Space key pressed!"); // Confirma que la tecla espacio se detecta
            if (playerTransform != null)
            {
                Vector3Int playerCellPosition = groundTilemap.WorldToCell(playerTransform.position);
                playerCellPosition.z = 0; // Asegurarse de que el Z sea 0 para Tilemaps 2D

                // Debug.Log($"Player at Grid: {playerCellPosition}"); // Muestra la posición de la celda del jugador

                if (groundTilemap.HasTile(playerCellPosition))
                {
                    HandleCellInteraction(playerCellPosition);
                }
                else
                {
                    DisplayMessage("El jugador no está sobre una celda de terreno válida.");
                    Debug.LogWarning($"No hay tile en la posición: {playerCellPosition}");
                }
            }
            else
            {
                Debug.LogError("Player Transform no está asignado en PlantManager. No se puede interactuar con el terreno.");
            }
        }

        List<Vector3Int> cellsToUpdate = new List<Vector3Int>(plantedAreas.Keys);
        foreach (var cellPosition in cellsToUpdate)
        {
            PlantData plant = plantedAreas[cellPosition];
            if (plant.isPlanted && !plant.isReadyToHarvest)
            {
                plant.AdvanceGrowth(Time.deltaTime);
                plantedAreas[cellPosition] = plant; // Asegura que los cambios se guarden en el diccionario
                UpdatePlantSprite(plant);
            }
        }
    }

    void HandleCellInteraction(Vector3Int cellPosition)
    {
        Debug.Log($"Handling interaction at: {cellPosition}");
        // Primero, verifica si la celda ya tiene un PlantData
        if (plantedAreas.ContainsKey(cellPosition))
        {
            PlantData plant = plantedAreas[cellPosition];

            // Si hay una planta plantada
            if (plant.isPlanted)
            {
                // Si está lista para cosechar
                if (plant.isReadyToHarvest)
                {
                    Debug.Log("Planta lista para cosechar.");
                    HarvestPlant(cellPosition, plant);
                }
                // Si no está lista para cosechar (sigue creciendo)
                else
                {
                    DisplayMessage($"Planta de {plant.plantName} creciendo: {plant.currentGrowthProgress:F1}/{plant.growthTime:F1}");
                    Debug.Log("Planta en crecimiento. No lista para cosechar.");
                }
            }
            // Si la celda existe en plantedAreas pero no tiene una planta "plantada" (ej. está arada pero vacía)
            else
            {
                Debug.Log("Terreno arado, intentando sembrar...");
                // Intenta sembrar las semillas en orden de prioridad
                if (TrySeedPlant(cellPosition, "Semilla de Zanahoria")) return;
                if (TrySeedPlant(cellPosition, "Semilla de Tomate")) return;
                if (TrySeedPlant(cellPosition, "Semilla de Uva")) return;
                if (TrySeedPlant(cellPosition, "Semilla de Platano")) return;
                if (TrySeedPlant(cellPosition, "Semilla de Girasol")) return;
                if (TrySeedPlant(cellPosition, "Semilla de Tulipan")) return;

                DisplayMessage("No hay semillas disponibles de los tipos predeterminados para sembrar o no tienes ninguna.");
                Debug.Log("Fallo al sembrar: No se encontró semilla disponible o inventario vacío."); // Este es el mensaje que recibiste
            }
        }
        // Si la celda NO existe en plantedAreas (es tierra árida virgen)
        else
        {
            Debug.Log("Terreno árido, intentando arar...");
            TillSoil(cellPosition);
        }
    }

    void TillSoil(Vector3Int cellPosition)
    {
        // Verifica si realmente es tierra árida antes de arar
        if (groundTilemap.GetTile(cellPosition) == barrenGroundTile)
        {
            groundTilemap.SetTile(cellPosition, defaultGroundTile); // <-- AQUÍ CAMBIA EL SPRITE
            DisplayMessage("¡Tierra arada!");
            plantedAreas.Add(cellPosition, new PlantData(cellPosition));
            Debug.Log($"Celda {cellPosition} arada y PlantData creada (isPlanted=false).");
        }
        else if (groundTilemap.GetTile(cellPosition) == defaultGroundTile)
        {
            DisplayMessage("Esta tierra ya está arada.");
            Debug.Log($"Celda {cellPosition} ya está arada.");
        }
        else
        {
            DisplayMessage("No se puede arar este tipo de terreno.");
            Debug.Log($"Celda {cellPosition} no es árida ni arada. No se puede arar.");
        }
    }

    bool TrySeedPlant(Vector3Int cellPosition, string seedInventoryName)
    {
        Debug.Log($"Intentando sembrar con: {seedInventoryName} en {cellPosition}");
        PlantDefinition plantDefToPlant = null;
        // Busca la definición de la planta por el nombre de la semilla
        foreach (var def in plantDefinitions)
        {
            if (def.seedItemName == seedInventoryName)
            {
                plantDefToPlant = def;
                Debug.Log($"Definición de planta encontrada para {seedInventoryName}. Plant Name: {def.plantName}");
                break;
            }
        }

        if (plantDefToPlant != null)
        {
            Debug.Log($"PlantDefToPlant no es null para {seedInventoryName}.");
            // Verifica si el jugador tiene la semilla en el inventario
            if (inventory.ContainsKey(seedInventoryName) && inventory[seedInventoryName] > 0)
            {
                Debug.Log($"Inventario tiene {inventory[seedInventoryName]} de {seedInventoryName}.");
                // Verifica que la celda exista en plantedAreas y no tenga una planta ya activa
                if (plantedAreas.ContainsKey(cellPosition) && !plantedAreas[cellPosition].isPlanted)
                {
                    Debug.Log("Celda válida para sembrar (arada y vacía).");
                    PlantData newPlant = plantedAreas[cellPosition]; // Obtén la PlantData existente
                    newPlant.plantName = plantDefToPlant.plantName;
                    newPlant.growthStages = plantDefToPlant.growthSprites;
                    newPlant.growthTime = plantDefToPlant.growthDuration;
                    newPlant.isPlanted = true;
                    newPlant.isReadyToHarvest = false;
                    newPlant.currentStage = 0;
                    newPlant.currentGrowthProgress = 0f;
                    newPlant.harvestedItemType = plantDefToPlant.harvestedItemName;

                    plantedAreas[cellPosition] = newPlant; // Guarda los cambios en el diccionario
                    UpdatePlantSprite(newPlant);

                    inventory[seedInventoryName]--;
                    UpdateInventoryUI();
                    DisplayMessage($"¡Sembrada {newPlant.plantName}!");
                    Debug.Log($"¡{newPlant.plantName} sembrada exitosamente en {cellPosition}!");
                    return true;
                }
                else
                {
                    Debug.Log($"Fallo al sembrar {seedInventoryName}: La celda {cellPosition} ya tiene una planta o no está arada.");
                }
            }
            else
            {
                Debug.Log($"Fallo al sembrar {seedInventoryName}: No hay suficientes semillas en el inventario.");
            }
        }
        else
        {
            Debug.Log($"Fallo al sembrar {seedInventoryName}: No se encontró la definición de planta para esta semilla.");
        }
        return false;
    }

    void HarvestPlant(Vector3Int cellPosition, PlantData plant)
    {
        if (plant.isReadyToHarvest)
        {
            plantTilemap.SetTile(cellPosition, null); // Quita la planta del tilemap de plantas
            groundTilemap.SetTile(cellPosition, defaultGroundTile); // Devuelve la tierra a arado

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
                DisplayMessage($"¡Cosechada {plant.plantName}! Has obtenido 1 {harvestedItem}.");

                // ¡NUEVA LÍNEA CLAVE PARA QUE AVANCEN LAS MISIONES!!!
                // Se invoca el evento, pasando los datos completos de la planta
                OnPlantHarvested?.Invoke(plant);
            }
            else
            {
                DisplayMessage($"¡Cosechada {plant.plantName}! (No se especificó un ítem de cosecha)");
            }

            // Resetea los datos de la planta para que la celda esté lista para una nueva siembra
            plant.isPlanted = false;
            plant.isReadyToHarvest = false;
            plant.currentStage = 0;
            plant.currentGrowthProgress = 0f;
            plant.plantName = "";
            plant.growthStages = null;
            plant.harvestedItemType = "";

            plantedAreas[cellPosition] = plant; // Guarda los cambios reseteados
            Debug.Log($"Planta {plant.plantName} cosechada en {cellPosition}. Celda reseteada.");
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
            // Crea una nueva instancia de Tile (esto ya lo tienes)
            Tile plantVisualTile = ScriptableObject.CreateInstance<Tile>();

            // Asigna el sprite de la etapa actual (esto ya lo tienes)
            plantVisualTile.sprite = plant.growthStages[plant.currentStage];

            // ¡IMPORTANTE! Asegúrate de que el color del tile sea blanco
            // Esto elimina cualquier tintado que pudiera venir por defecto o de otra configuración
            plantVisualTile.color = Color.white;

            // Establece el tile en el Tilemap de plantas (esto ya lo tienes)
            plantTilemap.SetTile(plant.gridPosition, plantVisualTile);
        }
        else
        {
            // Si no hay planta o no tiene sprites, borra el tile
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