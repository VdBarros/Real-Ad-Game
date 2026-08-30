using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public static class FigureContour
    {
        public const string ShaderPath = "Shaders/FigureRim";

        public const string MaterialName = "FigureContour";

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        static readonly int WidthId = Shader.PropertyToID("_RimWidth");

        public static Material Raise()
        {
            var shader = Resources.Load<Shader>(ShaderPath);

            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = MaterialName,
                hideFlags = HideFlags.HideAndDontSave
            };

            material.SetColor(BaseColorId, Tints.Of(FigureRim.Contour));
            material.SetFloat(WidthId, FigureRim.Width);

            return material;
        }

        public static int Draw(GameObject instance, Material contour)
        {
            if (instance == null || contour == null)
            {
                return 0;
            }

            var drawn = 0;

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var worn = renderer.sharedMaterials;

                if (worn == null || worn.Length == 0 || Carries(worn, contour))
                {
                    continue;
                }

                var dressed = new Material[worn.Length + 1];
                worn.CopyTo(dressed, 0);
                dressed[worn.Length] = contour;
                renderer.sharedMaterials = dressed;
                drawn++;
            }

            return drawn;
        }

        public static bool IsContour(Material material)
        {
            return material != null && material.shader != null
                && material.shader == Resources.Load<Shader>(ShaderPath);
        }

        static bool Carries(Material[] worn, Material contour)
        {
            for (var slot = 0; slot < worn.Length; slot++)
            {
                if (worn[slot] == contour)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
