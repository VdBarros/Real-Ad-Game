using System;
using Game.Presentation;
using Game.Presentation.Pure;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Flow
{
    public sealed class RecentreButton : IDisposable
    {
        public const string RootName = "RecentreButton";

        public const string CallName = "Call";

        public const string LabelName = "CallLabel";

        public const string EventsName = "ScreenEvents";

        public const string Label = "RECENTRE";

        const int Layer = 90;

        const float LabelSize = 52f;

        static readonly Color Face = new Color(0.16f, 0.19f, 0.26f, 0.93f);

        static readonly Color Ink = new Color(0.94f, 0.95f, 0.98f, 1f);

        GameObject root;

        GameObject events;

        Button call;

        bool disposed;

        public static RecentreButton Raise(Action onRecentre)
        {
            if (onRecentre == null)
            {
                throw new ArgumentNullException(nameof(onRecentre));
            }

            var button = new RecentreButton();
            button.Build(onRecentre);
            return button;
        }

        public bool IsShowing
        {
            get { return root != null && root.activeSelf; }
        }

        public Button Call
        {
            get { return call; }
        }

        public GameObject Root
        {
            get { return root; }
        }

        public void Offer(bool showing)
        {
            RequireOpen();

            if (root.activeSelf == showing)
            {
                return;
            }

            root.SetActive(showing);
        }

        void Build(Action onRecentre)
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

            var frame = Frame(CallName, root.transform);
            Seat(frame);
            var face = Paint(frame, Face);

            call = frame.gameObject.AddComponent<Button>();
            call.targetGraphic = face;
            call.onClick.AddListener(() => onRecentre());

            Write(LabelName, frame);

            RaiseEvents();
            root.SetActive(false);
        }

        void RaiseEvents()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            events = new GameObject(EventsName);
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>();
        }

        static void Write(string name, Transform parent)
        {
            var frame = Frame(name, parent);
            frame.anchorMin = Vector2.zero;
            frame.anchorMax = Vector2.one;
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.offsetMin = Vector2.zero;
            frame.offsetMax = Vector2.zero;

            var label = frame.gameObject.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = false;
            label.fontSize = LabelSize;
            label.color = Ink;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.text = Label;
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

        static void Seat(RectTransform frame)
        {
            frame.anchorMin = new Vector2(0.5f, 0f);
            frame.anchorMax = new Vector2(0.5f, 0f);
            frame.pivot = new Vector2(0.5f, 0f);
            frame.anchoredPosition = new Vector2(0f, RecentreCall.Lift);
            frame.sizeDelta = new Vector2(RecentreCall.Width, RecentreCall.Height);
        }

        void RequireOpen()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(RecentreButton));
            }
        }

        public void Dispose()
        {
            if (call != null)
            {
                call.onClick.RemoveAllListeners();
                call = null;
            }

            WorldObjects.Destroy(root);
            root = null;

            WorldObjects.Destroy(events);
            events = null;

            disposed = true;
        }
    }
}
