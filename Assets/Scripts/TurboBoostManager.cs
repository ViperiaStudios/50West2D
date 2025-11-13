using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TurboBoostManager : MonoBehaviour
{
    public static TurboBoostManager Instance;
    
    [Header("References")]
    public Button turboButton;
    public Graphic turboText;
    public FuelBarManager fuelBarManager;
    public BusController busController;
    public ObjectPool objectPool;
    
    [Header("Boost Settings")]
    public float boostDuration = 8f;
    public float playerScaleMultiplier = 2.2f;
    
    [Header("Turbo UI Settings")]
    [Range(0f, 1f)]
    public float yellowThreshold = 0.6f;
    [Range(0f, 1f)]
    public float redThreshold = 0.99f;
    public Color greenTintColor = new Color(0.3f, 1f, 0.3f, 1f);
    public Color yellowTintColor = new Color(1f, 0.95f, 0.2f, 1f);
    public Color redTintColor = new Color(1f, 0.3f, 0.3f, 1f);
    public Color disabledTintColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [Range(0f, 1f)]
    public float greenTintStrength = 0.35f;
    [Range(0f, 1f)]
    public float yellowTintStrength = 0.35f;
    [Range(0f, 1f)]
    public float redTintStrength = 0.5f;
    [Range(0f, 1f)]
    public float disabledTintStrength = 0.45f;
    public float turboTextFlashInterval = 0.4f;
    
    private bool isBoostActive = false;
    private Coroutine turboFlashRoutine;
    private bool buttonColorsCaptured = false;
    private Color originalButtonImageColor = Color.white;
    private ColorBlock originalButtonColors;
    
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
            CaptureOriginalButtonColors();
            ApplyButtonTint(originalButtonImageColor, 0f);
        }
        
        if (turboText != null)
        {
            turboText.gameObject.SetActive(false);
        }
        
        Debug.Log("[TurboBoostManager] Initialized. Waiting for fuel bar to fill...");
    }
    
    void Update()
    {
        if (fuelBarManager == null)
        {
            return;
        }

        float fuelPercent = fuelBarManager.GetFuelPercentage();
        bool isFuelFull = fuelBarManager.IsFuelBarFull();
        
        UpdateTurboButtonState(isFuelFull);
        UpdateTurboVisuals(fuelPercent, isFuelFull);
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
        StopTurboFlash();
        
        // Disable button during boost
        if (turboButton != null)
        {
            turboButton.interactable = false;
            ApplyButtonTint(disabledTintColor, disabledTintStrength);
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

        if (fuelBarManager != null)
        {
            bool isFuelFull = fuelBarManager.IsFuelBarFull();
            UpdateTurboButtonState(isFuelFull);
            UpdateTurboVisuals(fuelBarManager.GetFuelPercentage(), isFuelFull);
        }
        else
        {
            UpdateTurboButtonState(false);
            UpdateTurboVisuals(0f, false);
        }
        
        Debug.Log("[TurboBoostManager] All effects reverted to normal!");
    }
    
    public bool IsBoostActive()
    {
        return isBoostActive;
    }

    void UpdateTurboButtonState(bool canActivate)
    {
        if (turboButton == null)
        {
            return;
        }
        
        bool interactable = canActivate && !isBoostActive;
        turboButton.interactable = interactable;
    }

    void UpdateTurboVisuals(float fuelPercent, bool isFuelFull)
    {
        if (turboButton == null)
        {
            StopTurboFlash();
            return;
        }

        float clampedPercent = Mathf.Clamp01(fuelPercent);
        
        if (isBoostActive || clampedPercent <= 0f)
        {
            ApplyButtonTint(originalButtonImageColor, 0f);
            StopTurboFlash();
            return;
        }
        
        if (isFuelFull && !isBoostActive)
        {
            ApplyButtonTint(redTintColor, redTintStrength);
            StartTurboFlash();
            return;
        }
        
        if (clampedPercent >= Mathf.Clamp01(yellowThreshold))
        {
            ApplyButtonTint(yellowTintColor, yellowTintStrength);
            StopTurboFlash();
            return;
        }
        
        ApplyButtonTint(greenTintColor, greenTintStrength);
        StopTurboFlash();
    }

    void CaptureOriginalButtonColors()
    {
        if (buttonColorsCaptured || turboButton == null)
        {
            return;
        }

        if (turboButton.image != null)
        {
            originalButtonImageColor = turboButton.image.color;
        }

        originalButtonColors = turboButton.colors;
        buttonColorsCaptured = true;
    }

    void ApplyButtonTint(Color tintColor, float tintStrength)
    {
        if (turboButton == null)
        {
            return;
        }

        CaptureOriginalButtonColors();

        float strength = Mathf.Clamp01(tintStrength);
        Color targetImageColor = strength <= 0f
            ? originalButtonImageColor
            : Color.Lerp(originalButtonImageColor, tintColor, strength);

        Image buttonImage = turboButton.image;
        if (buttonImage != null)
        {
            buttonImage.color = targetImageColor;
        }

        ColorBlock tintedColors = originalButtonColors;
        tintedColors.normalColor = strength <= 0f
            ? originalButtonColors.normalColor
            : Color.Lerp(originalButtonColors.normalColor, tintColor, strength);
        tintedColors.highlightedColor = strength <= 0f
            ? originalButtonColors.highlightedColor
            : Color.Lerp(originalButtonColors.highlightedColor, tintColor, strength * 0.9f);
        tintedColors.pressedColor = strength <= 0f
            ? originalButtonColors.pressedColor
            : Color.Lerp(originalButtonColors.pressedColor, tintColor, strength * 0.7f);
        tintedColors.selectedColor = strength <= 0f
            ? originalButtonColors.selectedColor
            : Color.Lerp(originalButtonColors.selectedColor, tintColor, strength);
        tintedColors.disabledColor = strength <= 0f
            ? originalButtonColors.disabledColor
            : Color.Lerp(originalButtonColors.disabledColor, tintColor, strength);

        turboButton.colors = tintedColors;
    }

    void StartTurboFlash()
    {
        if (turboText == null)
        {
            return;
        }
        
        if (turboFlashRoutine == null)
        {
            turboText.gameObject.SetActive(true);
            turboFlashRoutine = StartCoroutine(FlashTurboText());
        }
    }

    void StopTurboFlash()
    {
        if (turboFlashRoutine != null)
        {
            StopCoroutine(turboFlashRoutine);
            turboFlashRoutine = null;
        }

        if (turboText != null)
        {
            turboText.gameObject.SetActive(false);
        }
    }

    IEnumerator FlashTurboText()
    {
        bool visible = true;
        
        while (true)
        {
            if (turboText != null)
            {
                turboText.gameObject.SetActive(visible);
                visible = !visible;
            }
            
            yield return new WaitForSeconds(turboTextFlashInterval);
        }
    }
}

