using UnityEngine;

public class GameConfigProvider : MonoBehaviour
{
    public static GameConfigProvider Instance;

    void Awake()
    {
        Instance = this;
    }
    public GameConfig GameConfig;
}
