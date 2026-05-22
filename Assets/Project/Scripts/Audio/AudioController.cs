using UnityEngine;

public class AudioController : MonoBehaviour
{
    [SerializeField] private Client client;
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip music;
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip cardShuffle;
    [SerializeField] private AudioClip cardDraw;
    [SerializeField] private AudioClip win;
    [SerializeField] private AudioClip lose;
    [SerializeField] private AudioClip tie;


    void Start()
    {
        PlayMusicLoop();
        client.OnGameStateReceived.AddListener(OnGameState);
        client.OnDrawnReceived.AddListener(OnDrawn);
        client.OnGameOverReceived.AddListener(OnGameOver);
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
        if(drawResponse.Status == DrawResponseStatus.Success)
        {
           PlaySFX(cardDraw); 
        }      
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

    private void OnGameOver(GameOver gameOver)
    {
        switch (gameOver.GameOutcome)
        {
            case Outcome.PlayerWin:
                PlaySFX(win);
                break;

            case Outcome.EnemyWin:
                PlaySFX(lose);
                break;

            case Outcome.Tie:
                PlaySFX(tie);
                break;
        }
    }
}
