using UnityEngine;

public class am : MonoBehaviour
{
public AudioSource intro;
public AudioSource ghNormal;
public AudioSource eatPel;
public AudioSource bonus;
public AudioSource crush;
public AudioSource dead;
public AudioSource eatGh;
public AudioSource ghScared;
public AudioSource ghDead;
public float introDur = 3f;
private bool ghScaredOn;
private bool ghAreDead;
private bool pcDying;
private bool roundStart;

    public void BeginRoundCountdown()
    {
        roundStart = true;
        ghNormal.Stop();
        ghScared.Stop();
        if (ghDead != null) ghDead.Stop();
        intro.loop = true;
        intro.Play();
    }

    public void CompleteRoundCountdown()
    {
        roundStart = false;
        intro.Stop();
        intro.loop = false;
        RefreshGhostMusic();
    }

    public void SetGhostScaredMusic(bool scared)
    {
        ghScaredOn = scared;
        RefreshGhostMusic();
    }

    public void SetGhostDeadMusic(bool hasDeadGh)
    {
        ghAreDead = hasDeadGh;
        RefreshGhostMusic();
    }

    private void RefreshGhostMusic()
    {
        if (pcDying || roundStart) return;
        intro.Stop();
        AudioSource next = ghAreDead ? ghDead : ghScaredOn ? ghScared : ghNormal;
        if (next != ghNormal) ghNormal.Stop();
        if (next != ghScared) ghScared.Stop();
        if (next != ghDead && ghDead != null) ghDead.Stop();
        if (next == null) return;
        next.loop = true;
        if (!next.isPlaying) next.Play();
    }

    public void BeginPlayerDeath()
    {
        pcDying = true;
        intro.Stop();
        ghNormal.Stop();
        ghScared.Stop();
        if (ghDead != null) ghDead.Stop();
        dead.loop = false;
        dead.Play();
    }

    public void EndPlayerDeath()
    {
        pcDying = false;
        dead.Stop();
        RefreshGhostMusic();
    }
      
    public void Playeatpellet()
    {
        eatPel.Play();
    }


    public void Playeatghost()
    {
        eatGh.Play();
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
        ghScared.Play();
    }
}
