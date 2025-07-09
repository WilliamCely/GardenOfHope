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

    // --- NUEVOS CAMPOS PARA PLANTAS SECAS ---
    public Sprite witheredSprite;     // ¡NUEVO! Sprite cuando la planta se seca
    public float witheredDuration;    // ¡NUEVO! Tiempo que tarda en secarse después de estar lista para cosechar
    // ---------------------------------------

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
    public string harvestedItemType;
    public bool isWatered; // ¡NUEVO! Para el estado de riego de la planta
    public float timeSinceLastWatered; // ¡NUEVO! Para controlar el riego

    // --- NUEVOS CAMPOS PARA PLANTAS SECAS ---
    public bool isWithered;           // ¡NUEVO! Indica si la planta está seca
    public float timeSinceReadyToHarvest; // ¡NUEVO! Contador de tiempo desde que maduró
    public Sprite witheredSprite;         // ¡NUEVO! Almacena el sprite de marchitamiento
    public float witheredDuration;        // ¡NUEVO! Almacena la duración hasta marchitarse
    // ---------------------------------------

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
        isWatered = false; // Inicialmente no regada
        timeSinceLastWatered = 0f;

        // --- INICIALIZAR NUEVOS CAMPOS ---
        isWithered = false;
        timeSinceReadyToHarvest = 0f;
        witheredSprite = null;
        witheredDuration = 0f;
        // ----------------------------------
    }

    public void AdvanceGrowth(float deltaTime)
    {
        if (isPlanted) // Asegurarse de que esté plantada
        {
            // Resetear el estado de regado después de un tiempo para requerir riego periódico
            if (isWatered)
            {
                timeSinceLastWatered += deltaTime;
                // Asumiendo que la planta necesita ser regada cada X segundos para continuar creciendo
                if (timeSinceLastWatered >= 10f) // Ejemplo: necesita ser regada cada 10 segundos
                {
                    isWatered = false;
                    timeSinceLastWatered = 0f;
                    // Debug.Log($"Planta {plantName} en {gridPosition} necesita ser regada de nuevo.");
                }
            }


            if (!isReadyToHarvest && !isWithered) // Si aún no está lista para cosechar y no está seca
            {
                // Solo crece si está regada
                if (isWatered)
                {
                    currentGrowthProgress += deltaTime;
                    if (growthTime > 0 && currentGrowthProgress >= growthTime)
                    {
                        currentGrowthProgress = growthTime;
                        isReadyToHarvest = true;
                        Debug.Log($"Planta {plantName} en {gridPosition} está lista para cosechar!");
                    }
                }
                else
                {
                    // Debug.Log($"Planta {plantName} en {gridPosition} no está regada, crecimiento pausado.");
                }
            }
            // --- NUEVA LÓGICA: Si está lista para cosechar, empieza a secarse ---
            else if (isReadyToHarvest && !isWithered) // Si está lista para cosechar pero no seca
            {
                timeSinceReadyToHarvest += deltaTime; // Empieza a contar el tiempo para secarse

                // Si ha pasado suficiente tiempo desde que maduró, la planta se seca
                if (witheredDuration > 0 && timeSinceReadyToHarvest >= witheredDuration)
                {
                    isWithered = true;
                    isReadyToHarvest = false; // Ya no está lista para cosechar, ahora está seca
                    Debug.Log($"La planta {plantName} en {gridPosition} se ha secado.");
                }
            }
            // -------------------------------------------------------------------

            // Actualiza la etapa visual si NO está seca y tiene sprites de crecimiento
            if (!isWithered && growthStages != null && growthStages.Length > 0 && growthTime > 0)
            {
                float stageProgress = currentGrowthProgress / growthTime;
                int newStage = Mathf.FloorToInt(stageProgress * growthStages.Length);
                currentStage = Mathf.Min(newStage, growthStages.Length - 1);
            }
        }
    }
}

