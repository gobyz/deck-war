using System;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    [SerializeField] Client client;
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip music;
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip cardShuffle;
    [SerializeField] private AudioClip cardDraw;

    void Start()
    {
        PlayMusicLoop();
        client.OnGameStateReceived.AddListener(OnGameState);
        client.OnDrawnReceived.AddListener(OnDrawn);
    }

    private void OnGameState(GameState gameState)
    {
        if(gameState == GameState.Shuffling)
        {
            PlaySFX(cardShuffle); 
        }      
    }

    private void OnDrawn(DrawResponse drawResponse)
    {
        PlaySFX(cardDraw);
    }

    private void PlayMusicLoop()
    {
        musicAudioSource.clip = music;
        musicAudioSource.loop = true;
        musicAudioSource.Play();
    }

    private void PlaySFX(AudioClip clip)
    {
        sfxAudioSource.PlayOneShot(clip);
    }

    public void OnJoinClick()
    {
        PlaySFX(buttonClick);
    }
    public void OnDrawClick()
    {
         PlaySFX(buttonClick);
    }
}
