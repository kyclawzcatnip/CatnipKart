using UnityEngine;
using UnityEngine.UI;
using CatnipCart.Kart;
using CatnipCart.Track;
using CatnipCart.Items;

namespace CatnipCart.UI
{
    /// <summary>
    /// In-race HUD: position, lap counter, item display with big popup,
    /// countdown, speedometer, results screen.
    /// </summary>
    public class RaceUI : MonoBehaviour
    {
        [Header("References")]
        public Core.RaceManager raceManager;
        public CheckpointSystem checkpointSystem;
        public KartController playerKart;

        // UI elements (created at runtime)
        private Text positionText;
        private Text lapText;
        private Text countdownText;
        private Text speedText;
        private Text itemText;
        private Text itemNameText;       // Big item name popup
        private Text itemDescText;       // Item description
        private Text resultsText;
        private Text controlsText;       // Controls hint
        private GameObject resultsPanel;
        private GameObject itemPopupPanel;
        private CanvasGroup countdownGroup;
        private CanvasGroup itemPopupGroup;
        private float itemPopupTimer;

        // Roulette display
        private Text rouletteText;
        private bool isShowingRoulette;
        private float rouletteFlashTimer;
        private int rouletteIndex;
        private readonly string[] rouletteItems = { "🧶", "🐾", "🌿", "🔴", "✨" };

        void Start()
        {
            BuildUI();

            if (raceManager != null)
            {
                raceManager.OnCountdownTick += OnCountdown;
                raceManager.OnRaceComplete += ShowResults;
                raceManager.OnLapComplete += OnLap;
            }

            if (playerKart != null)
            {
                var holder = playerKart.GetComponent<ItemHolder>();
                if (holder != null)
                {
                    holder.OnItemReceived += OnItemReceived;
                    holder.OnItemUsed += OnItemUsed;
                    holder.OnRouletteUpdate += OnRouletteUpdate;
                }
            }
        }

