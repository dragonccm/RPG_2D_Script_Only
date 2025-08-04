using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ?? UNIVERSAL TELEGRAPH MANAGER - 2D Top-Down RPG Telegraph System
/// Implements Universal Boss Design Framework telegraph principles
/// Manages visual warnings with RPG-adapted timing and multi-modal feedback
/// </summary>
public class TelegraphManager : MonoBehaviour
{
    #region UNIVERSAL FRAMEWORK SETTINGS
    [Header("?? UNIVERSAL FRAMEWORK TIMING")]
    [SerializeField] private float baseWarningDuration = 1.5f;
    [SerializeField] private float finalWarningDuration = 0.5f;
    [SerializeField] private float threatAssessmentDuration = 0.5f;
    [SerializeField] private bool adaptToPlayerSkill = true;
    [SerializeField] private float playerSkillRating = 0.5f; // 0-1, updated by boss system
    
    [Header("?? VISUAL FRAMEWORK")]
    [SerializeField] private Color warningColor = new Color(1f, 1f, 0f, 0.6f);
    [SerializeField] private Color finalWarningColor = new Color(1f, 0f, 0f, 0.8f);
    [SerializeField] private Color threatColor = new Color(1f, 0.5f, 0f, 0.3f);
    
    [Header("?? MULTI-MODAL FEEDBACK")]
    [SerializeField] private AudioClip warningSound;
    [SerializeField] private AudioClip finalWarningSound;
    [SerializeField] private AudioClip threatSound;
    [SerializeField] private float audioVolume = 0.7f;
    
    [Header("? ANIMATION FRAMEWORK")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float intensityRamp = 1.5f;
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    #endregion

    #region PRIVATE VARIABLES
    private AudioSource audioSource;
    private List<GameObject> activeWarnings = new List<GameObject>();
    
    // Performance optimization
    private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private Queue<GameObject> warningPool = new Queue<GameObject>();
    #endregion

    #region INITIALIZATION
    public void Initialize(float baseWarning, float finalWarning, Color warning, Color final)
    {
        baseWarningDuration = baseWarning;
        finalWarningDuration = finalWarning;
        warningColor = warning;
        finalWarningColor = final;
        
        SetupAudioSystem();
        CreateSpriteCache();
        InitializeObjectPool();
        
        Debug.Log($"[TelegraphManager] Initialized with Universal Framework settings");
    }

    private void SetupAudioSystem()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        audioSource.volume = audioVolume;
        audioSource.spatialBlend = 0f; // 2D sound
    }

    private void CreateSpriteCache()
    {
        if (!spriteCache.ContainsKey("Circle"))
            spriteCache["Circle"] = CreateCircleSprite();
        if (!spriteCache.ContainsKey("Square"))
            spriteCache["Square"] = CreateSquareSprite();
        if (!spriteCache.ContainsKey("Line"))
            spriteCache["Line"] = CreateLineSprite();
    }

    private void InitializeObjectPool()
    {
        // Pre-create warning objects for performance
        for (int i = 0; i < 10; i++)
        {
            GameObject warning = CreatePooledWarning();
            warning.SetActive(false);
            warningPool.Enqueue(warning);
        }
    }
    #endregion

    #region TELEGRAPH CREATION METHODS
    
    /// <summary>
    /// Creates a circle warning with Universal Framework compliance
    /// </summary>
    public GameObject CreateCircleWarning(Vector3 position, float radius, Color color, float duration)
    {
        GameObject warning = GetPooledWarning();
        SetupWarningVisuals(warning, position, radius * 2f, "Circle");
        
        // Override color if specified
        var renderer = warning.GetComponent<SpriteRenderer>();
        if (renderer != null && color != Color.clear)
        {
            renderer.color = color;
        }
        
        StartCoroutine(AnimateWarning(warning, duration));
        activeWarnings.Add(warning);
        
        return warning;
    }

