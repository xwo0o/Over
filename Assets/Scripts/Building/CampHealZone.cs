using UnityEngine;

public class CampHealZone : MonoBehaviour
{
    public float healPerSecond = 5f;

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player_new"))
        {
            CharacterStats stats = other.GetComponentInChildren<CharacterStats>();
            if (stats != null && stats.currentHealth < stats.maxHealth)
            {
                stats.currentHealth += Mathf.CeilToInt(healPerSecond * Time.deltaTime);
                if (stats.currentHealth > stats.maxHealth)
                {
                    stats.currentHealth = stats.maxHealth;
                }
            }
        }
    }
}
