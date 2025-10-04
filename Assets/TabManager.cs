using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TabManager : MonoBehaviour
{
    [Header("UI References")]
    public UIDocument uiDocument;

    [Header("Popup Animation Settings")]
    [SerializeField] private float popupDuration = 0.25f;
    [SerializeField] private AnimationCurve popupEasing = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Tab Slide Animation Settings")]
    [SerializeField] private float tabSlideDuration = 0.3f;
    [SerializeField] private AnimationCurve tabSlideEasing = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float tabSlideDistancePercent = 100f;

    [Header("Tab Button Bounce Settings")]
    [SerializeField] private float buttonBounceDuration = 0.25f;
    [SerializeField] private float buttonBounceScale = 1.2f;
    [SerializeField] private AnimationCurve buttonBounceEasing = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private VisualElement root;
    private VisualElement tabsContainer;
    private List<VisualElement> tabs = new();
    private List<VisualElement> menus = new();

    private int currentTab;
    private Button closeButton;

    //  Added references for all popups
    private VisualElement settingsMenu;
    private VisualElement profileMenu;
    private VisualElement moneyMenu;
    private VisualElement livesMenu;
    private VisualElement starsMenu;

    private Coroutine slideRoutine;

    void Start()
    {
        root = uiDocument.rootVisualElement;
        tabsContainer = root.Q<VisualElement>("TabsContainer");

        InitializeTabs();
        SetupBottomBarButtons();

        closeButton = root.Q<Button>("xButton");
        if (closeButton != null)
            closeButton.clicked += CloseAllMenus;

        //  Query all popup menus
        settingsMenu = root.Q<VisualElement>("Settings");
        profileMenu = root.Q<VisualElement>("Profile");
        moneyMenu = root.Q<VisualElement>("Money");
        livesMenu = root.Q<VisualElement>("Lives");
        starsMenu = root.Q<VisualElement>("Stars");

        //  Add them to menus list so CloseAllMenus handles them automatically
        menus.AddRange(new[] { settingsMenu, profileMenu, moneyMenu, livesMenu, starsMenu });

        //  Hook up all popup buttons inside tab3's topbar
        var topBar = tabsContainer.Q<VisualElement>("tab3")?.Q<VisualElement>("topbar");
        topBar?.Q<Button>("settings")?.RegisterCallback<ClickEvent>(_ => ShowMenu(settingsMenu));
        topBar?.Q<Button>("profile")?.RegisterCallback<ClickEvent>(_ => ShowMenu(profileMenu));
        topBar?.Q<Button>("money")?.RegisterCallback<ClickEvent>(_ => ShowMenu(moneyMenu));
        topBar?.Q<Button>("lives")?.RegisterCallback<ClickEvent>(_ => ShowMenu(livesMenu));
        topBar?.Q<Button>("stars")?.RegisterCallback<ClickEvent>(_ => ShowMenu(starsMenu));

        currentTab = Mathf.Clamp(2, 0, tabs.Count - 1);
        SetContainerPositionInstant();

        RegisterBounceForAllButtons();
        CloseAllMenus();
    }

    // ------------------ TAB INITIALIZATION ------------------

    private void InitializeTabs()
    {
        tabs.Clear();
        for (int i = 1; i <= 5; i++)
            tabs.Add(tabsContainer.Q<VisualElement>($"tab{i}"));
    }

    private void SetupBottomBarButtons()
    {
        var bottomBar = root.Q<VisualElement>("TabSwitcher");
        if (bottomBar == null) return;

        for (int i = 0; i < 5; i++)
        {
            int idx = i;
            var button = bottomBar.Q<Button>($"btn{i + 1}");
            if (button != null)
            {
                button.RegisterCallback<ClickEvent>(_ => SwitchTab(idx));
            }
        }
    }

    // ------------------ TAB SWITCHING ------------------

    private void SwitchTab(int newTab)
    {
        if (newTab == currentTab || newTab < 0 || newTab >= tabs.Count) return;

        foreach (var tab in tabs)
            tab?.RemoveFromClassList("active");

        tabs[newTab]?.AddToClassList("active");
        AnimateContainerPosition(currentTab, newTab);
        currentTab = newTab;

        UpdateActiveTabButtonState();
    }
    private void UpdateActiveTabButtonState()
    {
        var bottomBar = root.Q<VisualElement>("TabSwitcher");
        if (bottomBar == null) return;

        for (int i = 0; i < 5; i++)
        {
            var button = bottomBar.Q<Button>($"btn{i + 1}");
            if (button == null) continue;

            var child = button.Children().First();
            if (child == null) continue;

            float targetY = (i == currentTab) ? -80f : 0f;
            StartCoroutine(AnimateTabButtonChild(child, targetY));
        }
    }

    private IEnumerator AnimateTabButtonChild(VisualElement child, float targetY)
    {
        float startY = child.resolvedStyle.translate.y;
        float elapsed = 0f;

        while (elapsed < tabSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / tabSlideDuration);
            float eased = tabSlideEasing.Evaluate(t);
            float currentY = Mathf.Lerp(startY, targetY, eased);

            child.style.translate = new StyleTranslate(new Translate(0, currentY));
            yield return null;
        }

        child.style.translate = new StyleTranslate(new Translate(0, targetY));
    }


    private void SetContainerPositionInstant()
    {
        float offsetPercent = -tabSlideDistancePercent * currentTab + 200f;
        tabsContainer.style.translate = new StyleTranslate(
            new Translate(new Length(offsetPercent, LengthUnit.Percent), 0)
        );
    }

    private void AnimateContainerPosition(int fromTab, int toTab)
    {
        if (slideRoutine != null) StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(SlideTabsRoutine(fromTab, toTab));
    }

    private IEnumerator SlideTabsRoutine(int fromTab, int toTab)
    {
        float startPercent = -tabSlideDistancePercent * fromTab + 200f;
        float endPercent = -tabSlideDistancePercent * toTab + 200f;
        float elapsed = 0f;

        while (elapsed < tabSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / tabSlideDuration);
            float eased = tabSlideEasing.Evaluate(t);

            float currentPercent = Mathf.Lerp(startPercent, endPercent, eased);
            tabsContainer.style.translate = new StyleTranslate(
                new Translate(new Length(currentPercent, LengthUnit.Percent), 0)
            );

            yield return null;
        }

        tabsContainer.style.translate = new StyleTranslate(
            new Translate(new Length(endPercent, LengthUnit.Percent), 0)
        );
    }

    // ------------------ BUTTON BOUNCE ------------------

    private IEnumerator AnimateButtonBounce(Button button)
    {
        float elapsed = 0f;
        while (elapsed < buttonBounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / buttonBounceDuration);
            float eased = buttonBounceEasing.Evaluate(t);

            float scale = Mathf.Lerp(1f, buttonBounceScale, Mathf.Sin(eased * Mathf.PI));
            button.style.scale = new StyleScale(new Scale(Vector3.one * scale));

            yield return null;
        }

        button.style.scale = new StyleScale(new Scale(Vector3.one));
    }

    private void RegisterBounceForAllButtons()
    {
        foreach (var button in root.Query<Button>().ToList())
        {
            button.RegisterCallback<ClickEvent>(_ => StartCoroutine(AnimateButtonBounce(button)));
        }
    }

    // ------------------ POPUP MENUS ------------------

    // ------------------ POPUP MENUS ------------------

    private void ShowMenu(VisualElement menu)
    {
        if (menu == null) return;

        CloseAllMenus();
        closeButton?.RemoveFromClassList("hidden");

        menu.RemoveFromClassList("hidden");
        menu.AddToClassList("active");
        menu.pickingMode = PickingMode.Position;

        // Bring it onscreen before animating
        menu.style.translate = new StyleTranslate(new Translate(0, 0));

        StartCoroutine(AnimatePopupRoutine(menu));
    }

    private void CloseAllMenus()
    {
        closeButton?.AddToClassList("hidden");
        foreach (var menu in menus)
        {
            if (menu == null) continue;
            menu.RemoveFromClassList("active");
            menu.AddToClassList("hidden");
            menu.pickingMode = PickingMode.Ignore;

            // Push far offscreen so it never blocks interaction
            menu.style.translate = new StyleTranslate(new Translate(0, -3000));
        }
    }

    private IEnumerator AnimatePopupRoutine(VisualElement menu)
    {
        menu.style.scale = new StyleScale(new Scale(Vector3.zero));
        float elapsed = 0f;

        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popupDuration);
            float eased = popupEasing.Evaluate(t);

            menu.style.scale = new StyleScale(new Scale(Vector3.one * eased));
            yield return null;
        }

        menu.style.scale = new StyleScale(new Scale(Vector3.one));
    }



}
