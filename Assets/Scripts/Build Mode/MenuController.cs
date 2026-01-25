using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

[RequireComponent(typeof(UIDocument))]
public class MenuController : MonoBehaviour
{
    private static readonly Dictionary<string, MenuController> registry = new();

    public static bool TryGet(string id, out MenuController menu) => registry.TryGetValue(id, out menu);

    public static bool IsPointerOverAnyOpenMenuUI(Vector2 screenPos)
    {
        foreach (var kvp in registry)
        {
            var mc = kvp.Value;
            if (mc == null || !mc.enabled) continue;
            if (!mc.isOpen) continue;
            if (mc.root == null || mc.root.panel == null) continue;

            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(mc.root.panel, screenPos);
            VisualElement picked = mc.root.panel.Pick(panelPos);

            if (picked != null && picked.pickingMode != PickingMode.Ignore)
                return true;
        }

        return false;
    }

    public static void CloseAllOpenMenus()
    {
        foreach (var kvp in registry)
        {
            var menu = kvp.Value;
            if (menu != null && menu.isOpen)
                menu.Close();
        }
    }

    [Header("Menu Identity")]
    [SerializeField] private string menuId = "Build";

    [Header("UXML")]
    [SerializeField] private string menuRootName = "MenuRoot";

    [Header("Slide Animation")]
    [SerializeField] private float slideDuration = 0.18f;
    [SerializeField] private float hiddenMarginTop = -420f;

    private UIDocument uiDocument;
    private VisualElement root;
    private VisualElement menuRoot;

    private bool isOpen = false;
    private bool animating = false;

    public bool IsOpen => isOpen;

    private void OnEnable()
    {
        if (string.IsNullOrWhiteSpace(menuId))
        {
            return;
        }

        registry[menuId] = this;
    }

    private void OnDisable()
    {
        if (!string.IsNullOrWhiteSpace(menuId) && registry.TryGetValue(menuId, out var existing) && existing == this)
            registry.Remove(menuId);
    }

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        menuRoot = root.Q<VisualElement>(menuRootName);
        if (menuRoot == null)
        {
            enabled = false;
            return;
        }

        root.style.display = DisplayStyle.Flex;

        menuRoot.RegisterCallback<PointerDownEvent>(e => { if (isOpen) e.StopImmediatePropagation(); });
        menuRoot.RegisterCallback<PointerUpEvent>(e => { if (isOpen) e.StopImmediatePropagation(); });
        menuRoot.RegisterCallback<ClickEvent>(e => { if (isOpen) e.StopImmediatePropagation(); });

        root.RegisterCallback<PointerDownEvent>(OnRootPointerDown);

        ApplyMarginTransition(slideDuration);

        HideImmediate();
    }

    private void OnRootPointerDown(PointerDownEvent evt)
    {
        if (!isOpen || animating) return;

        Vector2 panelPos = evt.localPosition;
        VisualElement picked = root.panel.Pick(panelPos);

        if (picked == root || picked == null)
        {
            Close();
        }
    }

    private void Update()
    {
        if (!isOpen || animating) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    public void Toggle()
    {
        if (animating) return;

        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (!enabled || animating || menuRoot == null) return;
        if (isOpen) return;

        CloseAllOtherMenus();

        animating = true;

        SetPickingRecursive(root, PickingMode.Ignore);

        ApplyMarginTransition(slideDuration);

        menuRoot.style.marginTop = hiddenMarginTop;

        menuRoot.schedule.Execute(() =>
        {
            menuRoot.style.marginTop = 0f;

            menuRoot.schedule.Execute(() =>
            {
                animating = false;
                isOpen = true;

                SetPickingRecursive(root, PickingMode.Position);

            }).ExecuteLater((int)(slideDuration * 1000f) + 20);

        }).ExecuteLater(1);
    }

    public void Close()
    {
        if (!enabled || animating || menuRoot == null) return;
        if (!isOpen)
        {
            HideImmediate();
            return;
        }

        animating = true;

        SetPickingRecursive(root, PickingMode.Ignore);

        ApplyMarginTransition(slideDuration);

        menuRoot.style.marginTop = hiddenMarginTop;

        menuRoot.schedule.Execute(() =>
        {
            animating = false;
            isOpen = false;

            SetPickingRecursive(root, PickingMode.Ignore);

        }).ExecuteLater((int)(slideDuration * 1000f) + 20);
    }

    public void HideImmediate()
    {
        animating = false;
        isOpen = false;

        if (menuRoot != null)
            menuRoot.style.marginTop = hiddenMarginTop;

        if (root != null)
            SetPickingRecursive(root, PickingMode.Ignore);
    }

    private void CloseAllOtherMenus()
    {
        foreach (var kvp in registry)
        {
            var menu = kvp.Value;
            if (menu != null && menu != this)
                menu.Close();
        }
    }

    private void ApplyMarginTransition(float seconds)
    {
        menuRoot.style.transitionProperty = new List<StylePropertyName>
        {
            new StylePropertyName("margin-top")
        };

        menuRoot.style.transitionDuration = new List<TimeValue>
        {
            new TimeValue(seconds, TimeUnit.Second)
        };

        menuRoot.style.transitionTimingFunction = new List<EasingFunction>
        {
            EasingMode.EaseOut
        };
    }

    private void SetPickingRecursive(VisualElement ve, PickingMode mode)
    {
        if (ve == null) return;

        ve.pickingMode = mode;

        for (int i = 0; i < ve.hierarchy.childCount; i++)
            SetPickingRecursive(ve.hierarchy[i], mode);
    }
}