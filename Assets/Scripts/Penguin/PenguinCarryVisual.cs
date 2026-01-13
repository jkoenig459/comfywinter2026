using UnityEngine;

public class PenguinCarryVisual : MonoBehaviour
{
    [Header("Hook this to the empty transform above the hands")]
    public Transform carrySocket;

    private GameObject carriedGO;
    private SpriteRenderer carriedSR;

    public void ShowCarried(Sprite sprite)
    {
        if (!carrySocket)
        {
            Debug.LogWarning("CarrySocket not assigned on PenguinCarryVisual.");
            return;
        }

        if (carriedGO == null)
        {
            carriedGO = new GameObject("CarriedItem");
            carriedGO.transform.SetParent(carrySocket, worldPositionStays: false);
            carriedGO.transform.localPosition = Vector3.zero;

            carriedSR = carriedGO.AddComponent<SpriteRenderer>();

            var penguinSR = GetComponentInChildren<SpriteRenderer>();
            if (penguinSR != null)
            {
                carriedSR.sortingLayerID = penguinSR.sortingLayerID;
                carriedSR.sortingOrder = penguinSR.sortingOrder + 1;
            }
        }

        carriedSR.sprite = sprite;
        carriedGO.SetActive(sprite != null);
    }

    public void HideCarried()
    {
        if (carriedGO != null) carriedGO.SetActive(false);
    }
}
