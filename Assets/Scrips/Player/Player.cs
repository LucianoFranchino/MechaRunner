using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    [Header("Sound Effects")]
    [SerializeField] private AudioClip jumpSound;

    [Header("Player Jump")]
    private Rigidbody2D rb;
    [SerializeField] private float jumpForce;
    [SerializeField] private float doubleJumpForce;
    public LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float checkRadius = 0.2f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float lastGroundedTime;
    private bool doubleJump;
    [SerializeField] private ParticleSystem dust;


    private Animator animator;
    //Sacar de aca
    public ScoreManager score;
    public GameObject pauseMenu;
    private bool pause;

    public void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        CreateDust();
        Time.timeScale = 1;
    }
    private void Update()
    {
        JumpCheck();
        if (Input.GetKeyDown(KeyCode.J))
        {
            Reset();
            Debug.Log("reinicio");
        }
    }

    private void JumpCheck ()
    {
        bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            doubleJump = true;
        }

        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded || (Time.time - lastGroundedTime <= coyoteTime))
            {
                Jump(jumpForce);
            }
            else if (doubleJump)
            {
                Jump(doubleJumpForce);
                doubleJump = false;
            }
        }
    }
    public void Jump(float force)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocityX, force);
        animator.Play("Jump");
        AudioManager.instance.PlayAudio(jumpSound);
    }

    //private void DoubleJump()
    //{
    //    rb.AddForce(Vector2.up * (jumpForce / 2f));
    //    doubleJump = true;
    //}

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

    void CreateDust()
    {
        dust.Play();
    }

    public void Reset()
    {
        PlayerPrefs.SetInt("Coins", 0);
        PlayerPrefs.SetInt("highscore", 0);
    }
}
