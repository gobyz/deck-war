using UnityEngine;

public class Anchor : MonoBehaviour
{
    [SerializeField] private Transform startDrawAnchor;
    [SerializeField] private Transform endDrawAnchor;
    public Transform StartDrawAnchor => startDrawAnchor;
    public Transform EndDrawAnchor => endDrawAnchor;
}
