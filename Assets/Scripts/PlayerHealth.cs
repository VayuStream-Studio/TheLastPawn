using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    private int currentHealth;
    public Slider healthBarUI;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void TakeDamage(int amount)
    {
        // Check if player is currently dodging (using i-frames!)
        PlayerDodge dodgeComponent = GetComponent<PlayerDodge>();
        if (dodgeComponent != null && dodgeComponent.isInvulnerable)
        {
            Debug.Log("DODGED! Attack phased right through you.");
            return; 
        }

        currentHealth -= amount;
        UpdateHealthUI();
        
        // Trigger screen shake or red damage flash here if desired
        Debug.Log($"Player took damage! Current HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHealthUI()
    {
        if (healthBarUI != null)
        {
            healthBarUI.maxValue = maxHealth;
            healthBarUI.value = currentHealth;
        }
    }

    void Die()
    {
        Debug.Log("Player perished. Reloading scene...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}