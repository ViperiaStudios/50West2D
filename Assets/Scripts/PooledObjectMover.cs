using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PooledObjectMover : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float offScreenX = -15f;
    
    [Header("Scoring")]
    public int pointValue = 4; // How many points this food item is worth

    private ObjectPool pool;
    private EffectPool effectPool; // Assuming you have an EffectPool for visual effects
    private float timeAlive; // Tracks how long the object has been active
    private float originalMoveSpeed; // Store original speed for turbo boost
    private bool turboBoostActive = false;

    // Initialize is called by the ObjectPool when an object is taken from the pool
    public void Initialize(ObjectPool objectPool)
    {
        pool = objectPool; // Store reference to the ObjectPool

        // Find the effect pool in the scene only once when this component is first initialized.
        // This avoids repeatedly calling FindObjectOfType, which can be slow.
        if (effectPool == null)
            effectPool = FindObjectOfType<EffectPool>();
    }

    // OnEnable is called every time the GameObject is activated (either initially or from the pool)
    void OnEnable()
    {
        timeAlive = 0f; // Reset the timeAlive counter
        
        // Store original speed when first enabled
        if (originalMoveSpeed == 0)
        {
            originalMoveSpeed = moveSpeed;
        }
        
        // Debug.Log statement to track when objects are enabled/activated
        Debug.Log($"[PooledObjectMover] Object {gameObject.name} ENABLED at position {transform.position}");
    }

    void Update()
    {
        timeAlive += Time.deltaTime; // Increment time alive

        // Check if turbo boost is active and apply speed
        float currentSpeed = moveSpeed;
        if (TurboBoostManager.Instance != null && TurboBoostManager.Instance.IsBoostActive())
        {
            currentSpeed = originalMoveSpeed * 2f; // Double speed during turbo
        }
        else
        {
            currentSpeed = originalMoveSpeed; // Normal speed
        }

        // Move the object to the left based on current speed
        transform.Translate(Vector2.left * currentSpeed * Time.deltaTime);

        // Check if the object has moved far enough off-screen to the left
        if (transform.position.x <= offScreenX)
        {
            // Debug.Log statement to track when objects are returned due to going off-screen
            Debug.Log($"[PooledObjectMover] Object {gameObject.name} returned to pool: Off-screen at X={transform.position.x}");
            ReturnToPool(); // Return the object to the pool
        }
    }

    // OnTriggerEnter2D is called when this object's trigger collider enters another collider
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the other collider has the "Player" tag
        if (other.CompareTag("Player"))
        {
            // Add points based on this object's point value
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddPoints(pointValue);
            }
            // Debug.Log statement to track when objects are returned due to collision
            Debug.Log($"[PooledObjectMover] Object {gameObject.name} collected for {pointValue} points!");
            ReturnToPool(); // Return the object to the pool after collision
        }
    }

    // Public method to return the object to its pool
    public void ReturnToPool()
    {
        // IMPORTANT: Only proceed if the object is currently active.
        // This prevents multiple calls to ReturnToPool if, for example,
        // it collides and goes off-screen in the same frame, or multiple rapid collisions.
        if (gameObject.activeSelf)
        {
            // Spawn an effect (e.g., explosion, pickup animation) at the object's position
            // just before it's deactivated and returned to the pool.
            if (effectPool != null)
            {
                effectPool.GetEffect(transform.position);
            }

            // Ensure the 'pool' reference is valid before attempting to return the object.
            if (pool != null)
            {
                pool.ReturnToPool(gameObject);
            }
            else
            {
                // If for some reason the pool reference is lost, destroy the object to prevent it from being orphaned
                Debug.LogWarning($"[PooledObjectMover] Object {gameObject.name} has no pool reference. Destroying object to prevent leak.");
                Destroy(gameObject);
            }
        }
    }

    // ResetState is called by the ObjectPool when an object is dequeued and prepared for reuse.
    // This method ensures the object is in a clean, ready-to-use state.
    public void ResetState()
    {
        // Reset any necessary logic like velocity, scale, alpha, etc.
        // The position is usually set by ObjectPool.GetFromPool, so no need to set it here.
        // transform.position = new Vector2(11f, Random.Range(-1f, 1.4f));

        // Ensure the object's visual components are enabled, in case they were disabled during its previous use.
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        // Ensure the object's collider is enabled, in case it was disabled during its previous use.
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        // If the object uses a Rigidbody2D, reset its velocity and angular velocity.
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Add any other specific resets for your object's state (e.g., health, animation state)
    }
    
}
