using UnityEngine;

namespace CatnipCart.Core
{
    /// <summary>
    /// Color scheme data for each cat character, matching the Super Cat World closet system.
    /// Colors sourced directly from sprites.js CAT_SKIN values.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCatColor", menuName = "Catnip Cart/Cat Color Data")]
    public class CatColorData : ScriptableObject
    {
        public string catName = "Ginger";

        [Header("Body Colors")]
        public Color body = new Color(0.96f, 0.65f, 0.14f);       // #F5A623 - exact SCW orange
        public Color bodyDark = new Color(0.91f, 0.58f, 0.12f);   // #E8941E - darker legs
        public Color belly = new Color(0.97f, 0.75f, 0.34f);      // #F7BF56 - highlight

        [Header("Details")]
        public Color paw = Color.white;                             // #FFFFFF - white paws (SCW)
        public Color innerEar = new Color(1f, 0.56f, 0.67f);      // #FF8FAB - pink inner ear
        public Color eyes = new Color(0.1f, 0.1f, 0.18f);         // #1a1a2e - dark pupils
        public Color nose = new Color(1f, 0.42f, 0.54f);          // #FF6B8A - pink nose

        [Header("Hat")]
        public Color hatRed = new Color(0.8f, 0.13f, 0.13f);      // #CC2222 - red hat
        public Color hatBand = new Color(1f, 0.84f, 0f);          // #FFD700 - gold band
        public Color gemColor = new Color(0f, 0.9f, 0.8f);        // #00E5CC - anticatite gem
        public Color gemHighlight = new Color(0.5f, 1f, 0.94f);   // #80FFF0 - gem shine

        [Header("Kart")]
        public Color kartPrimary = new Color(0.96f, 0.65f, 0.14f);
        public Color kartSecondary = new Color(1f, 1f, 1f);
        public Color kartAccent = new Color(1f, 0.42f, 0.54f);

        /// <summary>
        /// Pre-defined color scheme matching the Super Cat World orange tabby exactly.
        /// Colors taken directly from sprites.js CAT_SKIN constant.
        /// </summary>
        public static CatColorData CreateGinger()
        {
            var data = CreateInstance<CatColorData>();
            data.catName = "Ginger";
            // Exact Super Cat World colors from sprites.js
            data.body = HexToColor("#F5A623");       // CAT_SKIN.body
            data.bodyDark = HexToColor("#E8941E");   // CAT_SKIN.legs (darker orange)
            data.belly = HexToColor("#F7BF56");      // CAT_SKIN.highlight
            data.paw = HexToColor("#FFFFFF");        // CAT_SKIN.paw (white)
            data.innerEar = HexToColor("#FF8FAB");   // CAT_SKIN.ear (pink)
            data.eyes = HexToColor("#1a1a2e");       // Dark pupil color
            data.nose = HexToColor("#FF6B8A");       // CAT_SKIN.nose (pink)
            data.hatRed = HexToColor("#CC2222");     // Hat red
            data.hatBand = HexToColor("#FFD700");    // Hat gold band
            data.gemColor = HexToColor("#00E5CC");   // Anticatite gem
            data.gemHighlight = HexToColor("#80FFF0"); // Gem highlight
            data.kartPrimary = HexToColor("#F5A623");
            data.kartSecondary = HexToColor("#ffffff");
            data.kartAccent = HexToColor("#FF6B8A");
            return data;
        }

        public static CatColorData CreateShadow()
        {
            var data = CreateInstance<CatColorData>();
            data.catName = "Shadow";
            data.body = HexToColor("#6b7280");
            data.bodyDark = HexToColor("#4b5563");
            data.belly = HexToColor("#e5e7eb");
            data.paw = HexToColor("#d1d5db");
            data.innerEar = HexToColor("#fca5a5");
            data.eyes = HexToColor("#1e40af");
            data.nose = HexToColor("#f472b6");
            data.hatRed = HexToColor("#CC2222");
            data.hatBand = HexToColor("#FFD700");
            data.gemColor = HexToColor("#00E5CC");
            data.gemHighlight = HexToColor("#80FFF0");
            data.kartPrimary = HexToColor("#6b7280");
            data.kartSecondary = HexToColor("#e5e7eb");
            data.kartAccent = HexToColor("#1e40af");
            return data;
        }

        public static CatColorData CreateMidnight()
        {
            var data = CreateInstance<CatColorData>();
            data.catName = "Midnight";
            data.body = HexToColor("#374151");
            data.bodyDark = HexToColor("#1f2937");
            data.belly = HexToColor("#6b7280");
            data.paw = HexToColor("#9ca3af");
            data.innerEar = HexToColor("#fb923c");
            data.eyes = HexToColor("#eab308");
            data.nose = HexToColor("#d1d5db");
            data.hatRed = HexToColor("#CC2222");
            data.hatBand = HexToColor("#FFD700");
            data.gemColor = HexToColor("#00E5CC");
            data.gemHighlight = HexToColor("#80FFF0");
            data.kartPrimary = HexToColor("#374151");
            data.kartSecondary = HexToColor("#6b7280");
            data.kartAccent = HexToColor("#eab308");
            return data;
        }

        public static CatColorData CreateSnow()
        {
            var data = CreateInstance<CatColorData>();
            data.catName = "Snow";
            data.body = HexToColor("#e5e7eb");
            data.bodyDark = HexToColor("#d1d5db");
            data.belly = HexToColor("#f9fafb");
            data.paw = HexToColor("#fce7f3");
            data.innerEar = HexToColor("#fca5a5");
            data.eyes = HexToColor("#7c3aed");
            data.nose = HexToColor("#f9a8d4");
            data.hatRed = HexToColor("#CC2222");
            data.hatBand = HexToColor("#FFD700");
            data.gemColor = HexToColor("#00E5CC");
            data.gemHighlight = HexToColor("#80FFF0");
            data.kartPrimary = HexToColor("#e5e7eb");
            data.kartSecondary = HexToColor("#f9fafb");
            data.kartAccent = HexToColor("#7c3aed");
            return data;
        }

        private static Color HexToColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
