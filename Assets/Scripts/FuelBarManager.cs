using UnityEngine;
using UnityEngine.UI;

public class FuelBarManager : MonoBehaviour
{
    [Header("Fuel Bar UI")]
    public Slider fuelBarSlider; // Reference to UI Slider
    public Image fuelBarFillImage; // Reference to the fill image (optional, for color changes)
    
    [Header("Fuel Settings")]
    public int maxFuelTanks = 5; // Total gas tanks needed to fill bar
    public Color emptyColor = Color.red;
    public Color halfColor = Color.yellow;
    public Color fullColor = Color.green;
    
    private int currentFuelTanks = 0;
    private bool boostActive = false; // Whether turbo boost is currently active
    
    void Start()
    {
        UpdateFuelBar();
        Debug.Log($"[FuelBarManager] Initialized. Need {maxFuelTanks} gas tanks to fill the bar.");
    }
    
    public void AddFuel()
    {
        // Don't add fuel during turbo boost
        if (boostActive)
        {
            Debug.Log("[FuelBarManager] Cannot collect fuel during turbo boost!");
            return;
        }
        
        if (currentFuelTanks < maxFuelTanks)
        {
            currentFuelTanks++;
            UpdateFuelBar();
            
            Debug.Log($"[FuelBarManager] Fuel added! {currentFuelTanks}/{maxFuelTanks} tanks collected");
            
            // Check if bar is full
            if (currentFuelTanks >= maxFuelTanks)
            {
                OnFuelBarFull();
            }
        }
        else
        {
            Debug.Log("[FuelBarManager] Fuel bar already full! Collecting more gas tanks has no effect.");
        }
    }
    
    public void RemoveFuel()
    {
        if (currentFuelTanks > 0)
        {
            currentFuelTanks--;
            UpdateFuelBar();
            
            Debug.Log($"[FuelBarManager] Fuel lost! {currentFuelTanks}/{maxFuelTanks} tanks remaining");
        }
        else
        {
            Debug.Log("[FuelBarManager] No fuel to lose (already at 0)");
        }
    }
    
    void UpdateFuelBar()
    {
        if (fuelBarSlider != null)
        {
            // Update slider value (0.0 to 1.0)
            fuelBarSlider.value = (float)currentFuelTanks / maxFuelTanks;
        }
        
        // Update color based on fill percentage
        if (fuelBarFillImage != null)
        {
            float fillPercentage = (float)currentFuelTanks / maxFuelTanks;
            
            if (fillPercentage < 0.5f)
            {
                // Blend from empty to half color
                fuelBarFillImage.color = Color.Lerp(emptyColor, halfColor, fillPercentage * 2f);
            }
            else
            {
                // Blend from half to full color
                fuelBarFillImage.color = Color.Lerp(halfColor, fullColor, (fillPercentage - 0.5f) * 2f);
            }
        }
    }
    
    void OnFuelBarFull()
    {
        Debug.Log("[FuelBarManager] FUEL BAR FULL! Special event triggered!");
        
        // TODO: Add special effects/events when fuel bar is full
        // You can add boost mode, invulnerability, special abilities, etc.
        
        // Reset the fuel bar for next cycle (optional)
        // currentFuelTanks = 0;
        // UpdateFuelBar();
    }
    
    // Public method to check if fuel bar is full
    public bool IsFuelBarFull()
    {
        return currentFuelTanks >= maxFuelTanks;
    }
    
    // Public method to get current fuel percentage
    public float GetFuelPercentage()
    {
        return (float)currentFuelTanks / maxFuelTanks;
    }
    
    // Method to reset fuel bar (useful for restarting)
    public void ResetFuelBar()
    {
        currentFuelTanks = 0;
        UpdateFuelBar();
        Debug.Log("[FuelBarManager] Fuel bar reset!");
    }
    
    // Method to enable/disable boost mode (prevents fuel collection during boost)
    public void SetBoostActive(bool active)
    {
        boostActive = active;
        Debug.Log($"[FuelBarManager] Boost active set to: {active}");
    }
    
    // Check if fuel can be collected
    public bool CanCollectFuel()
    {
        return !boostActive;
    }
}

