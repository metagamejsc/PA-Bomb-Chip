using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AudioManager : MonoBehaviour
{
    public static AudioManager ins;
    public AudioSource sound;
    public AudioSource music;
    public AudioClip claimSound;
    public AudioClip loseSound;
    public AudioClip winSound;
    public AudioClip bgSound;
    public AudioClip bombSound;
    public AudioClip chipSound;
    public AudioClip clickSound;

    private void Awake()
    {
        ins = this;
    }

    private void Start()
    {
        if (bgSound)
        {
            PlayMusic();
        }
    }

    public void PlaySound(AudioClip audioClip)
    {
        sound.PlayOneShot(audioClip,1);
    }
    public void PlaySoundClick()
    {
        sound.PlayOneShot(clickSound,1);
    }
    public void PlaySoundClaim()
    {
        sound.PlayOneShot(claimSound,1);
    }
    public void PlaySoundBomb()
    {
        sound.PlayOneShot(bombSound,1);
    }
    public void PlaySoundChip()
    {
        sound.PlayOneShot(chipSound,1);
    }

    public void PlayMusicLose()
    {
        music.loop = false;
        music.clip = loseSound;
        music.Play();
    }
    public void PlayMusicWin()
    {
        music.loop = false;
        music.clip = winSound;
        music.Play();
    }
    public void PlayMusic()
    {
        music.loop = true;
        music.clip = bgSound;
        music.Play();
    }
}