using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Delivery.Editor
{
    /// <summary>Cooking-style tickets and compact HUD, authored into the scene.</summary>
    internal static class DeliveryUIBuilder
    {
        private static readonly Color Ink = new Color32(36, 29, 26, 255);
        private static readonly Color Cream = new Color32(247, 245, 230, 255);
        private static readonly Color Orange = new Color32(255, 132, 20, 255);
        private static readonly Color Gold = new Color32(255, 215, 45, 255);
        private static readonly Color Metal = new Color32(86, 91, 104, 255);
        private static readonly Color Dark = new Color32(56, 58, 66, 255);
        private static readonly Color Green = new Color32(56, 202, 62, 255);
        private static TMP_FontAsset font;
        private static Sprite white;
        private const string Icons = "Assets/Art/Delivery/Sourced/Icons/";

        public static DeliveryUIView Build(RenderTexture texture, TMP_FontAsset deliveryFont, out DeliverySwipeInput input)
        {
            font = deliveryFont;
            white = Load("Assets/Art/UI/white.png");
            var obj = new GameObject("Delivery UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            obj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = obj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0;
            var ui = obj.AddComponent<DeliveryUIView>();
            var backdrop = Panel("Kitchen brown", obj.transform, new Color32(69, 55, 47, 255));
            Fit(backdrop.rectTransform, 0, 0, 1, 1);
            var safe = Rect("Safe area", obj.transform); Fit(safe, 0, 0, 1, 1);
            safe.gameObject.AddComponent<DeliverySafeArea>();
            var bounds = Rect("Stage bounds", safe); Fit(bounds, 0, 0, 1, 1);
            bounds.offsetMin = new Vector2(24, 24); bounds.offsetMax = new Vector2(-24, -24);
            var stage = Rect("Kitchen delivery stage", bounds); Fit(stage, 0, 0, 1, 1);
            var stageFit = stage.gameObject.AddComponent<AspectRatioFitter>();
            stageFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; stageFit.aspectRatio = 1080f / 1760f;

            var hud = Box("Metal HUD", stage, Metal); Fit(hud, 0, .925f, 1, 1);
            var score = Box("Score", hud, Dark); Fit(score, .015f, .12f, .24f, .9f);
            Icon("Star", score, "star", Gold, .06f, .24f, .29f, .76f);
            ui.score = Text("Score number", score, "0", 43, Gold, .32f, .14f, .96f, .9f);
            var delivered = Box("Delivered", hud, Dark); Fit(delivered, .255f, .12f, .48f, .9f);
            Icon("Basket", delivered, "basket", Orange, .06f, .24f, .29f, .76f);
            ui.orderCount = Text("Delivered count", delivered, "0/3", 40, Orange, .32f, .14f, .96f, .9f);
            var cargo = Box("Cargo", hud, Dark); Fit(cargo, .495f, .12f, .845f, .9f);
            Icon("Cargo icon", cargo, "basket", Cream, .06f, .3f, .23f, .85f);
            ui.integrity = Text("Cargo percent", cargo, "100%", 36, Cream, .25f, .32f, .93f, .94f);
            ui.integrityFill = Meter("Cargo meter", cargo, Green, .08f, .12f, .92f, .27f);
            ui.pauseButton = Action("Pause", hud, "", "pause", Orange);
            Fit((RectTransform)ui.pauseButton.transform, .865f, .12f, .985f, .9f);

            var roadFrame = Box("Road dark frame", stage, Ink); Fit(roadFrame, 0, .105f, 1, .92f);
            var road = Rect("Road swipe surface", roadFrame); Fit(road, 0, 0, 1, 1);
            road.offsetMin = new Vector2(6, 6); road.offsetMax = new Vector2(-6, -6);
            var raw = road.gameObject.AddComponent<RawImage>(); raw.texture = texture; raw.raycastTarget = true;
            input = road.gameObject.AddComponent<DeliverySwipeInput>();
            var toast = Box("Delivery toast", roadFrame, Cream); Fit(toast, .29f, .88f, .71f, .97f);
            ui.toastPanel = toast.gameObject;
            ui.toast = Text("Toast", toast, "", 40, Ink, .06f, .08f, .94f, .92f);
            var tip = Text("Swipe hint", roadFrame, "Vuốt để đổi làn", 26, Cream, .19f, .02f, .81f, .065f);
            tip.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
            ui.hint = tip;

            var ticket = Box("Cooking delivery ticket", stage, Cream); Fit(ticket, 0, 0, 1, .10f);
            // Feedback stays outside the road so approaching hazards remain visible.
            toast.SetParent(ticket, false); Fit(toast, .72f, .47f, .97f, .92f);
            var accent = Panel("Ship orange tab", ticket, Orange); Fit(accent.rectTransform, 0, 0, .14f, 1);
            var food = Picture("Dish", ticket, Load("Assets/Art/Cooking/Food/chicken_drumstick_cooked.png"), Color.white);
            Fit(food.rectTransform, .02f, .22f, .12f, .84f); ui.orderIcon = food;
            ui.orderName = Text("Dish name", ticket, "Giao ship", 37, Ink, .17f, .40f, .69f, .89f, TextAlignmentOptions.Left);
            ui.routeCaption = Text("Order index", ticket, "1/3", 26, Dark, .17f, .15f, .35f, .42f, TextAlignmentOptions.Left);
            ui.directionIcon = Icon("Delivery arrow", ticket, "arrowLeft", Ink, .72f, .41f, .82f, .82f);
            ui.direction = Text("Distance or turn", ticket, "288 m", 33, Ink, .72f, .14f, .97f, .43f);
            ui.progressFill = Meter("Route meter", ticket, Green, .17f, .04f, .97f, .10f);
            toast.SetAsLastSibling();
            ui.arrowLeft = Load(Icons + "arrowLeft.png"); ui.arrowRight = Load(Icons + "arrowRight.png");
            ui.foodSprites = new[] { "chicken_drumstick_cooked", "sandwich", "basket", "macaroni", "cake_slice_plain" }
                .Select(name => Load(name == "basket" ? Icons + "basket.png" : "Assets/Art/Cooking/Food/" + name + ".png")).ToArray();

            ui.readyPanel = Overlay("Ready overlay", safe, .91f, out var ready);
            Banner(ready, "GIAO SHIP");
            var foods = new[] { "sandwich", "chicken_drumstick_cooked", "cake_slice_plain" };
            for (var i = 0; i < foods.Length; i++)
            {
                var slot = Box("Food slot " + i, ready, new Color32(222, 216, 195, 255));
                Fit(slot, .14f + i * .25f, .54f, .36f + i * .25f, .74f);
                var img = Picture("Food", slot, Load("Assets/Art/Cooking/Food/" + foods[i] + ".png"), Color.white);
                Fit(img.rectTransform, .12f, .12f, .88f, .88f);
            }
            ui.readyDescription = Text("Run size", ready, "3 đơn", 42, Ink, .1f, .42f, .9f, .53f);
            Text("Gesture help", ready, "Vuốt để đổi làn", 28, Dark, .1f, .33f, .9f, .42f);
            ui.startButton = Action("Start delivery", ready, "LÊN ĐƯỜNG", "arrowRight", Orange);
            Fit((RectTransform)ui.startButton.transform, .10f, .15f, .90f, .29f);
            ui.readyRecord = Text("Ready record", ready, "Kỷ lục 0", 26, Dark, .16f, .055f, .84f, .12f);

            ui.countdownPanel = Overlay("Countdown overlay", safe, 1.1f, out var count, .30f);
            ui.countdownText = Text("Countdown", count, "3", 160, Orange, .1f, .1f, .9f, .9f);

            ui.pausePanel = Overlay("Pause overlay", safe, 1.05f, out var pause);
            Banner(pause, "TẠM DỪNG");
            Icon("Pause emblem", pause, "pause", Dark, .40f, .54f, .60f, .73f);
            ui.resumeButton = Action("Resume", pause, "TIẾP TỤC", "arrowRight", Orange);
            Fit((RectTransform)ui.resumeButton.transform, .10f, .32f, .90f, .49f);
            ui.restartButton = Action("Restart", pause, "Chơi lại", "return", Cream);
            Fit((RectTransform)ui.restartButton.transform, .10f, .09f, .48f, .25f);
            ui.pauseHomeButton = Action("Return ready", pause, "Về đầu", "home", Cream);
            Fit((RectTransform)ui.pauseHomeButton.transform, .52f, .09f, .90f, .25f);

            ui.resultsPanel = Overlay("Results overlay", safe, .69f, out var results, .06f);
            var top = Panel("Result orange banner", results, Orange); Fit(top.rectTransform, 0, .875f, 1, 1);
            ui.resultsTitle = Text("Result title", top.transform, "HOÀN TẤT", 47, Ink, .04f, .1f, .96f, .9f);
            Icon("Result star", results, "star", Orange, .24f, .755f, .36f, .85f);
            ui.resultsScore = Text("Result score", results, "0", 76, Ink, .37f, .74f, .76f, .86f);
            ui.resultsSummary = Text("Delivered summary", results, "0/3 đơn", 32, Dark, .08f, .68f, .92f, .74f);
            ui.resultRecord = Text("Result record", results, "Kỷ lục 0", 25, Dark, .06f, .63f, .94f, .68f);
            var scrollArea = Rect("Orders scroll", results); Fit(scrollArea, .05f, .20f, .95f, .61f);
            var scroll = scrollArea.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Panel("Viewport", scrollArea, new Color32(222, 216, 195, 255)); Fit(viewport.rectTransform, 0, 0, 1, 1); viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Order rows", viewport.transform); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(10, 10, 10, 10); layout.spacing = 8;
            layout.childControlWidth = layout.childForceExpandWidth = true;
            layout.childControlHeight = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport.rectTransform; scroll.content = content;
            ui.resultScroll = scroll; ui.resultContent = content; ui.resultRowPrefab = ResultPrefab();
            ui.resultReplayButton = Action("Play again", results, "CHƠI LẠI", "return", Orange);
            Fit((RectTransform)ui.resultReplayButton.transform, .06f, .055f, .67f, .16f);
            ui.resultHomeButton = Action("Results home", results, "Về đầu", "home", Cream);
            Fit((RectTransform)ui.resultHomeButton.transform, .71f, .055f, .94f, .16f);
            Navigate(ui.resumeButton, ui.restartButton, ui.pauseHomeButton);
            Navigate(ui.resultReplayButton, ui.resultHomeButton);
            ui.readyPanel.SetActive(true); ui.pausePanel.SetActive(false); ui.resultsPanel.SetActive(false); ui.countdownPanel.SetActive(false); ui.toastPanel.SetActive(false);
            return ui;
        }

        private static DeliveryResultRow ResultPrefab()
        {
            var root = Box("Delivery result row", null, Cream); root.sizeDelta = new Vector2(800, 112);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 112;
            var row = root.gameObject.AddComponent<DeliveryResultRow>();
            row.status = Icon("Status", root, "checkmark", Green, .025f, .26f, .095f, .74f);
            row.deliveredIcon = Load(Icons + "checkmark.png"); row.failedIcon = Load(Icons + "cross.png");
            row.title = Text("Order title", root, "Cơm gà nóng", 29, Ink, .13f, .44f, .72f, .9f, TextAlignmentOptions.Left);
            row.details = Text("Order status", root, "100%", 23, Dark, .13f, .12f, .72f, .43f, TextAlignmentOptions.Left);
            row.reward = Text("Reward", root, "+100", 33, Ink, .74f, .18f, .98f, .82f, TextAlignmentOptions.Right);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, "Assets/Prefabs/Delivery/DeliveryResultRow.prefab");
            Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<DeliveryResultRow>();
        }

        private static void Navigate(params Button[] buttons)
        {
            for (var i = 0; i < buttons.Length; i++)
            {
                var previous = buttons[(i + buttons.Length - 1) % buttons.Length];
                var next = buttons[(i + 1) % buttons.Length];
                buttons[i].navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = previous, selectOnLeft = previous, selectOnDown = next, selectOnRight = next };
            }
        }

        private static void Banner(Transform parent, string title)
        {
            var bar = Panel("Ship orange banner", parent, Orange); Fit(bar.rectTransform, 0, .80f, 1, 1);
            Text("Title", bar.transform, title, 57, Ink, .05f, .16f, .95f, .84f);
        }
        private static GameObject Overlay(string name, Transform safe, float aspect, out RectTransform card, float inset = .10f)
        {
            var shade = Panel(name, safe, new Color(0, 0, 0, .68f)); Fit(shade.rectTransform, 0, 0, 1, 1); shade.raycastTarget = true;
            var area = Rect("Popup bounds", shade.transform); Fit(area, inset, .08f, 1 - inset, .92f);
            var frame = Panel("Yellow selection frame", area, Gold); Fit(frame.rectTransform, 0, 0, 1, 1);
            var fit = frame.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = aspect;
            card = Box("Cooking ticket", frame.transform, Cream); Fit(card, 0, 0, 1, 1); card.offsetMin = new Vector2(9, 9); card.offsetMax = new Vector2(-9, -9);
            return shade.gameObject;
        }
        private static Button Action(string name, Transform parent, string caption, string icon, Color color)
        {
            var face = Box(name, parent, color); var image = face.GetComponent<Image>(); image.raycastTarget = true;
            var button = face.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1, .94f, .64f); colors.selectedColor = new Color(1, .88f, .35f); colors.pressedColor = new Color(.72f, .69f, .6f); colors.disabledColor = new Color(.65f, .65f, .65f, .75f); button.colors = colors;
            if (string.IsNullOrEmpty(caption)) Icon("Icon", face, icon, Ink, .27f, .22f, .73f, .78f);
            else
            {
                Icon("Icon", face, icon, Ink, .06f, .27f, .20f, .73f);
                Text("Label", face, caption, 35, Ink, .23f, .12f, .94f, .88f);
            }
            return button;
        }
        private static Image Meter(string name, Transform parent, Color color, float x0, float y0, float x1, float y1)
        {
            var bg = Panel(name, parent, Ink); Fit(bg.rectTransform, x0, y0, x1, y1);
            var fill = Panel("Fill", bg.transform, color); Fit(fill.rectTransform, 0, 0, 1, 1); fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
            return fill;
        }
        private static RectTransform Box(string name, Transform parent, Color color)
        {
            var panel = Panel(name, parent, color);
            var outline = panel.gameObject.AddComponent<Outline>(); outline.effectColor = Ink; outline.effectDistance = new Vector2(4, -4); outline.useGraphicAlpha = false;
            return panel.rectTransform;
        }
        private static Image Icon(string name, Transform parent, string icon, Color color, float x0, float y0, float x1, float y1)
        { var image = Picture(name, parent, Load(Icons + icon + ".png"), color); Fit(image.rectTransform, x0, y0, x1, y1); return image; }
        private static Image Picture(string name, Transform parent, Sprite sprite, Color color)
        { var image = Rect(name, parent).gameObject.AddComponent<Image>(); image.sprite = sprite; image.color = color; image.preserveAspect = true; image.raycastTarget = false; return image; }
        private static Image Panel(string name, Transform parent, Color color)
        { var image = Picture(name, parent, white, color); image.preserveAspect = false; return image; }
        private static TMP_Text Text(string name, Transform parent, string text, float size, Color color, float x0, float y0, float x1, float y1, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>(); Fit(label.rectTransform, x0, y0, x1, y1);
            label.font = font; label.text = text; label.fontSize = label.fontSizeMax = size; label.fontSizeMin = size * .76f; label.enableAutoSizing = true;
            label.fontStyle = FontStyles.Bold; label.color = color; label.alignment = align; label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Ellipsis; label.raycastTarget = false;
            return label;
        }
        private static RectTransform Rect(string name, Transform parent)
        { var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect; }
        private static void Fit(RectTransform rect, float x0, float y0, float x1, float y1)
        { rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1); rect.offsetMin = rect.offsetMax = Vector2.zero; rect.pivot = new Vector2(.5f, .5f); }
        private static Sprite Load(string path)
        { var result = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(); if (result == null) throw new System.InvalidOperationException("Missing UI sprite " + path); return result; }
    }
}
