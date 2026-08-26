using System;
using System.Globalization;
using Game.Presentation;
using Game.Presentation.Pure;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Flow
{
    public sealed class ResultScreen : IDisposable
    {
        public const string RootName = "ResultScreen";

        public const string VeilName = "Veil";

        public const string CardName = "Card";

        public const string CaptionName = "Caption";

        public const string PowerName = "FinalPower";

        public const string NextName = "Next";

        public const string NextLabelName = "NextLabel";

        public const string EventsName = "ScreenEvents";

        public const string Caption = "BOSS DOWN";

        public const string NextLabel = "NEXT";

        const int Layer = 100;

        const float CardWidth = 760f;

        const float CardHeight = 720f;

        const float ButtonWidth = 460f;

        const float ButtonHeight = 140f;

        static readonly Color Veil = new Color(0.04f, 0.05f, 0.07f, 0.82f);

        static readonly Color Card = new Color(0.11f, 0.13f, 0.18f, 1f);

        static readonly Color Ink = new Color(0.94f, 0.95f, 0.98f, 1f);

        static readonly Color Quiet = new Color(0.62f, 0.67f, 0.78f, 1f);

        static readonly Color Call = new Color(0.24f, 0.55f, 0.92f, 1f);

        GameObject root;

        GameObject events;

        InputSystemUIInputModule module;

        BaseInputModule displaced;

        TextMeshProUGUI reading;

        Button next;

        bool disposed;

        public static ResultScreen Raise(Action onNext)
        {
            if (onNext == null)
            {
                throw new ArgumentNullException(nameof(onNext));
            }

            var screen = new ResultScreen();
            screen.Build(onNext);
            return screen;
        }

        public bool IsShowing
        {
            get { return root != null && root.activeSelf; }
        }

        public int Power { get; private set; }

        public Button Next
        {
            get { return next; }
        }

        public GameObject Root
        {
            get { return root; }
        }

        public void Show(int power)
        {
            RequireOpen();

            if (power < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(power), power, "A run that beat the boss ended holding power.");
            }

            Power = power;
            reading.text = power.ToString(CultureInfo.InvariantCulture);
            root.SetActive(true);
        }

        public void Hide()
        {
            RequireOpen();
            root.SetActive(false);
        }

        void Build(Action onNext)
        {
            root = new GameObject(RootName, typeof(RectTransform));

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = Layer;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ScreenFrame.Width, ScreenFrame.Height);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            root.AddComponent<GraphicRaycaster>();

            var veil = Frame(VeilName, root.transform);
            Stretch(veil);
            Paint(veil, Veil);

            var card = Frame(CardName, veil);
            Centre(card, 0f, CardWidth, CardHeight);
            Paint(card, Card);

            Write(CaptionName, card, 220f, 64f, Quiet, Caption);
            reading = Write(PowerName, card, 40f, 220f, Ink, string.Empty);

            var button = Frame(NextName, card);
            Centre(button, -230f, ButtonWidth, ButtonHeight);
            var face = Paint(button, Call);

            next = button.gameObject.AddComponent<Button>();
            next.targetGraphic = face;
            next.onClick.AddListener(() => onNext());

            Write(NextLabelName, button, 0f, 56f, Ink, NextLabel);

            RaiseEvents();
            root.SetActive(false);
        }

        void RaiseEvents()
        {
            var standing = EventSystem.current;

            if (standing == null)
            {
                events = new GameObject(EventsName);
                events.AddComponent<EventSystem>();
                events.AddComponent<InputSystemUIInputModule>();
                return;
            }

            if (standing.GetComponent<InputSystemUIInputModule>() != null)
            {
                return;
            }

            var other = standing.GetComponent<BaseInputModule>();
            if (other != null && other.enabled)
            {
                other.enabled = false;
                displaced = other;
            }

            module = standing.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        static TextMeshProUGUI Write(
            string name, Transform parent, float y, float size, Color colour, string text)
        {
            var frame = Frame(name, parent);
            Stretch(frame);
            frame.anchoredPosition = new Vector2(0f, y);

            var label = frame.gameObject.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = false;
            label.fontSize = size;
            label.color = colour;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        static Image Paint(RectTransform frame, Color colour)
        {
            var image = frame.gameObject.AddComponent<Image>();
            image.color = colour;
            return image;
        }

        static RectTransform Frame(string name, Transform parent)
        {
            var carrier = new GameObject(name, typeof(RectTransform));
            var frame = carrier.GetComponent<RectTransform>();
            frame.SetParent(parent, worldPositionStays: false);
            return frame;
        }

        static void Stretch(RectTransform frame)
        {
            frame.anchorMin = Vector2.zero;
            frame.anchorMax = Vector2.one;
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.offsetMin = Vector2.zero;
            frame.offsetMax = Vector2.zero;
        }

        static void Centre(RectTransform frame, float y, float width, float height)
        {
            frame.anchorMin = new Vector2(0.5f, 0.5f);
            frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.anchoredPosition = new Vector2(0f, y);
            frame.sizeDelta = new Vector2(width, height);
        }

        void RequireOpen()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ResultScreen));
            }
        }

        public void Dispose()
        {
            if (next != null)
            {
                next.onClick.RemoveAllListeners();
                next = null;
            }

            WorldObjects.Destroy(root);
            root = null;

            WorldObjects.Destroy(events);
            events = null;

            WorldObjects.Destroy(module);
            module = null;

            if (displaced != null)
            {
                displaced.enabled = true;
                displaced = null;
            }

            reading = null;
            disposed = true;
        }
    }
}
