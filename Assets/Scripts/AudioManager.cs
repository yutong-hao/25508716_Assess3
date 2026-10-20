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
public float introduration = 3f;

    void Start()
    {
        StartCoroutine(StartGameMusic());
    }


    IEnumerator StartGameMusic()
{
    intro.Play();

    yield return new WaitForSeconds(introduration);

    intro.Stop();

    ghostnormal.loop = true;
    ghostnormal.Play();
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
