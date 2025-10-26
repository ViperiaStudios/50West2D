using UnityEngine;
using TMPro; // Add this for TextMeshPro

public class BusController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    public Joystick joystick;
    
    [Header("Speed Upgrade System")]
    public float baseSpeed = 4f;
    public float maxSpeed = 5f;
    public float speedIncreasePerTank = 0.2f; // (5 - 4) / 5 = 0.2 per tank
    public float turboBoostSpeed = 6f; // Speed during turbo boost

    [Header("Health System")]
    public bool isInvulnerable = false;
    public float invulnerabilityTime = 1f;

    [Header("Health Manager")]
    public HealthManager healthManager; // Reference to the health manager script

    private Rigidbody2D rb;
    private float invulnerabilityTimer = 0f;
    private int gasTanksCollected = 0;
    private Vector3 originalScale;
    private bool isTurboBoostActive = false;
    private float speedBeforeTurbo; // Store speed before turbo boost

    // Movement boundaries
    private float minX = -7.46f;
    private float maxX = 7.46f;
    private float minY = -0.73f;
    private float maxY = 1.37f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        moveSpeed = baseSpeed; // Start at base speed
        originalScale = transform.localScale; // Store original scale
        Debug.Log("Bus Controller started - health managed by HealthManager");
        Debug.Log($"[BusController] Starting speed: {moveSpeed}, Max speed: {maxSpeed}");
    }
    
    // Method to increase speed when collecting gas tanks
    public void IncreaseSpeed()
    {
        if (gasTanksCollected < 5 && moveSpeed < maxSpeed)
        {
            gasTanksCollected++;
            moveSpeed = Mathf.Min(maxSpeed, baseSpeed + (speedIncreasePerTank * gasTanksCollected));
            
            Debug.Log($"[BusController] Speed increased! Tanks: {gasTanksCollected}/5, Speed: {moveSpeed}");
        }
        else
        {
            Debug.Log("[BusController] Already at max speed!");
        }
    }

    void Update()
    {
        // Handle invulnerability timer
        if (isInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0f)
            {
                isInvulnerable = false;
            }
        }
    }

    void FixedUpdate()
    {
        // Use both X and Y input from the joystick
        float horizontalInput = joystick.Horizontal;
        float verticalInput = joystick.Vertical;

        // Apply joystick input to velocity
        Vector2 newVelocity = new Vector2(horizontalInput * moveSpeed, verticalInput * moveSpeed);
        rb.velocity = newVelocity;

        // Clamp position to stay within bounds
        Vector2 clampedPosition = rb.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, minY, maxY);
        rb.position = clampedPosition;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if it's an obstacle (has ObstacleController script)
        ObstacleController obstacle = other.GetComponent<ObstacleController>();
        if (obstacle != null)
        {
            TakeDamage(1);
            obstacle.DestroyObstacle();
            return;
        }

        // If the object has a PooledObjectMover script, treat it as a pickup
        PooledObjectMover pooledObj = other.GetComponent<PooledObjectMover>();
        if (pooledObj != null)
        {
            ScoreManager.Instance.AddPoints(4);
            pooledObj.ReturnToPool();
        }
    }

    public void TakeDamage(int damage)
    {
        // Can't take damage during turbo boost or invulnerability
        if (isInvulnerable || isTurboBoostActive) return;

        // Tell the health manager to handle damage
        if (healthManager != null)
        {
            healthManager.TakeDamage(damage);
        }
        
        Debug.Log($"Bus took {damage} damage!");
        
        // Set invulnerability
        isInvulnerable = true;
        invulnerabilityTimer = invulnerabilityTime;
    }
    
    // Activate turbo boost - scale up and become invulnerable
    public void ActivateTurboBoost(float scaleMultiplier)
    {
        isTurboBoostActive = true;
        isInvulnerable = true;
        
        // Store current speed and boost to turbo speed
        speedBeforeTurbo = moveSpeed;
        moveSpeed = turboBoostSpeed;
        
        // Scale up the player
        StartCoroutine(ScaleToSize(originalScale * scaleMultiplier, 0.5f));
        
        Debug.Log($"[BusController] Turbo boost activated! Scaling to {scaleMultiplier}x, Speed: {moveSpeed}");
    }
    
    // Deactivate turbo boost - scale back down
    public void DeactivateTurboBoost()
    {
        isTurboBoostActive = false;
        isInvulnerable = false;
        
        // Restore speed from before turbo
        moveSpeed = speedBeforeTurbo;
        
        // Scale back to original
        StartCoroutine(ScaleToSize(originalScale, 0.5f));
        
        Debug.Log($"[BusController] Turbo boost deactivated! Scaling back to normal, Speed: {moveSpeed}");
    }
    
    // Coroutine to smoothly scale the player
    System.Collections.IEnumerator ScaleToSize(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        transform.localScale = targetScale;
    }

    // Health is now managed by HealthManager script
}