    /// <summary>
    /// Creates a line warning with Universal Framework compliance
    /// </summary>
    public GameObject CreateLineWarning(Vector3 startPos, Vector3 endPos, float width, Color color, float duration)
    {
        Vector3 center = (startPos + endPos) / 2f;
        float length = Vector3.Distance(startPos, endPos);
        
        GameObject warning = GetPooledWarning();
        SetupWarningVisuals(warning, center, length, "Line");
        
        // Configure for line
        var renderer = warning.GetComponent<SpriteRenderer>();
        renderer.sprite = spriteCache["Line"];
        
        Vector3 direction = (endPos - startPos).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        warning.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        warning.transform.localScale = new Vector3(length, width, 1f);
        
        if (color != Color.clear)
        {
            renderer.color = color;
        }
        
        StartCoroutine(AnimateWarning(warning, duration));
        activeWarnings.Add(warning);
        
        return warning;
    }

    /// <summary>
    /// Creates a square warning with Universal Framework compliance
    /// </summary>
    public GameObject CreateSquareWarning(Vector3 position, float size, Color color, float duration)
    {
        GameObject warning = GetPooledWarning();
        SetupWarningVisuals(warning, position, size, "Square");
        
        var renderer = warning.GetComponent<SpriteRenderer>();
        renderer.sprite = spriteCache["Square"];
        
        if (color != Color.clear)
        {
            renderer.color = color;
        }
        
        StartCoroutine(AnimateWarning(warning, duration));
        activeWarnings.Add(warning);
        
        return warning;
    }

    /// <summary>
    /// Creates a pattern warning (multiple telegraphs) with Universal Framework
    /// </summary>
    public List<GameObject> CreatePatternWarning(List<Vector3> positions, List<float> sizes, Color color, float duration)
    {
        List<GameObject> warnings = new List<GameObject>();
        
        for (int i = 0; i < positions.Count; i++)
        {
            float size = i < sizes.Count ? sizes[i] : 2f;
            
            // Stagger the telegraphs slightly for better readability
            StartCoroutine(CreateDelayedPatternWarning(positions[i], size, color, i * 0.1f, warnings));
        }
        
        return warnings;
    }

    private IEnumerator CreateDelayedPatternWarning(Vector3 position, float size, Color color, float delay, List<GameObject> warnings)
    {
        yield return new WaitForSeconds(delay);
        
        GameObject warning = CreateCircleWarning(position, size, color, baseWarningDuration + finalWarningDuration);
        warnings.Add(warning);
    }
    #endregion

