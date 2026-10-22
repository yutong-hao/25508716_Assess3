using UnityEngine;

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
private bool roundStarting;

    public void BeginRoundCountdown()
    {
        roundStarting = true;
        ghostnormal.Stop();
        ghostscared.Stop();
        if (ghostdead != null) ghostdead.Stop();
        intro.loop = true;
        intro.Play();
    }

    public void CompleteRoundCountdown()
    {
        roundStarting = false;
        intro.Stop();
        intro.loop = false;
        RefreshGhostMusic();
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
        if (playerDying || roundStarting) return;
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
