using UnityEngine;
using UnityEngine.UIElements;

public class ResourceUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private TextElement iceText;
    private TextElement foodText;
    private TextElement pebblesText;

    private int lastIce = int.MinValue;
    private int lastFood = int.MinValue;
    private int lastPebbles = int.MinValue;

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        Bind();
        ForceRefresh();
    }

    private void Update()
    {
        if (GameManager.I == null)
            return;

        if (!IsBound())
        {
            Bind();
            return;
        }

        var gm = GameManager.I;

        if (gm.ice != lastIce)
        {
            lastIce = gm.ice;
            iceText.text = lastIce.ToString();
        }

        if (gm.food != lastFood)
        {
            lastFood = gm.food;
            foodText.text = lastFood.ToString();
        }

        if (gm.pebbles != lastPebbles)
        {
            lastPebbles = gm.pebbles;
            pebblesText.text = lastPebbles.ToString();
        }
    }

    private void Bind()
    {
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        if (root == null) return;

        iceText = root.Q<TextElement>("IceCount");
        foodText = root.Q<TextElement>("FoodCount");
        pebblesText = root.Q<TextElement>("PebblesCount");
    }

    private bool IsBound()
    {
        return iceText != null && foodText != null && pebblesText != null;
    }

    private void ForceRefresh()
    {
        lastIce = int.MinValue;
        lastFood = int.MinValue;
        lastPebbles = int.MinValue;
    }
}