    #region ANIMATION SYSTEM
    private IEnumerator AnimateWarning(GameObject warning, float duration)
    {
        var renderer = warning.GetComponent<SpriteRenderer>();
        if (renderer == null) yield break;
        
        Color startColor = renderer.color;
        float elapsed = 0f;
        
        while (elapsed < duration && warning != null)
        {
            // Pulsing animation
            float pulse = Mathf.Sin(elapsed * pulseSpeed) * 0.5f + 0.5f;
            Color currentColor = Color.Lerp(startColor, Color.white, pulse * 0.2f);
            renderer.color = currentColor;
            
            // Intensity ramp up as we approach execution
            float intensity = Mathf.Lerp(1f, intensityRamp, elapsed / duration);
            warning.transform.localScale = warning.transform.localScale.normalized * intensity;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Complete telegraph sequence with callback - Universal Framework compliant
    /// </summary>
    public IEnumerator CompleteWarningSequence(GameObject warning, System.Action onExecute = null)
    {
        if (warning == null) yield break;
        
        // Let the animation sequence handle timing
        while (warning != null && warning.activeInHierarchy && activeWarnings.Contains(warning))
        {
            yield return null;
        }
        
        // Execute callback when telegraph completes
        onExecute?.Invoke();
    }
    #endregion

    #region OBJECT POOLING
    private GameObject GetPooledWarning()
    {
        if (warningPool.Count > 0)
        {
            GameObject pooled = warningPool.Dequeue();
            pooled.SetActive(true);
            return pooled;
        }
        
        return CreatePooledWarning();
    }

    private GameObject CreatePooledWarning()
    {
        GameObject warning = new GameObject("TelegraphWarning");
        warning.transform.SetParent(transform);
        
        var renderer = warning.AddComponent<SpriteRenderer>();
        renderer.sprite = spriteCache["Circle"];
        renderer.sortingOrder = 10;
        renderer.sortingLayerName = "UI";
        
        return warning;
    }

    private void ReturnWarningToPool(GameObject warning)
    {
        if (warning == null) return;
        
        activeWarnings.Remove(warning);
        
        warning.SetActive(false);
        warning.transform.localScale = Vector3.one;
        warning.transform.rotation = Quaternion.identity;
        
        warningPool.Enqueue(warning);
    }

    private void SetupWarningVisuals(GameObject warning, Vector3 position, float size, string spriteType)
    {
        warning.transform.position = position;
        warning.transform.localScale = Vector3.one * size;
        
        var renderer = warning.GetComponent<SpriteRenderer>();
        renderer.color = warningColor;
        renderer.sprite = spriteCache[spriteType];
    }
    #endregion

    #region SPRITE CREATION
    private Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                
                // Create ring effect with gradient
                if (distance <= radius)
                {
                    float alpha = distance <= radius - 4f ? 0.4f : 
                                 1f - Mathf.Abs(distance - (radius - 2f)) / 2f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateSquareSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                bool isBorder = (x < 4 || x >= size - 4 || y < 4 || y >= size - 4);
                bool isInner = (x >= 4 && x < size - 4 && y >= 4 && y < size - 4);
                
                if (isBorder)
                {
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, 1f));
                }
                else if (isInner)
                {
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, 0.4f));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateLineSprite()
    {
        int width = 64;
        int height = 8;
        Texture2D texture = new Texture2D(width, height);
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float distanceFromCenter = Mathf.Abs(y - height / 2f) / (height / 2f);
                float alpha = 1f - distanceFromCenter;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
    #endregion

    #region UTILITY METHODS
    
    /// <summary>
    /// Clear all active warnings immediately
    /// </summary>
    public void ClearAllWarnings()
    {
        var warningsCopy = new List<GameObject>(activeWarnings);
        foreach (var warning in warningsCopy)
        {
            if (warning != null)
            {
                ReturnWarningToPool(warning);
            }
        }
        activeWarnings.Clear();
    }

    /// <summary>
    /// Get number of active warnings
    /// </summary>
    public int GetActiveWarningCount()
    {
        activeWarnings.RemoveAll(w => w == null || !w.activeInHierarchy);
        return activeWarnings.Count;
    }

    /// <summary>
    /// Check if position is inside any active warning area
    /// </summary>
    public bool IsPositionInWarningZone(Vector3 position)
    {
        foreach (var warning in activeWarnings)
        {
            if (warning == null || !warning.activeInHierarchy) continue;
            
            float distance = Vector3.Distance(position, warning.transform.position);
            float warningRadius = warning.transform.localScale.x / 2f;
            
            if (distance <= warningRadius)
                return true;
        }
        
        return false;
    }

    /// <summary>
    /// Get all safe positions within a radius (not in warning zones)
    /// </summary>
    public List<Vector3> GetSafePositions(Vector3 center, float radius, int sampleCount = 20)
    {
        List<Vector3> safePositions = new List<Vector3>();
        
        for (int i = 0; i < sampleCount; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * radius;
            Vector3 testPosition = center + new Vector3(randomOffset.x, randomOffset.y, 0f);
            
            if (!IsPositionInWarningZone(testPosition))
            {
                safePositions.Add(testPosition);
            }
        }
        
        return safePositions;
    }

    public void SetPlayerSkillRating(float rating)
    {
        playerSkillRating = Mathf.Clamp01(rating);
        Debug.Log($"[TelegraphManager] Player skill rating updated to {playerSkillRating:F2}");
    }
    #endregion

    #region CLEANUP
    private void OnDestroy()
    {
        ClearAllWarnings();
        spriteCache.Clear();
    }

    private void OnDisable()
    {
        ClearAllWarnings();
    }
    #endregion

    #region DEBUG & TESTING
    [ContextMenu("?? Test Universal Framework")]
    public void TestUniversalFramework()
    {
        CreateCircleWarning(transform.position, 3f, Color.red, 2f);
        Debug.Log("Universal Framework telegraph test initiated");
    }

    [ContextMenu("?? Show Framework Stats")]
    public void ShowFrameworkStats()
    {
        Debug.Log($@"
?? UNIVERSAL TELEGRAPH FRAMEWORK STATS
====================================
Active Telegraphs: {GetActiveWarningCount()}
Player Skill Rating: {playerSkillRating:F2}
Base Warning Duration: {baseWarningDuration:F1}s
Performance: {(GetActiveWarningCount() < 8 ? "Good" : "High Load")}
");
    }
    #endregion
}