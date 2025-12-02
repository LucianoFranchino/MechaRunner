using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int playerHealth = 2;
    [SerializeField] private DeadthCreen deathScreen;
    //private Animator animator;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

    //void Start()
    //{
    //    animator = GetComponent<Animator>();
    //}
    //public void OnTriggerEnter2D(Collider2D choque)
    //{
    //    if (choque.CompareTag("Enemy"))
    //    {
    //        PlayerDamage(1);
    //    }
    //}
    public void PlayerDamage(int damage)
    {
        playerHealth -= damage;
        //animator.Play("Damage");
        AudioManager.instance.PlayAudio(hurtSound);
        if(playerHealth <= 0)
        {
            Death();
            ScoreManager.instance.Save();
        }
    }

    public void Death()
    {
        AudioManager.instance.PlayAudio(deathSound);
        deathScreen.ToggleEndMenu();
    }


}
