using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    public int attackDamage = 20;
    private Collider weaponCollider;

    void Start()
    {
        weaponCollider = GetComponent<Collider>();
        // Start with the hitbox turned OFF so it doesn't hurt anyone while walking
        weaponCollider.enabled = false;
    }

    // Call this from Animation Events when the swing starts
    public void EnableHitbox()
    {
        weaponCollider.enabled = true;
    }

    // Call this from Animation Events when the swing ends
    public void DisableHitbox()
    {
        weaponCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Make sure you don't hit yourself
        if (other.CompareTag("Player")) return;

        // Check if we hit an enemy (you can tag enemies as "Enemy")
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Hit enemy for " + attackDamage + " damage!");
            // Add your enemy health script call here later, e.g.:
            // other.GetComponent<EnemyHealth>()?.TakeDamage(attackDamage);
        }
    }
}