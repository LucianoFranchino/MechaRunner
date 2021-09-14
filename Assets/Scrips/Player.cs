using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public GameObject hurtSound;

    public GameObject jumpSound;
    public ParticleSystem dust;
    public GameObject pauseMenu;
    public Animator animator;
    public Rigidbody2D rb;
    public float force;
    private bool pause, doubleJump;
    public int health = 2;
    public ScoreManager score;
    public DeadthCreen deathMenu;
    public AudioSource source;
    
    public GameObject deathSound;

    public bool secondChance;

  public void Start()
    {
        CreateDust();
        Time.timeScale = 1;
        if (!source) source = GetComponent<AudioSource>();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            Reset();
            Debug.Log("reinicio"); 
        }
        
        if (doubleJump && rb.velocity.y == 0)
            doubleJump = false;
    }

    public void SFX(AudioClip clip, float volume)
    {
        source.PlayOneShot(clip, volume);
    }

    public void OnTriggerEnter2D(Collider2D choque)
    {
        if (choque.CompareTag("Enemy"))
        {
            Damage();
        }
    }

    public void Damage()
    {
        animator.Play("Damage");
        Instantiate(hurtSound, transform.position, Quaternion.identity);

    }

    public void EnemyDamage(int dmg)
    {
        health -= dmg;
        if (health <= 0 && !secondChance)
        {
            score.Save();
            
            Death();
        }
        else if (health <= 0 && secondChance)
        {
            health = 4;
            // Efectitos lindos
        }
    }

        public void Restart()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Menu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
    }

    public void Jump()
    {
        
        animator.Play("Jump");
        Instantiate(jumpSound, transform.position, Quaternion.identity);
        if (rb.velocity.y == 0)
            rb.AddForce(Vector2.up * force);
        else if (rb.velocity.y != 0 && !doubleJump)
            DoubleJump();
        
    }

    private void DoubleJump()
    {
        rb.AddForce(Vector2.up * (force / 2f));
        doubleJump = true;
       
    }

    public void Death()
    {
        Instantiate(deathSound, transform.position, Quaternion.identity);
        deathMenu.ToggleEndMenu();
        
    }
    
    public void Pause()
    {
        pause = !pause;
        pauseMenu.SetActive(pause);
        score.scoreIncreasing = !pause;
        if (pause)
            Time.timeScale = 0;
        else
            Time.timeScale = 1;
    }

    void CreateDust(){
        dust.Play();
    }

    public void Reset(){
        PlayerPrefs.SetInt("Coins", 0); 
        PlayerPrefs.SetInt("highscore", 0);
    }
}
