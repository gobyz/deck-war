using UnityEngine;

public class GameConfigProvider : MonoBehaviour
{
    public static GameConfigProvider Instance;
    public GameConfig GameConfig;

    void Awake()
    {
        Instance = this;
    }
}
