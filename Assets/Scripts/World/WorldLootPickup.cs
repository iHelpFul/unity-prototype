using UnityEngine;
using UnityEngine.Rendering;

public enum WorldLootType
{
    Mesos,
    Item
}

public class WorldLootPickup : MonoBehaviour
{
    [SerializeField] private float bobAmplitude = 0.18f;
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float rotateSpeed = 140f;
    [SerializeField] private float magnetRange = 2.5f;
    [SerializeField] private float collectRange = 0.45f;
    [SerializeField] private float magnetSpeed = 10f;
    [SerializeField] private float lifeTime = 18f;
    [SerializeField] private GameBootstrap bootstrap;

    private WorldLootType lootType;
    private string itemId;
    private int lootAmount;
    private float baseY;
    private float spawnTime;
    private bool isCollected;
    private bool isInitialized;
    private bool hasVisual;

    public bool RequiresManualPickup => lootType != WorldLootType.Mesos;

    public static WorldLootPickup SpawnMesos(Vector3 worldPosition, int mesoAmount)
    {
        return Spawn(worldPosition, WorldLootType.Mesos, string.Empty, mesoAmount);
    }

    public static WorldLootPickup SpawnItem(Vector3 worldPosition, string itemId, int amount)
    {
        if (!ItemDatabase.TryGetDefinition(itemId, out _))
        {
            Debug.LogWarning($"WorldLootPickup ignored unknown item drop id '{itemId}'.");
            return null;
        }

        return Spawn(worldPosition, WorldLootType.Item, itemId, amount);
    }

    public static WorldLootPickup SpawnRedPotion(Vector3 worldPosition, int amount)
    {
        return SpawnItem(worldPosition, ItemDatabase.RedPotionId, amount);
    }

    public static WorldLootPickup SpawnBluePotion(Vector3 worldPosition, int amount)
    {
        return SpawnItem(worldPosition, ItemDatabase.BluePotionId, amount);
    }

    private static WorldLootPickup Spawn(Vector3 worldPosition, WorldLootType type, string itemId, int amount)
    {
        GameObject pickupObject = new GameObject(type == WorldLootType.Mesos ? "Mesos" : itemId);
        pickupObject.transform.position = worldPosition;

        WorldLootPickup pickup = pickupObject.AddComponent<WorldLootPickup>();
        pickup.Initialize(type, itemId, amount);
        return pickup;
    }

    private void Awake()
    {
        spawnTime = Time.time;
        baseY = transform.position.y;
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(WorldLootType type, string dropItemId, int amount)
    {
        lootType = type;
        itemId = string.IsNullOrWhiteSpace(dropItemId) ? string.Empty : dropItemId.Trim();
        lootAmount = Mathf.Max(1, amount);
        isInitialized = true;
        gameObject.name = $"{GetPickupPrefix()}_{lootAmount}";

        if (!hasVisual)
        {
            CreateDefaultVisual();
            hasVisual = true;
        }
    }

    private void Update()
    {
        if (!isInitialized || isCollected)
            return;

        if (RequiresManualPickup)
        {
            AnimateIdle();
            return;
        }

        PlayerCharacter collector = ResolveNearestAutoCollector();
        if (collector == null || collector.IsDead)
        {
            AnimateIdle();
            return;
        }

        Vector3 targetPosition = collector.transform.position + Vector3.up * 0.9f;
        float distanceToPlayer = Vector3.Distance(transform.position, targetPosition);

        if (distanceToPlayer <= magnetRange)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                magnetSpeed * Time.deltaTime);
        }
        else
        {
            AnimateIdle();
        }

