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

        Color washed;

        float crowd = 1f;

        Vector3 home;

        Vector3 perched;

        float lift;

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
            washed = BadgePalette.Of(part.Style);

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

            home = transform.localPosition;
            perched = home;
            Repaint();

            Fit(part.Cells);
            Show(part.Value);
        }

        public Color Colour
        {
            get { return washed; }
        }

        public float Opacity
        {
            get { return crowd; }
        }

        public float Lift
        {
            get { return lift; }
        }

        public Vector3 Home
        {
            get { return home; }
        }

        public int Order
        {
            get { return background == null ? 0 : background.sortingOrder; }
        }

        internal void Wash(Color colour)
        {
            washed = colour;
            Repaint();
        }

        internal void Fade(float opacity)
        {
            crowd = opacity;
            Repaint();
        }

        internal void Reseat()
        {
            var here = transform.localPosition;
            if (here != perched)
            {
                home = here;
            }
        }

        internal void Raise(float metres)
        {
            var up = IsoProjection.CameraUp;

            lift = metres;
            perched = new Vector3(
                home.x + up.X * metres, home.y + up.Y * metres, home.z + up.Z * metres);
            transform.localPosition = perched;
        }

        internal void Draw(int order)
        {
            background.sortingOrder = order;

            var glyphs = label.GetComponent<Renderer>();
            if (glyphs != null)
            {
                glyphs.sortingOrder = order + 1;
            }
        }

        void Repaint()
        {
            var plate = washed;
            plate.a *= crowd;
            background.color = plate;

            var glyphs = BadgePalette.Text;
            glyphs.a *= crowd;
            label.color = glyphs;
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