        void BuildUI()
        {
            // Create Canvas
            var canvasGO = new GameObject("RaceCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Position display (top-left) — big and bold
            positionText = CreateText(canvasGO.transform, "PositionText",
                new Vector2(120, -60), 72, TextAnchor.MiddleLeft, FontStyle.Bold);
            positionText.text = "1st";

            // Lap counter (top-center)
            lapText = CreateText(canvasGO.transform, "LapText",
                new Vector2(0, -40), 36, TextAnchor.MiddleCenter, FontStyle.Normal);
            lapText.text = "Lap 1/3";

            // Speed (bottom-right)
            speedText = CreateText(canvasGO.transform, "SpeedText",
                new Vector2(-120, 60), 28, TextAnchor.MiddleRight, FontStyle.Normal);

            // === ITEM BOX (top-right) — shows current held item ===
            // Background box for item
            var itemBoxGO = new GameObject("ItemBox");
            itemBoxGO.transform.SetParent(canvasGO.transform, false);
            var itemBoxRT = itemBoxGO.AddComponent<RectTransform>();
            itemBoxRT.anchorMin = new Vector2(1, 1);
            itemBoxRT.anchorMax = new Vector2(1, 1);
            itemBoxRT.pivot = new Vector2(1, 1);
            itemBoxRT.anchoredPosition = new Vector2(-40, -40);
            itemBoxRT.sizeDelta = new Vector2(120, 120);
            var itemBoxBg = itemBoxGO.AddComponent<Image>();
            itemBoxBg.color = new Color(0, 0, 0, 0.5f);

            // Item emoji (big, centered in box)
            itemText = CreateText(itemBoxGO.transform, "ItemEmoji",
                Vector2.zero, 52, TextAnchor.MiddleCenter, FontStyle.Bold);
            itemText.text = "";
            var itemTextRT = itemText.GetComponent<RectTransform>();
            itemTextRT.anchorMin = Vector2.zero; itemTextRT.anchorMax = Vector2.one;
            itemTextRT.sizeDelta = Vector2.zero;
            itemTextRT.anchoredPosition = Vector2.zero;

            // "Press E" hint under item box
            var useHint = CreateText(canvasGO.transform, "UseHint",
                new Vector2(-100, -170), 18, TextAnchor.MiddleCenter, FontStyle.Normal);
            useHint.text = "Press E to use";
            useHint.color = new Color(1, 1, 1, 0.6f);
            var useHintRT = useHint.GetComponent<RectTransform>();
            useHintRT.anchorMin = new Vector2(1, 1);
            useHintRT.anchorMax = new Vector2(1, 1);
            useHintRT.pivot = new Vector2(0.5f, 1);

            // === ITEM POPUP (center, appears when you get an item) ===
            itemPopupPanel = new GameObject("ItemPopup");
            itemPopupPanel.transform.SetParent(canvasGO.transform, false);
            itemPopupGroup = itemPopupPanel.AddComponent<CanvasGroup>();
            itemPopupGroup.alpha = 0f;

            var popupRT = itemPopupPanel.AddComponent<RectTransform>();
            popupRT.anchorMin = new Vector2(0.5f, 0.5f);
            popupRT.anchorMax = new Vector2(0.5f, 0.5f);
            popupRT.sizeDelta = new Vector2(500, 120);
            popupRT.anchoredPosition = new Vector2(0, 100);

            var popupBg = itemPopupPanel.AddComponent<Image>();
            popupBg.color = new Color(0, 0, 0, 0.6f);

            itemNameText = CreateText(itemPopupPanel.transform, "ItemName",
                new Vector2(0, 12), 38, TextAnchor.MiddleCenter, FontStyle.Bold);
            var nameRT = itemNameText.GetComponent<RectTransform>();
            nameRT.anchorMin = Vector2.zero; nameRT.anchorMax = Vector2.one;
            nameRT.sizeDelta = Vector2.zero;
            nameRT.anchoredPosition = new Vector2(0, 12);

            itemDescText = CreateText(itemPopupPanel.transform, "ItemDesc",
                new Vector2(0, -18), 20, TextAnchor.MiddleCenter, FontStyle.Normal);
            itemDescText.color = new Color(0.8f, 0.8f, 0.8f);
            var descRT = itemDescText.GetComponent<RectTransform>();
            descRT.anchorMin = Vector2.zero; descRT.anchorMax = Vector2.one;
            descRT.sizeDelta = Vector2.zero;
            descRT.anchoredPosition = new Vector2(0, -18);

            // === CONTROLS HINT (bottom-left) ===
            controlsText = CreateText(canvasGO.transform, "Controls",
                new Vector2(20, 20), 16, TextAnchor.LowerLeft, FontStyle.Normal);
            controlsText.text = "W/↑ Gas  |  S/↓ Brake  |  A/D Steer  |  Space Drift  |  E Item  |  R Restart";
            controlsText.color = new Color(1, 1, 1, 0.4f);
            var controlsRT = controlsText.GetComponent<RectTransform>();
            controlsRT.anchorMin = Vector2.zero;
            controlsRT.anchorMax = Vector2.zero;
            controlsRT.pivot = Vector2.zero;
            controlsRT.sizeDelta = new Vector2(800, 40);

            // === COUNTDOWN (center) ===
            var countdownGO = new GameObject("CountdownGroup");
            countdownGO.transform.SetParent(canvasGO.transform, false);
            countdownGroup = countdownGO.AddComponent<CanvasGroup>();
            countdownText = CreateText(countdownGO.transform, "CountdownText",
                Vector2.zero, 120, TextAnchor.MiddleCenter, FontStyle.Bold);
            countdownText.text = "";

            // === RESULTS PANEL (hidden) ===
            resultsPanel = new GameObject("ResultsPanel");
            resultsPanel.transform.SetParent(canvasGO.transform, false);
            var rt = resultsPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var bg = resultsPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            resultsText = CreateText(resultsPanel.transform, "ResultsText",
                Vector2.zero, 48, TextAnchor.MiddleCenter, FontStyle.Bold);

            resultsPanel.SetActive(false);
        }

        Text CreateText(Transform parent, string name, Vector2 pos, int size,
            TextAnchor anchor, FontStyle style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(500, 100);

            // Anchor based on position
            if (pos.x < -50) { rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(1, 0); }
            else if (pos.x > 50) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero; rt.pivot = Vector2.zero; }
            else { rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); }

