using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TurboBoostManager : MonoBehaviour
{
    public static TurboBoostManager Instance;
    
    [Header("References")]
    public Button turboButton;
    public FuelBarManager fuelBarManager;
    public BusController busController;
    public ObjectPool objectPool;
    
    [Header("Boost Settings")]
    public float boostDuration = 8f;
    public float playerScaleMultiplier = 2.2f;
    
    private bool isBoostActive = false;
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    void Start()
    {
        // Setup button listener
        if (turboButton != null)
        {
            turboButton.onClick.AddListener(OnTurboButtonPressed);
            turboButton.interactable = false; // Start disabled
        }
        
        Debug.Log("[TurboBoostManager] Initialized. Waiting for fuel bar to fill...");
    }
    
    void Update()
    {
        // Update button state based on fuel bar
        if (turboButton != null && fuelBarManager != null)
        {
            bool canActivate = fuelBarManager.IsFuelBarFull() && !isBoostActive;
            turboButton.interactable = canActivate;
        }
    }
    
    public void OnTurboButtonPressed()
    {
        if (fuelBarManager != null && fuelBarManager.IsFuelBarFull() && !isBoostActive)
        {
            Debug.Log("[TurboBoostManager] TURBO BOOST ACTIVATED!");
            StartCoroutine(ActivateTurboBoost());
        }
    }
    
    IEnumerator ActivateTurboBoost()
    {
        isBoostActive = true;
        
        // Disable button during boost
        if (turboButton != null)
        {
            turboButton.interactable = false;
        }
        
        // Reset fuel bar to 0
        if (fuelBarManager != null)
        {
            fuelBarManager.ResetFuelBar();
            fuelBarManager.SetBoostActive(true); // Prevent fuel collection
        }
        
        // Scale up player
        if (busController != null)
        {
            busController.ActivateTurboBoost(playerScaleMultiplier);
        }
        
        // Speed up object pool spawning
        if (objectPool != null)
        {
            objectPool.SetTurboBoostMode(true);
        }
        
        Debug.Log($"[TurboBoostManager] TURBO ACTIVE for {boostDuration} seconds! All food items will move 2x faster!");
        
        // Wait for boost duration
        yield return new WaitForSeconds(boostDuration);
        
        // Deactivate boost
        Debug.Log("[TurboBoostManager] TURBO BOOST ENDED - Reverting to normal");
        
        // Revert player scale and invulnerability
        if (busController != null)
        {
            busController.DeactivateTurboBoost();
        }
        
        // Revert spawn rate
        if (objectPool != null)
        {
            objectPool.SetTurboBoostMode(false);
        }
        
        // Allow fuel collection again
        if (fuelBarManager != null)
        {
            fuelBarManager.SetBoostActive(false);
        }
        
        isBoostActive = false;
        Debug.Log("[TurboBoostManager] All effects reverted to normal!");
    }
    
    public bool IsBoostActive()
    {
        return isBoostActive;
    }
}

