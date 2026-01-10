using UnityEngine;

public enum ResourceType { Ice, Food }

public class ResourceNode : MonoBehaviour
{
    public ResourceType type;
    public float workTime = 2f;   // seconds to gather
    public int yieldAmount = 1;   // per trip
}