            if (pos.y > 0) { rt.anchorMin = new Vector2(rt.anchorMin.x, 0); rt.anchorMax = new Vector2(rt.anchorMax.x, 0); }
            else if (pos.y < 0) { rt.anchorMin = new Vector2(rt.anchorMin.x, 1); rt.anchorMax = new Vector2(rt.anchorMax.x, 1); }

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.fontStyle = style;
            text.color = Color.white;

            // Outline for readability
            var outline = go.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            return text;
        }

        private bool playerFinished;
        private int finalPosition;

        void Update()
        {
            if (playerKart == null || checkpointSystem == null) return;

            var progress = checkpointSystem.GetProgress(playerKart.transform);
            if (progress == null) return;

            // === POSITION (freeze when finished) ===
            if (!playerFinished)
            {
                string[] suffixes = { "st", "nd", "rd", "th" };
                int pos = progress.position;
                string suffix = pos <= 3 ? suffixes[pos - 1] : suffixes[3];
                positionText.text = $"{pos}{suffix}";
                positionText.color = pos switch
                {
                    1 => new Color(1f, 0.85f, 0f),    // Gold
                    2 => new Color(0.75f, 0.75f, 0.8f), // Silver
                    3 => new Color(0.8f, 0.5f, 0.2f),   // Bronze
                    _ => Color.white
                };

                // === LAP ===
                int lap = Mathf.Max(1, progress.currentLap + 1);
                lapText.text = $"Lap {Mathf.Min(lap, raceManager.totalLaps)}/{raceManager.totalLaps}";
            }

            // === SPEED ===
            float kmh = Mathf.Abs(playerKart.CurrentSpeed) * 3.6f;
            speedText.text = $"{kmh:F0} km/h";

            // === ITEM POPUP FADE ===
            if (itemPopupTimer > 0)
            {
                itemPopupTimer -= Time.deltaTime;
                if (itemPopupTimer > 0.5f)
                    itemPopupGroup.alpha = 1f;
                else
                    itemPopupGroup.alpha = itemPopupTimer / 0.5f; // Fade out
            }

            // === ROULETTE ===
            if (isShowingRoulette)
            {
                rouletteFlashTimer -= Time.deltaTime;
                if (rouletteFlashTimer <= 0)
                {
                    rouletteFlashTimer = 0.08f;
                    rouletteIndex = (rouletteIndex + 1) % rouletteItems.Length;
                    itemText.text = rouletteItems[rouletteIndex];
                }
            }

            // === COUNTDOWN FADE ===
            if (countdownGroup != null && countdownGroup.alpha > 0)
                countdownGroup.alpha -= Time.deltaTime;
        }

        // === ITEM EVENTS ===

        void OnRouletteUpdate(int idx)
        {
            isShowingRoulette = true;
        }

        void OnItemReceived(ItemHolder.ItemType item)
        {
            isShowingRoulette = false;

            // Update the item box icon
            string emoji = GetItemEmoji(item);
            itemText.text = emoji;
            itemText.color = GetItemColor(item);

            // Show the big popup with name + description
            string itemName = GetItemName(item);
            string itemDesc = GetItemDesc(item);
            itemNameText.text = $"{emoji} {itemName}";
            itemNameText.color = GetItemColor(item);
            itemDescText.text = itemDesc;
            itemPopupTimer = 3f;
            itemPopupGroup.alpha = 1f;
        }

        void OnItemUsed()
        {
            itemText.text = "";
            itemPopupGroup.alpha = 0f;
            itemPopupTimer = 0f;
        }

