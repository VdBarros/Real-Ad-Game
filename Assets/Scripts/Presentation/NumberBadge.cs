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

        string prefix;

        string monospace;

        public BadgeStyle Style { get; private set; }

        public long Value { get; private set; }

        internal void Compose(BadgePart part, BadgePlan plan, BadgeAssets assets)
        {
            Style = part.Style;
            prefix = BadgeStyles.Prefix(part.Style);
            monospace = "<mspace=" + BadgeMetrics.MonospaceEm.ToString("0.###", CultureInfo.InvariantCulture) + "em>";

            background = gameObject.AddComponent<SpriteRenderer>();
            background.sprite = assets.Of(part.Shape);
            background.sharedMaterial = assets.Material;
            background.drawMode = SpriteDrawMode.Sliced;
            background.size = new Vector2(part.Width, plan.Height);
            background.color = BadgePalette.Of(part.Style);

            var labelObject = new GameObject(LabelName, typeof(RectTransform));
            labelObject.transform.SetParent(transform, worldPositionStays: false);

            label = labelObject.AddComponent<TextMeshPro>();
            label.rectTransform.sizeDelta = new Vector2(part.Width, plan.Height);
            label.rectTransform.localPosition = new Vector3(0f, 0f, -BadgeMetrics.TextLift);
            label.rectTransform.localRotation = Quaternion.identity;
            label.rectTransform.localScale = Vector3.one;
            label.enableAutoSizing = false;
            label.fontSize = plan.FontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.color = BadgePalette.Text;

            Show(part.Value);
        }

        public void Show(long value)
        {
            Value = value;
            label.text = monospace + prefix + value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
