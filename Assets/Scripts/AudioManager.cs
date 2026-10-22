using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
public AudioSource intro;
public AudioSource ghostnormal;
public AudioSource eatpellet;
public AudioSource bonus;
public AudioSource crush;
public AudioSource dead;
public AudioSource eatghost;
public AudioSource ghostscared;
public AudioSource ghostdead;
public float introduration = 3f;
private bool ghostsScared;
private bool ghostsDead;
private bool playerDying;

    void Start()
    {
        StartCoroutine(StartGameMusic());
    }


    IEnumerator StartGameMusic()
{
    intro.Play();

    yield return new WaitForSeconds(introduration);

    intro.Stop();

    if (!ghostsScared && !ghostsDead && !playerDying)
    {
        ghostnormal.loop = true;
        ghostnormal.Play();
    }
}

    public void SetGhostScaredMusic(bool scared)
    {
        ghostsScared = scared;
        RefreshGhostMusic();
    }

    public void SetGhostDeadMusic(bool hasDeadGhost)
    {
        ghostsDead = hasDeadGhost;
        RefreshGhostMusic();
    }

    private void RefreshGhostMusic()
    {
        if (playerDying) return;
        intro.Stop();
        AudioSource next = ghostsDead ? ghostdead : ghostsScared ? ghostscared : ghostnormal;
        if (next != ghostnormal) ghostnormal.Stop();
        if (next != ghostscared) ghostscared.Stop();
        if (next != ghostdead && ghostdead != null) ghostdead.Stop();
        if (next == null) return;
        next.loop = true;
        if (!next.isPlaying) next.Play();
    }

    public void BeginPlayerDeath()
    {
        playerDying = true;
        intro.Stop();
        ghostnormal.Stop();
        ghostscared.Stop();
        if (ghostdead != null) ghostdead.Stop();
        dead.loop = false;
        dead.Play();
    }

    public void EndPlayerDeath()
    {
        playerDying = false;
        dead.Stop();
        RefreshGhostMusic();
    }
      
    public void Playeatpellet()
    {
        eatpellet.Play();
    }


    public void Playeatghost()
    {
        eatghost.Play();
    }


    public void Playbonus()
    {
        bonus.Play();
    }


    public void Playcrush()
    {
        crush.Play();
    }


    public void Playdead()
    {
        dead.Play();
    }


    public void Playghostscared()
    {
        ghostscared.Play();
    }
}
