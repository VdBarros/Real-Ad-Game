using System.Globalization;
using Game.Presentation.Pure;
using TMPro;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class NumberBadge : MonoBehaviour
    {
        public const string LabelName = "Label";

        SpriteRenderer background;

        TextMeshPro label;

        string monospace;

        float subjectWidth;

        BadgeSize size;

        public BadgeStyle Style { get; private set; }

        public int Value { get; private set; }

        public float Width
        {
            get { return size.Width; }
        }

        public float Height
        {
            get { return size.Height; }
        }

        public float Cells
        {
            get { return size.Cells; }
        }

        public float SubjectWidth
        {
            get { return subjectWidth; }
        }

        internal void Compose(BadgePart part, BadgeAssets assets)
        {
            Style = part.Style;
            subjectWidth = part.SubjectWidth;
            monospace = "<mspace=" + BadgeMetrics.MonospaceEm.ToString("0.###", CultureInfo.InvariantCulture) + "em>";

            background = gameObject.AddComponent<SpriteRenderer>();
            background.sprite = assets.Of(part.Shape);
            background.sharedMaterial = assets.Material;
            background.drawMode = SpriteDrawMode.Sliced;
            background.color = BadgePalette.Of(part.Style);

            var labelObject = new GameObject(LabelName, typeof(RectTransform));
            labelObject.transform.SetParent(transform, worldPositionStays: false);

            label = labelObject.AddComponent<TextMeshPro>();
            label.rectTransform.localPosition = new Vector3(0f, 0f, -BadgeMetrics.TextLift);
            label.rectTransform.localRotation = Quaternion.identity;
            label.rectTransform.localScale = Vector3.one;
            label.enableAutoSizing = false;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.color = BadgePalette.Text;

            Fit(part.Cells);
            Show(part.Value);
        }

        public Color Colour
        {
            get { return background.color; }
        }

        internal void Wash(Color colour)
        {
            background.color = colour;
        }

        internal void Fit(float cells)
        {
            Fit(cells, subjectWidth);
        }

        internal void Fit(float cells, float labelled)
        {
            subjectWidth = labelled;
            size = BadgeFit.Of(cells, labelled);
            background.size = new Vector2(size.Width, size.Height);
            label.rectTransform.sizeDelta = new Vector2(size.Width, size.Height);
            label.fontSize = size.FontSize;
        }

        public void Show(int value)
        {
            Value = value;
            label.text = monospace + BadgeText.Of(Style, value);
        }
    }
}
