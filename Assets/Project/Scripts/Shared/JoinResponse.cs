using UnityEngine;

public class JoinResponse : MonoBehaviour
{
    public int PlayerId;
    public JoinResponseStatus Status;
}

public enum JoinResponseStatus
{
    Error,
    Success,
    ServerFull
}
