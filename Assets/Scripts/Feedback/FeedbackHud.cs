using System.Text;
using Parcial2.Core;
using Parcial2.Hunter;
using Parcial2.Wolves;
using UnityEngine;
using UnityEngine.UI;

namespace Parcial2.Feedback
{
    /// <summary>
    /// On-screen feedback built in code: Hunter state, gathers and chosen safe zone on top, and a short log of
    /// the detections and alerts that prove perception and communication are working.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HunterController))]
    public sealed class FeedbackHud : MonoBehaviour
    {
        private const int MaxLogLines = 8;
        private const string HunterTag = "#7CE38B";
        private const string WolfTag = "#FFB45A";

        [SerializeField, Min(10)] private int fontSize = 16;

        private readonly string[] logLines = new string[MaxLogLines];
        private readonly StringBuilder builder = new();

        private HunterController hunter;
        private WolfPack pack;
        private Text statusText;
        private Text logText;
        private int logCount;

        private HunterStateId shownState = (HunterStateId)(-1);
        private int shownGathers = -1;
        private ZoneVolume shownZone;
        private bool statusDirty = true;

        private void Awake()
        {
            hunter = GetComponent<HunterController>();
            BuildUi();
        }

        private void OnEnable()
        {
            hunter.StateChanged += OnHunterStateChanged;
            hunter.SafeZoneChosen += OnSafeZoneChosen;
            hunter.Caught += OnHunterCaught;
        }

        private void OnDisable()
        {
            hunter.StateChanged -= OnHunterStateChanged;
            hunter.SafeZoneChosen -= OnSafeZoneChosen;
            hunter.Caught -= OnHunterCaught;
        }

        private void Start()
        {
            pack = WolfPack.Instance;
            if (pack == null)
                return;

            pack.HunterSpotted += OnHunterSpotted;
            pack.HowlRaised += OnHowlRaised;
            pack.AlertReceived += OnAlertReceived;
            pack.HunterSheltered += OnHunterSheltered;
            pack.HunterEscaped += OnHunterEscaped;
        }

        private void OnDestroy()
        {
            if (pack == null)
                return;

            pack.HunterSpotted -= OnHunterSpotted;
            pack.HowlRaised -= OnHowlRaised;
            pack.AlertReceived -= OnAlertReceived;
            pack.HunterSheltered -= OnHunterSheltered;
            pack.HunterEscaped -= OnHunterEscaped;
        }

        private void Update()
        {
            // Rebuild the text only when something shown changed, so a steady frame allocates nothing.
            if (hunter.CurrentState != shownState || hunter.Gathers != shownGathers
                || hunter.ChosenSafeZone != shownZone)
                statusDirty = true;

            if (!statusDirty)
                return;

            statusDirty = false;
            shownState = hunter.CurrentState;
            shownGathers = hunter.Gathers;
            shownZone = hunter.ChosenSafeZone;
            statusText.text = BuildStatus();
        }

        private string BuildStatus()
        {
            builder.Clear();
            builder.Append("<b>HUNTER</b>  ").Append(shownState);
            builder.Append("\nGathers  ").Append(shownGathers).Append('/').Append(hunter.RequiredGathers);
            if (shownZone != null)
                builder.Append("\nSafe zone  ").Append(shownZone.name);
            return builder.ToString();
        }

        private void OnHunterStateChanged(HunterStateId from, HunterStateId to) => statusDirty = true;

        private void OnSafeZoneChosen(ZoneVolume zone, Transform wolf)
        {
            string who = wolf != null ? wolf.name : "a wolf";
            AddLog(HunterTag, $"Hunter detected {who}, fleeing to {zone.name}");
        }

        private void OnHunterCaught() => AddLog(WolfTag, "Wolves caught the Hunter");

        private void OnHunterSpotted(WolfController wolf, Vector3 position) =>
            AddLog(WolfTag, $"{wolf.name} spotted the Hunter at {Format(position)}");

        private void OnHowlRaised(WolfController source, Vector3 position) =>
            AddLog(WolfTag, $"{source.name} howled: last known position {Format(position)}");

        private void OnAlertReceived(WolfController receiver, WolfController source, Vector3 position) =>
            AddLog(WolfTag, $"{receiver.name} received the alert from {source.name}");

        private void OnHunterSheltered() => AddLog(HunterTag, "Hunter sheltered: wolves lose the target");

        private void OnHunterEscaped() => AddLog(HunterTag, "Hunter reached the exit: wolves lose the target");

        private static string Format(Vector3 position) => $"({position.x:F0}, {position.z:F0})";

        private void AddLog(string colorHex, string message)
        {
            string line = $"<color={colorHex}>[{Time.time:F1}]</color> {message}";
            if (logCount < MaxLogLines)
                logLines[logCount++] = line;
            else
            {
                for (int i = 1; i < MaxLogLines; i++)
                    logLines[i - 1] = logLines[i];
                logLines[MaxLogLines - 1] = line;
            }

            builder.Clear();
            for (int i = 0; i < logCount; i++)
                builder.Append(logLines[i]).Append('\n');
            logText.text = builder.ToString();
        }

        private void BuildUi()
        {
            GameObject canvasObject = new("FeedbackHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText = CreatePanel(canvasObject.transform, "Status", font, new Vector2(16f, -16f),
                new Vector2(420f, 88f));
            logText = CreatePanel(canvasObject.transform, "Log", font, new Vector2(16f, -112f),
                new Vector2(560f, 8f * (fontSize + 6f)));
        }

        private Text CreatePanel(Transform parent, string objectName, Font font, Vector2 position, Vector2 size)
        {
            GameObject panel = new(objectName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image background = panel.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.55f);
            background.raycastTarget = false;

            GameObject textObject = new("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 6f);
            textRect.offsetMax = new Vector2(-10f, -6f);

            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.alignment = TextAnchor.UpperLeft;
            text.raycastTarget = false;
            return text;
        }
    }
}