[RequireComponent(typeof(AudioSource))] // Asegura que haya un AudioSource en este GameObject
public class PlantManager : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Tilemap plantTilemap;
    public Tile defaultGroundTile; // Tile para tierra arada (este ya no se usa, usar plowedGroudTile)
    public Tile barrenGroundTile; // Tile para tierra árida / sin arar
    public Tile plowedGroudTile; // Tile para tierra arado

    [Header("UI Elementos")]
    public TMPro.TextMeshProUGUI infoText;        // Asignar desde el Inspector
    public TMPro.TextMeshProUGUI inventoryText; // Para mostrar el inventario

    [Header("Tipos de Plantas")]
    public PlantDefinition[] plantDefinitions; // Array de definiciones de plantas

    [Header("Referencias del Juego")]
    public Transform playerTransform; // ¡NUEVO! Asigna el objeto Player aquí en el Inspector

    // --- NUEVO: CONFIGURACIÓN DE SONIDOS ---
    [Header("Sonidos de Acciones")]
    public AudioClip tillSoilSound;      // Sonido para arar
    public AudioClip plantSeedSound;     // Sonido para sembrar
    public AudioClip waterPlantSound;    // Sonido para regar
    public AudioClip harvestPlantSound;  // Sonido para cosechar
    public AudioClip witheredPlantSound; // Sonido cuando la planta se seca
    public AudioClip missionCompleteSound; // Sonido cuando se completa una misión

    private AudioSource audioSource; // Referencia al componente AudioSource
    // --- FIN NUEVO: CONFIGURACIÓN DE SONIDOS ---

    // NEW: Color para el efecto de regado
    public Color wateredColor = new Color(0.7f, 0.9f, 1.0f, 1.0f); // Un azul claro/cielo para indicar regado
    public Color defaultPlantColor = Color.white; // Color normal de la planta

    // --- INVENTARIO Y SELECCIÓN DE SEMILLAS ---
    private Dictionary<string, int> inventory = new Dictionary<string, int>();
    private string currentSelectedSeed; // La semilla que el jugador ha seleccionado para plantar
    private int currentSeedIndex = 0;   // Índice de la semilla actual en availableSeedTypes
    private List<string> availableSeedTypes = new List<string>(); // Nombres de todas las semillas definidas
    // --- FIN INVENTARIO Y SELECCIÓN DE SEMILLAS ---

    private Camera mainCamera;
    private Dictionary<Vector3Int, PlantData> plantedAreas = new Dictionary<Vector3Int, PlantData>();

    // Eventos para que el MissionManager se suscriba
    public delegate void PlantEventHandler(PlantData plant);
    public static event PlantEventHandler OnPlantHarvested;
    public static event PlantEventHandler OnPlantPlanted; // ¡NUEVO EVENTO! Para misiones de sembrar
    public static event PlantEventHandler OnPlantWatered; // ¡NUEVO EVENTO! Para misiones de regar

    void Start()
    {
        mainCamera = Camera.main;

        // --- Obtener el AudioSource al inicio ---
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("No se encontró un componente AudioSource en el GameObject de PlantManager.");
        }
        // ----------------------------------------

        if (groundTilemap == null) Debug.LogError("Ground Tilemap no asignado en PlantManager.");
        if (plantTilemap == null) Debug.LogError("Plant Tilemap no asignado en PlantManager.");
        if (barrenGroundTile == null || plowedGroudTile == null) Debug.LogError("Tiles de terreno (árido/arado) no asignados. Asegúrate de asignar 'barrenGroundTile' y 'plowedGroudTile'.");
        if (infoText == null) Debug.LogError("Info Text (TextMeshProUGUI) no asignado en PlantManager. ¡Arrastra el objeto InfoText del Canvas al Inspector!");
        if (inventoryText == null) Debug.LogWarning("Inventory Text (TextMeshProUGUI) no asignado en PlantManager. Considera arrastrar un nuevo TextMeshProUGUI del Canvas al Inspector para ver el inventario.");
        if (playerTransform == null) Debug.LogError("Player Transform no asignado en PlantManager. ¡Arrastra el objeto Player del Hierarchy al Inspector!");

        // --- INICIALIZACIÓN CRÍTICA DEL TERRENO ---
        if (groundTilemap != null)
        {
            groundTilemap.ClearAllTiles();

            // Si el Tilemap tiene bounds definidos (ya se pintaron tiles en el editor)
            if (groundTilemap.cellBounds.size.x > 0 && groundTilemap.cellBounds.size.y > 0)
            {
                foreach (var pos in groundTilemap.cellBounds.allPositionsWithin)
                {
                    if (groundTilemap.GetTile(pos) == null) // Solo si la celda está vacía
                    {
                        groundTilemap.SetTile(pos, barrenGroundTile);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Ground Tilemap no tiene Cell Bounds definidos (no hay tiles pintados en el editor). Inicializando un área para pruebas.");

                int halfSize = 50; // Esto creará un área de 100x100 (de -50 a +49)
                for (int x = -halfSize; x < halfSize; x++)
                {
                    for (int y = -halfSize; y < halfSize; y++)
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

        // --- INICIALIZACIÓN DEL INVENTARIO Y TIPOS DE SEMILLAS ---
        // Aseguramos que todas las semillas de las definiciones estén en el inventario, incluso con 0 cantidad.
        foreach (var def in plantDefinitions)
        {
            if (!inventory.ContainsKey(def.seedItemName))
            {
                inventory[def.seedItemName] = 0; // Inicializa a 0 si no existe
            }
            if (!availableSeedTypes.Contains(def.seedItemName))
            {
                availableSeedTypes.Add(def.seedItemName);
            }
            // También asegura que el ítem cosechado esté en el inventario
            if (!inventory.ContainsKey(def.harvestedItemName))
            {
                inventory[def.harvestedItemName] = 0;
            }
        }

        // Añadir algunas semillas iniciales para prueba
        inventory["Semilla de Zanahoria"] = 25;
        inventory["Semilla de Tomate"] = 28;
        inventory["Semilla de Uva"] = 30;
        inventory["Semilla de Platano"] = 25;
        inventory["Semilla de Girasol"] = 31;
        inventory["Semilla de Tulipan"] = 23;

        // Establecer la semilla inicial seleccionada
        if (availableSeedTypes.Count > 0)
        {
            currentSeedIndex = 0; // Ensure initial index is valid
            currentSelectedSeed = availableSeedTypes[currentSeedIndex];
        }
        else
        {
            Debug.LogWarning("No hay definiciones de plantas con ítems de semillas. La selección de semillas no funcionará.");
            currentSelectedSeed = "Ninguna Semilla Disponible";
        }

        UpdateInventoryUI(); // Muestra el estado inicial del inventario y la semilla seleccionada

        // Suscribirse al evento de misión completada (asumiendo que MissionManager lo invoca)
        // Necesitarás una referencia a tu MissionManager o un evento estático en MissionManager
        // Ejemplo (si MissionManager.OnMissionCompleted es estático):
        // MissionManager.OnMissionCompleted += PlayMissionCompleteSound;
        // Si no es estático, necesitarás obtener la instancia de MissionManager
    }

    void OnEnable()
    {
        // Suscribir al evento de plantas secas del propio PlantManager si se invoca desde aquí
        // Actualmente, la lógica de 'seca' está en PlantData, pero el PlantManager debería "darse cuenta" y reproducir el sonido.
        // Podríamos modificar PlantData para que invoque un evento, o hacer que PlantManager detecte el cambio de isWithered.
        // Por ahora, el sonido de marchitar se reproducirá en el ResetPlantCell si la planta estaba seca.

        // ¡IMPORTANTE! Si MissionManager tiene un evento OnMissionCompleted, suscríbete aquí:
        // MissionManager.OnMissionCompleted += PlayMissionCompleteSound;
    }

    void OnDisable()
    {
        // ¡IMPORTANTE! Desuscribirse del evento cuando el objeto está deshabilitado
        // MissionManager.OnMissionCompleted -= PlayMissionCompleteSound;
    }


    void Update()
    {
        // Interacción con el terreno (Space)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Space key pressed!");
            if (playerTransform != null)
            {
                Vector3Int playerCellPosition = groundTilemap.WorldToCell(playerTransform.position);
                playerCellPosition.z = 0; // Asegurarse de que el Z sea 0 para Tilemaps 2D

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

        // Ciclar semillas (Tab)
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            CycleSeeds();
        }

        // Actualizar el crecimiento de las plantas
        List<Vector3Int> cellsToUpdate = new List<Vector3Int>(plantedAreas.Keys);
        foreach (var cellPosition in cellsToUpdate)
        {
            PlantData plant = plantedAreas[cellPosition];
            // Solo avanza el crecimiento si está plantada y no seca
            // La lógica de isWatered ya está dentro de AdvanceGrowth
            if (plant.isPlanted && !plant.isWithered)
            {
                bool wasWithered = plant.isWithered; // Guarda el estado antes de avanzar
                plant.AdvanceGrowth(Time.deltaTime);
                plantedAreas[cellPosition] = plant; // Asegura que los cambios se guarden en el diccionario
                UpdatePlantSprite(plant); // Update sprite always, even if not growing, to reflect watered state

                // Si la planta se acaba de marchitar en este frame
                if (!wasWithered && plant.isWithered)
                {
                    PlaySound(witheredPlantSound); // Reproduce el sonido de marchitar
                }
            }
            // Si está seca, solo actualiza el sprite para asegurar que se vea marchita
            else if (plant.isPlanted && plant.isWithered)
            {
                UpdatePlantSprite(plant);
            }
        }
    }

    // --- NUEVO MÉTODO: Para ciclar entre los tipos de semillas disponibles ---
    void CycleSeeds()
    {
        if (availableSeedTypes.Count == 0)
        {
            DisplayMessage("No hay tipos de semillas definidos para seleccionar.");
            return;
        }

        currentSeedIndex = (currentSeedIndex + 1) % availableSeedTypes.Count;
        currentSelectedSeed = availableSeedTypes[currentSeedIndex];
        DisplayMessage($"Semilla seleccionada: {currentSelectedSeed}");
        UpdateInventoryUI(); // Actualiza la UI para mostrar la nueva semilla seleccionada
    }
    // -----------------------------------------------------------------------

    void HandleCellInteraction(Vector3Int cellPosition)
    {
        Debug.Log($"Handling interaction at: {cellPosition}");
        // Primero, verifica si la celda ya tiene un PlantData
        if (plantedAreas.ContainsKey(cellPosition))
        {
            PlantData plant = plantedAreas[cellPosition];

            if (plant.isPlanted)
            {
                // Si está seca, solo se puede "arar" (limpiar)
                if (plant.isWithered)
                {
                    DisplayMessage("La planta se ha secado. Terreno arado.");
                    Debug.Log($"Planta {plant.plantName} en {cellPosition} está seca. Se va a resetear la celda.");
                    ResetPlantCell(cellPosition, plant); // Llama al método de reinicio de celda
                    // El sonido de marchitar se reproduce cuando se detecta el cambio a isWithered en Update
                    // o aquí si se activa por interacción: PlaySound(witheredPlantSound);
                }
                else if (plant.isReadyToHarvest)
                {
                    Debug.Log("Planta lista para cosechar.");
                    HarvestPlant(cellPosition, plant);
                }
                else // Planta en crecimiento
                {
                    // ¡NUEVO!: Opción para regar la planta
                    if (!plant.isWatered)
                    {
                        WaterPlant(cellPosition, plant); // Llama al método de regar
                    }
                    else
                    {
                        DisplayMessage($"Planta de {plant.plantName} creciendo: {plant.currentGrowthProgress:F1}/{plant.growthTime:F1}. Ya está regada.");
                        Debug.Log("Planta en crecimiento. No lista para cosechar y ya regada.");
                    }
                }
            }
            // Si la celda existe en plantedAreas pero no tiene una planta "plantada" (ej. está arada pero vacía)
            else
            {
                Debug.Log("Terreno arado y vacío, intentando sembrar con la semilla seleccionada...");
                // Intenta sembrar SOLO la semilla actualmente seleccionada
                if (currentSelectedSeed != "Ninguna Semilla Disponible" && TrySeedPlant(cellPosition, currentSelectedSeed))
                {
                    // Éxito al sembrar
                }
                else
                {
                    DisplayMessage($"No puedes sembrar {currentSelectedSeed}. Revisa tu inventario o selecciona otra semilla (tecla Tab).");
                    Debug.Log($"Fallo al sembrar: No se pudo sembrar {currentSelectedSeed}.");
                }
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
        Debug.Log($"TillSoil: Intentando arar la celda en la posición de grilla: {cellPosition}");
        TileBase currentTile = groundTilemap.GetTile(cellPosition);

        if (currentTile != null)
        {
            Debug.Log($"TillSoil: Tile actual en {cellPosition}: {currentTile.name}");
        }
        else
        {
            Debug.Log($"TillSoil: No hay Tile en {cellPosition} (o es nulo).");
        }

        Debug.Log($"TillSoil: barrenGroundTile asignado: {(barrenGroundTile != null ? barrenGroundTile.name : "NULL")}");
        Debug.Log($"TillSoil: plowedGroudTile asignado: {(plowedGroudTile != null ? plowedGroudTile.name : "NULL")}");

        // Verifica si realmente es tierra árida antes de arar
        if (groundTilemap.GetTile(cellPosition) == barrenGroundTile)
        {
            Debug.Log($"TillSoil: Condición 'es tierra árida' CUMPLIDA. Cambiando sprite.");
            groundTilemap.SetTile(cellPosition, plowedGroudTile); // <-- AQUÍ CAMBIA EL SPRITE
            DisplayMessage("¡Tierra arada!");
            // Asegúrate de añadir una nueva PlantData limpia para la celda arada
            plantedAreas[cellPosition] = new PlantData(cellPosition);
            Debug.Log($"Celda {cellPosition} arada y PlantData creada (isPlanted=false).");
            PlaySound(tillSoilSound); // Reproduce el sonido de arado
        }
        else if (groundTilemap.GetTile(cellPosition) == plowedGroudTile)
        {
            Debug.Log($"TillSoil: Condición 'ya está arada' CUMPLIDA. No se hace nada.");
            DisplayMessage("Esta tierra ya está arada.");
            Debug.Log($"Celda {cellPosition} ya está arada.");
        }
        else
        {
            Debug.Log($"TillSoil: Ninguna condición anterior CUMPLIDA. Tile actual no es árido ni arado.");
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
                    newPlant.isWatered = true; // La planta recién sembrada se considera regada al inicio
                    newPlant.timeSinceLastWatered = 0f;

                    // --- Asignar propiedades de marchitamiento ---
                    newPlant.witheredSprite = plantDefToPlant.witheredSprite;
                    newPlant.witheredDuration = plantDefToPlant.witheredDuration;
                    newPlant.isWithered = false; // Asegurar que no está seca al sembrar
                    newPlant.timeSinceReadyToHarvest = 0f; // Resetear contador
                    // ----------------------------------------------------

                    plantedAreas[cellPosition] = newPlant; // Guarda los cambios en el diccionario
                    UpdatePlantSprite(newPlant); // Update the visual for the newly planted sprite

                    inventory[seedInventoryName]--;
                    UpdateInventoryUI();
                    DisplayMessage($"¡Sembrada {newPlant.plantName}!");
                    Debug.Log($"¡{newPlant.plantName} sembrada exitosamente en {cellPosition}!");

                    PlaySound(plantSeedSound); // Reproduce el sonido de siembra

                    // ¡NUEVO!: Invoca el evento OnPlantPlanted
                    OnPlantPlanted?.Invoke(newPlant);

                    return true;
                }
                else
                {
                    Debug.Log($"Fallo al sembrar {seedInventoryName}: La celda {cellPosition} ya tiene una planta o no está arada.");
                }
            }
            else
            {
                DisplayMessage($"No tienes suficientes {seedInventoryName} en el inventario.");
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

                PlaySound(harvestPlantSound); // Reproduce el sonido de cosecha

                // Invoca el evento OnPlantHarvested
                OnPlantHarvested?.Invoke(plant);
            }
            else
            {
                DisplayMessage($"¡Cosechada {plant.plantName}! (No se especificó un ítem de cosecha)");
            }

            // Llama al nuevo método para resetear la celda y los tiles
            ResetPlantCell(cellPosition, plant);
            Debug.Log($"Planta {plant.plantName} cosechada en {cellPosition}. Celda reseteada.");
        }
        else
        {
            DisplayMessage($"La planta de {plant.plantName} aún no está lista para cosechar.");
        }
    }

    // --- NUEVO MÉTODO: Para regar una planta ---
    void WaterPlant(Vector3Int cellPosition, PlantData plant)
    {
        if (plant.isPlanted && !plant.isReadyToHarvest && !plant.isWithered)
        {
            if (!plant.isWatered)
            {
                plant.isWatered = true;
                plant.timeSinceLastWatered = 0f; // Reinicia el contador de tiempo de regado
                plantedAreas[cellPosition] = plant; // Guarda los cambios

                DisplayMessage($"¡Regada la planta de {plant.plantName}!");
                Debug.Log($"Planta {plant.plantName} en {cellPosition} ha sido regada.");

                // Apply the watered color immediately
                UpdatePlantSprite(plant);
                PlaySound(waterPlantSound); // Reproduce el sonido de regar

                // ¡NUEVO!: Invoca el evento OnPlantWatered
                OnPlantWatered?.Invoke(plant);
            }
            else
            {
                DisplayMessage($"La planta de {plant.plantName} ya está regada.");
            }
        }
        else
        {
            DisplayMessage($"No puedes regar esta celda.");
        }
    }
    // ------------------------------------------

    // --- NUEVO MÉTODO: Reinicia los datos de la celda y los tiles visuales ---
    void ResetPlantCell(Vector3Int cellPosition, PlantData plant)
    {
        plantTilemap.SetTile(cellPosition, null); // Quita la planta del tilemap de plantas
        groundTilemap.SetTile(cellPosition, plowedGroudTile); // Devuelve la tierra a arado

        // Resetea los datos de la PlantData para que la celda esté lista para una nueva siembra
        plant.isPlanted = false;
        plant.isReadyToHarvest = false;
        // Si la planta estaba seca, ya se reproduce el sonido en Update o en HandleCellInteraction
        plant.isWithered = false; // ¡IMPORTANTE! Resetear también el estado de seca
        plant.currentStage = 0;
        plant.currentGrowthProgress = 0f;
        plant.timeSinceReadyToHarvest = 0f; // ¡IMPORTANTE! Resetear el contador de secado
        plant.isWatered = false; // Reiniciar estado de regado
        plant.timeSinceLastWatered = 0f;

        // Opcional: podrías resetear otros campos de `plant` si prefieres una PlantData "virgen`
        plant.plantName = "";
        plant.growthStages = null;
        plant.harvestedItemType = "";
        plant.witheredSprite = null;
        plant.witheredDuration = 0f;

        plantedAreas[cellPosition] = plant; // Guarda los cambios reseteados en el diccionario
    }
    // -------------------------------------------------------------------------

    void UpdatePlantSprite(PlantData plant)
    {
        Tile plantVisualTile = ScriptableObject.CreateInstance<Tile>();

        // NEW: Set color based on watered state
        if (plant.isWatered && !plant.isReadyToHarvest && !plant.isWithered) // Only tint if watered, not ready to harvest, and not withered
        {
            plantVisualTile.color = wateredColor;
        }
        else
        {
            plantVisualTile.color = defaultPlantColor;
        }

        if (plant.isPlanted)
        {
            // --- NUEVO: Mostrar sprite seco si la planta está seca ---
            if (plant.isWithered && plant.witheredSprite != null)
            {
                plantVisualTile.sprite = plant.witheredSprite;
                plantTilemap.SetTile(plant.gridPosition, plantVisualTile);
            }
            // -------------------------------------------------------
            else if (plant.growthStages != null && plant.growthStages.Length > 0 && plant.currentStage < plant.growthStages.Length && plant.growthStages[plant.currentStage] != null)
            {
                // Si la planta está creciendo o lista para cosechar (pero no seca), usa los sprites de crecimiento
                plantVisualTile.sprite = plant.growthStages[plant.currentStage];
                plantTilemap.SetTile(plant.gridPosition, plantVisualTile);
            }
            else
            {
                // Si la planta no tiene sprites válidos o está en un estado indefinido, borra el tile
                Debug.LogWarning($"PlantManager: Planta {plant.plantName} en {plant.gridPosition} tiene un estado de sprite inconsistente (o sprite nulo para su etapa). Borrando tile visual.");
                plantTilemap.SetTile(plant.gridPosition, null);
            }
        }
        else
        {
            // Si no está plantada (ej. después de cosechar/arar seca), borra el tile visual
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

    public void AddSeedsToInventory(string seedName, int amount)
    {
        if (inventory.ContainsKey(seedName))
        {
            inventory[seedName] += amount;
        }
        else
        {
            inventory.Add(seedName, amount);
        }
        UpdateInventoryUI();
        DisplayMessage($"Has recibido {amount} {seedName} como recompensa!");
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
            // Añadir la semilla seleccionada a la UI
            inventoryString += $"\nSemilla Seleccionada: {currentSelectedSeed}";
            inventoryString = inventoryString.TrimEnd('\n'); // Eliminar el último salto de línea si no es necesario
            inventoryText.text = inventoryString;
        }
    }

    // --- NUEVO MÉTODO GENÉRICO PARA REPRODUCIR SONIDOS ---
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip); // PlayOneShot reproduce el clip sin detener el actual
        }
        else if (clip == null)
        {
            Debug.LogWarning("Intento de reproducir un sonido nulo. Asegúrate de asignar todos los AudioClips en el Inspector.");
        }
    }
    // ----------------------------------------------------

    // --- NUEVO MÉTODO PARA REPRODUCIR SONIDO DE MISIÓN COMPLETADA ---
    // Este método debería ser público para que MissionManager pueda llamarlo.
    public void PlayMissionCompleteSound()
    {
        PlaySound(missionCompleteSound);
        Debug.Log("Sonido de misión completada reproducido.");
    }
    // --------------------------------------------------------------
}