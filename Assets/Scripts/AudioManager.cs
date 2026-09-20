using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    // Background music
    public AudioSource intro;
    public AudioSource ghostNormal;

    // Sound effects
    public AudioSource eatPellet;
    public AudioSource eatGhost;
    public AudioSource bonus;
    public AudioSource crush;
    public AudioSource dead;
    public AudioSource ghostScared;


    void Start()
    {
        StartCoroutine(StartGameMusic());
    }


    IEnumerator StartGameMusic()
    {
        // Play intro music
        intro.Play();


        // Wait until intro finishes OR 3 seconds pass
        yield return new WaitForSeconds(
            Mathf.Min(intro.clip.length, 3f)
        );


        // Stop intro
        intro.Stop();


        // Start normal ghost background music
        ghostNormal.loop = true;
        ghostNormal.Play();
    }


    // These functions can be called later by gameplay scripts

    public void PlayEatPellet()
    {
        eatPellet.Play();
    }


    public void PlayEatGhost()
    {
        eatGhost.Play();
    }


    public void PlayBonus()
    {
        bonus.Play();
    }


    public void PlayCrush()
    {
        crush.Play();
    }


    public void PlayDead()
    {
        dead.Play();
    }


    public void PlayGhostScared()
    {
        ghostScared.Play();
    }
}