        string GetItemEmoji(ItemHolder.ItemType item) => item switch
        {
            ItemHolder.ItemType.YarnBall => "🧶",
            ItemHolder.ItemType.Hairball => "🐾",
            ItemHolder.ItemType.CatnipBoost => "🌿",
            ItemHolder.ItemType.LaserPointer => "🔴",
            ItemHolder.ItemType.GoldenCatnip => "✨",
            _ => ""
        };

        string GetItemName(ItemHolder.ItemType item) => item switch
        {
            ItemHolder.ItemType.YarnBall => "Yarn Ball",
            ItemHolder.ItemType.Hairball => "Hairball Trap",
            ItemHolder.ItemType.CatnipBoost => "Catnip Boost",
            ItemHolder.ItemType.LaserPointer => "Laser Pointer",
            ItemHolder.ItemType.GoldenCatnip => "Golden Catnip",
            _ => "???"
        };

        string GetItemDesc(ItemHolder.ItemType item) => item switch
        {
            ItemHolder.ItemType.YarnBall => "Fires forward — spins out whoever it hits!",
            ItemHolder.ItemType.Hairball => "Drops behind you — a sticky trap for followers!",
            ItemHolder.ItemType.CatnipBoost => "Instant speed boost! Nyoom!",
            ItemHolder.ItemType.LaserPointer => "Shrinks & slows ALL other racers!",
            ItemHolder.ItemType.GoldenCatnip => "MEGA BOOST + Invincibility for 8 seconds!",
            _ => ""
        };

        Color GetItemColor(ItemHolder.ItemType item) => item switch
        {
            ItemHolder.ItemType.YarnBall => new Color(0.9f, 0.4f, 0.4f),
            ItemHolder.ItemType.Hairball => new Color(0.6f, 0.5f, 0.3f),
            ItemHolder.ItemType.CatnipBoost => new Color(0.3f, 0.9f, 0.4f),
            ItemHolder.ItemType.LaserPointer => new Color(1f, 0.3f, 0.3f),
            ItemHolder.ItemType.GoldenCatnip => new Color(1f, 0.85f, 0f),
            _ => Color.white
        };

        // === RACE EVENTS ===

        void OnCountdown(int num)
        {
            if (countdownGroup != null) countdownGroup.alpha = 1f;
            countdownText.text = num > 0 ? num.ToString() : "GO!";
        }

        void OnLap(CheckpointSystem.RacerProgress progress)
        {
            if (progress.racer == playerKart.transform)
            {
                lapText.color = Color.yellow;
                // Flash back to white after 1 second
                Invoke(nameof(ResetLapColor), 1f);
            }
        }

        void ResetLapColor()
        {
            if (lapText != null) lapText.color = Color.white;
        }

        void ShowResults()
        {
            if (resultsPanel == null) return;

            var progress = checkpointSystem.GetProgress(playerKart.transform);
            if (progress == null) return;

            // Lock in the finishing position!
            playerFinished = true;
            finalPosition = progress.position;

            // Freeze the position text to final place
            string[] suffixes = { "st", "nd", "rd", "th" };
            string suffix = finalPosition <= 3 ? suffixes[finalPosition - 1] : suffixes[3];
            positionText.text = $"{finalPosition}{suffix}";
            positionText.fontSize = 90; // Make it bigger to celebrate!
            lapText.text = "FINISH!";
            lapText.color = new Color(1f, 0.85f, 0f);

            // Show results panel
            resultsPanel.SetActive(true);

            string place = finalPosition switch
            {
                1 => "🏆 1st Place! 🏆",
                2 => "🥈 2nd Place!",
                3 => "🥉 3rd Place!",
                _ => $"{finalPosition}th Place"
            };

            string celebrate = finalPosition <= 3 ? "\n\nGreat job! 🐱" : "\n\nBetter luck next time!";
            resultsText.text = $"FINISH!\n\n{place}{celebrate}\n\nPress R to restart";
        }
    }
}
