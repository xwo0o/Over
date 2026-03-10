using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class HealthBarUI : MonoBehaviour
{
    public Slider healthSlider;
    public Text healthText;
    public CharacterStats targetStats;
    public EnemyHealthManager targetEnemyHealth;
    public Transform cameraTransform;
    
    private bool initialized = false;
    private int retryCount = 0;
    private const int MAX_RETRY_COUNT = 10;

    void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
        }
        
        InitializeTarget();
    }

    void LateUpdate()
    {
        if (!initialized)
        {
            retryCount++;
            if (retryCount <= MAX_RETRY_COUNT)
            {
                InitializeTarget();
            }
        }
        
        UpdateHealthBar();
        UpdateHealthText();

        if (cameraTransform != null)
        {
            transform.LookAt(cameraTransform.position);
            transform.Rotate(0, 180, 0);
        }
    }

    void InitializeTarget()
    {
        if (initialized && (targetStats != null || targetEnemyHealth != null))
        {
            return;
        }

        if (targetStats == null && targetEnemyHealth == null)
        {
            targetStats = GetComponentInParent<CharacterStats>();
        }

        if (targetStats == null && targetEnemyHealth == null)
        {
            targetEnemyHealth = GetComponentInParent<EnemyHealthManager>();
        }
        
        if (targetStats == null && targetEnemyHealth == null)
        {
            NetworkIdentity networkIdentity = GetComponentInParent<NetworkIdentity>();
            if (networkIdentity != null)
            {
                targetEnemyHealth = networkIdentity.GetComponent<EnemyHealthManager>();
                if (targetEnemyHealth == null)
                {
                    targetStats = networkIdentity.GetComponent<CharacterStats>();
                }
            }
        }
        
        if (targetStats == null && targetEnemyHealth == null)
        {
            Transform parent = transform.parent;
            while (parent != null && retryCount <= MAX_RETRY_COUNT)
            {
                targetEnemyHealth = parent.GetComponent<EnemyHealthManager>();
                if (targetEnemyHealth != null)
                {
                    break;
                }
                targetStats = parent.GetComponent<CharacterStats>();
                if (targetStats != null)
                {
                    break;
                }
                parent = parent.parent;
            }
        }

        if (targetStats != null || targetEnemyHealth != null)
        {
            initialized = true;
            UpdateHealthBar();
            UpdateHealthText();
            Debug.Log($"[HealthBarUI] 成功找到目标 - EnemyHealth: {(targetEnemyHealth != null ? "是" : "否")}, CharacterStats: {(targetStats != null ? "是" : "否")}, 重试次数: {retryCount}");
        }
        else if (retryCount >= MAX_RETRY_COUNT)
        {
            Debug.LogWarning($"[HealthBarUI] 无法找到目标组件，已达到最大重试次数 {MAX_RETRY_COUNT}");
        }
    }

    void UpdateHealthBar()
    {
        if (healthSlider != null)
        {
            if (targetStats != null)
            {
                healthSlider.maxValue = targetStats.maxHealth;
                healthSlider.value = targetStats.currentHealth;
            }
            else if (targetEnemyHealth != null)
            {
                healthSlider.maxValue = targetEnemyHealth.maxHealth;
                healthSlider.value = targetEnemyHealth.currentHealth;
            }
        }
    }

    void UpdateHealthText()
    {
        if (healthText != null)
        {
            if (targetStats != null)
            {
                healthText.text = $"{targetStats.currentHealth}";
            }
            else if (targetEnemyHealth != null)
            {
                healthText.text = $"{targetEnemyHealth.currentHealth}";
            }
        }
    }

    public void SetTarget(CharacterStats stats)
    {
        targetStats = stats;
        targetEnemyHealth = null;
        initialized = true;
        retryCount = 0;
        UpdateHealthBar();
        UpdateHealthText();
    }

    public void SetTarget(EnemyHealthManager enemyHealth)
    {
        targetEnemyHealth = enemyHealth;
        targetStats = null;
        initialized = true;
        retryCount = 0;
        UpdateHealthBar();
        UpdateHealthText();
    }
}
