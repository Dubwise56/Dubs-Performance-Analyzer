using UnityEngine;
using Verse;

namespace Analyzer
{
    [StaticConstructorOnStartup]
    public static class DubResources
    {
        public static readonly Texture2D black = SolidColorMaterials.NewSolidColorTexture(Color.black);
        public static readonly Texture2D grey = SolidColorMaterials.NewSolidColorTexture(Color.grey);
        public static readonly Texture2D darkgrey = SolidColorMaterials.NewSolidColorTexture(Color.grey * 0.5f);
        public static readonly Texture2D clear = SolidColorMaterials.NewSolidColorTexture(Color.clear);
        public static readonly Texture2D red = SolidColorMaterials.NewSolidColorTexture(new Color32(160, 80, 90, 255));
        public static readonly Texture2D blue = SolidColorMaterials.NewSolidColorTexture(new Color32(80, 123, 160, 255));
        public static Texture2D sav = ContentFinder<Texture2D>.Get("DPA/UI/sav", false);
    }
}