        if (distanceToPlayer <= collectRange)
            Collect(collector);
    }

    private void AnimateIdle()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        Vector3 position = transform.position;
        position.y = baseY + Mathf.Sin((Time.time - spawnTime) * bobSpeed) * bobAmplitude;
        transform.position = position;
    }

    public bool CanBePickedUp(PlayerCharacter collector, float pickupRange)
    {
        if (collector == null || isCollected || !RequiresManualPickup)
            return false;

        Vector3 targetPosition = collector.transform.position + Vector3.up * 0.9f;
        return Vector3.Distance(transform.position, targetPosition) <= pickupRange;
    }

    public bool TryCollect(PlayerCharacter collector)
    {
        if (collector == null || isCollected || !RequiresManualPickup)
            return false;

        return Collect(collector);
    }

    private bool Collect(PlayerCharacter collector = null)
    {
        PlayerCharacter targetCollector = collector ?? ResolveNearestAutoCollector();
        if (targetCollector == null || !targetCollector.IsLocalPlayer)
            return false;

        ResolveBootstrap();
        if (bootstrap == null)
            return false;

        isCollected = true;

        switch (lootType)
        {
            case WorldLootType.Mesos:
                bootstrap.AddMesos(targetCollector, lootAmount);
                PublishNotification(targetCollector, GameplayNotificationCategory.Mesos, $"Mesos +{lootAmount}");
                break;

            case WorldLootType.Item:
                if (!bootstrap.AddInventoryItem(targetCollector, itemId, lootAmount))
                {
                    isCollected = false;
                    return false;
                }

                ItemDefinition definition = ItemDatabase.GetDefinition(itemId);
                string itemName = definition != null ? definition.DisplayName : itemId;
                PublishNotification(targetCollector, GameplayNotificationCategory.Item, $"Picked up {itemName} x{lootAmount}");
                break;
        }

        Destroy(gameObject);
        return true;
    }

    private PlayerCharacter ResolveNearestAutoCollector()
    {
        PlayerCharacter collector = WorldRuntimeSceneUtility.FindLocalPlayer(FindObjectsInactive.Exclude);
        if (collector == null || collector.IsDead)
            return null;

        Vector3 targetPosition = collector.transform.position + Vector3.up * 0.9f;
        float sqrDistance = (targetPosition - transform.position).sqrMagnitude;
        float sqrMagnetRange = magnetRange * magnetRange;

        return sqrDistance <= sqrMagnetRange ? collector : null;
    }

    private void ResolveBootstrap()
    {
        bootstrap = GameBootstrap.FindReadyBootstrap(bootstrap);
    }

    private void CreateDefaultVisual()
    {
        if (lootType == WorldLootType.Mesos)
        {
            CreateMesoVisual();
            return;
        }

        ItemDefinition definition = ItemDatabase.GetDefinition(itemId);
        if (definition != null && definition.Category == ItemCategory.Consumable)
        {
            CreatePotionVisual(definition);
            return;
        }

        CreateEtcVisual(definition);
    }

    private void CreateMesoVisual()
    {
        GameObject outerOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        outerOrb.name = "OuterOrb";
        outerOrb.transform.SetParent(transform, false);
        outerOrb.transform.localScale = Vector3.one * 0.38f;
        PrepareVisualRenderer(outerOrb, new Color(1f, 0.82f, 0.22f));

        GameObject innerOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        innerOrb.name = "InnerOrb";
        innerOrb.transform.SetParent(outerOrb.transform, false);
        innerOrb.transform.localScale = Vector3.one * 0.55f;
        PrepareVisualRenderer(innerOrb, new Color(1f, 0.96f, 0.7f));
    }

    private void CreatePotionVisual(ItemDefinition definition)
    {
        GameObject bottle = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bottle.name = "Bottle";
        bottle.transform.SetParent(transform, false);
        bottle.transform.localScale = new Vector3(0.24f, 0.18f, 0.24f);
        PrepareVisualRenderer(bottle, definition.PrimaryColor);

        GameObject liquid = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        liquid.name = "Liquid";
        liquid.transform.SetParent(bottle.transform, false);
        liquid.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        liquid.transform.localScale = new Vector3(0.72f, 0.52f, 0.72f);
        PrepareVisualRenderer(liquid, definition.AccentColor);

        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cap.name = "Cap";
        cap.transform.SetParent(bottle.transform, false);
        cap.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        cap.transform.localScale = new Vector3(0.42f, 0.14f, 0.42f);
        PrepareVisualRenderer(cap, new Color(0.96f, 0.96f, 0.96f));
    }

    private void CreateEtcVisual(ItemDefinition definition)
    {
        Color primaryColor = definition != null ? definition.PrimaryColor : new Color(0.7f, 0.84f, 0.6f);
        Color accentColor = definition != null ? definition.AccentColor : new Color(0.93f, 1f, 0.87f);

        GameObject baseChunk = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        baseChunk.name = "EtcBase";
        baseChunk.transform.SetParent(transform, false);
        baseChunk.transform.localScale = new Vector3(0.34f, 0.26f, 0.34f);
        PrepareVisualRenderer(baseChunk, primaryColor);

        GameObject accentChunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        accentChunk.name = "EtcAccent";
        accentChunk.transform.SetParent(baseChunk.transform, false);
        accentChunk.transform.localPosition = new Vector3(0f, 0.32f, 0f);
        accentChunk.transform.localRotation = Quaternion.Euler(18f, 24f, 12f);
        accentChunk.transform.localScale = new Vector3(0.34f, 0.18f, 0.34f);
        PrepareVisualRenderer(accentChunk, accentColor);
    }

    private void PrepareVisualRenderer(GameObject visualObject, Color color)
    {
        Collider visualCollider = visualObject.GetComponent<Collider>();
        if (visualCollider != null)
        {
            visualCollider.enabled = false;
            Destroy(visualCollider);
        }

        Renderer renderer = visualObject.GetComponent<Renderer>();
        if (renderer == null)
            return;

        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.material.color = color;
    }

    private string GetPickupPrefix()
    {
        if (lootType == WorldLootType.Mesos)
            return "MesoDrop";

        ItemDefinition definition = ItemDatabase.GetDefinition(itemId);
        if (definition == null)
            return "ItemDrop";

        return definition.DisplayName.Replace(" ", string.Empty);
    }

    private void PublishNotification(PlayerCharacter target, GameplayNotificationCategory category, string message)
    {
        if (target == null || !target.IsLocalPlayer || string.IsNullOrWhiteSpace(message))
            return;

        EventBus.Publish(new GameplayNotificationEvent
        {
            Target = target,
            CharacterId = target.CharacterId,
            Category = category,
            Message = message
        });
    }
}
