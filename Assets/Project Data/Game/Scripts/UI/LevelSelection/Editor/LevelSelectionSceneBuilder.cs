#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Watermelon.BusStop;

namespace Watermelon.EditorTools
{
    /// <summary>
    /// Builds the approved Level Selection composition as a fully serialized,
    /// designer-editable 1080x1920 Canvas. Runtime controllers never reposition it.
    /// </summary>
    [InitializeOnLoad]
    public static class LevelSelectionSceneBuilder
    {
        private const string ScenePath = "Assets/Project Data/Game/Scenes/LevelSelection.unity";
        private const string AssetFolder = "Assets/Project Data/Game/Images/LevelSelection";
        private const float W = 1080f;
        private const float H = 1920f;

        private static readonly string[] Required =
        {
            "taj_mahal_riverside_game_landscape.png",
            "ornate_blue_gold_game_banner.png",
            "glossy_golden_game_ui_nameplate.png",
            "glossy_green_leaves_with_golden_flourish.png",
            "glossy_green_leaf_cluster_with_gold_finial.png",
            "glossy_waving_indian_flag.png",
            "glossy_blue_and_gold_game_ui_frame.png",
            "glossy_blue_and_gold_ornate_game_frame.png",
            "ornate_blue_and_gold_parchment_panel.png",
            "glossy_blue_and_gold_level_card.png",
            "ornate_blue_and_gold_level_card.png",
            "glossy_locked_level_card_ui.png",
            "glossy_blue_and_gold_game_badge.png",
            "glossy_blue_and_gold_game_frame.png",
            "glossy_golden_parchment_banner.png",
            "glossy_green_gold_trimmed_game_button.png",
            "glossy_locked_pill_button.png",
            "glossy_golden_back_arrow_button.png",
            "glossy_gold_trimmed_blue_arrow_button.png",
            "glossy_india_map_emblem.png",
            "sunset_serenity_along_the_varanasi_ghats.png",
            "delhi_street_festival_at_india_gate.png",
            "mumbai_gateway_waterfront_adventure.png",
            "glossy_blue_and_gold_country_progress_bar.png",
            "glossy_gold_ornate_game_banner.png",
            "glossy_blue_lotus_game_emblem.png",
            "glossy_tricolour_india_map_icon.png",
            "glossy_blue_game_progress_bar.png",
            "glossy_green_progress_bar_fill.png",
            "glossy_golden_game_ui_badge.png",
            "glossy_green_leaf_cluster_with_gold_accents.png",
            "glossy_green_leaf_and_gold_sprig_cluster.png"
        };

        static LevelSelectionSceneBuilder()
        {
            EditorApplication.delayCall += TryAutoBakeOpenScene;
        }

        private static void TryAutoBakeOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid() || active.path != ScenePath)
                return;

            if (GameObject.Find("NEW Level Selection") != null)
                return;

            if (MissingAssets().Count == 0)
                BakeInternal(false);
        }

        [MenuItem("Conveyor Chef/Level Selection/0. Import Generated Art Pack", priority = 0)]
        public static void ImportGeneratedArtPack()
        {
            string zipPath = EditorUtility.OpenFilePanel(
                "Select ConveyorChef_LevelSelection_Complete_Assets.zip",
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "zip");

            if (string.IsNullOrWhiteSpace(zipPath))
                return;

            Directory.CreateDirectory(AssetFolder);

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (string required in Required)
                    {
                        ZipArchiveEntry entry = archive.Entries.FirstOrDefault(e =>
                            string.Equals(Path.GetFileName(e.FullName), required, StringComparison.OrdinalIgnoreCase));

                        if (entry == null)
                            continue;

                        entry.ExtractToFile(AssetFolder + "/" + required, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[LevelSelection] Art import failed: " + ex);
                EditorUtility.DisplayDialog("Level Selection", "Import failed:\n\n" + ex.Message, "OK");
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ImportSprites();

            List<string> missing = MissingAssets();
            if (missing.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "Level Selection Assets Missing",
                    "The ZIP is missing:\n\n- " + string.Join("\n- ", missing),
                    "OK");
                return;
            }

            BakeInternal(false);
            EditorUtility.DisplayDialog(
                "Level Selection Ready",
                "Imported the generated art pack and rebuilt LevelSelection.unity as a fully editable Canvas.",
                "OK");
        }

        [MenuItem("Conveyor Chef/Level Selection/1. Bake or Replace Editable Level Selection", priority = 1)]
        public static void Bake()
        {
            List<string> missing = MissingAssets();
            if (missing.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "Level Selection Assets Missing",
                    "Import the generated art pack first.\n\nMissing:\n- " + string.Join("\n- ", missing),
                    "OK");
                return;
            }

            BakeInternal(true);
        }

        [MenuItem("Conveyor Chef/Level Selection/2. Open Editable Level Selection", priority = 2)]
        public static void Open()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Level Selection", "LevelSelection.unity was not found.", "OK");
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            if (GameObject.Find("NEW Level Selection") == null && MissingAssets().Count == 0)
                BakeInternal(false);

            GameObject root = GameObject.Find("NEW Level Selection");
            if (root != null)
            {
                Selection.activeGameObject = root;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }

        [MenuItem("Conveyor Chef/Level Selection/3. Validate Editable Level Selection", priority = 3)]
        public static void ValidateScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject root = GameObject.Find("NEW Level Selection");
            LevelSelectionController controller = root != null
                ? root.GetComponentInChildren<LevelSelectionController>(true)
                : null;

            string message =
                "Editable root: " + (root != null ? "OK" : "MISSING") + "\n" +
                "Controller: " + (controller != null ? "OK" : "MISSING") + "\n" +
                "Generated art: " + (Required.Length - MissingAssets().Count) + "/" + Required.Length + "\n" +
                "Reference resolution: 1080x1920\n" +
                "Runtime layout mutation: DISABLED";

            EditorUtility.DisplayDialog("Level Selection Validation", message, "OK");
        }

        private static void BakeInternal(bool askBeforeReplace)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (askBeforeReplace && File.Exists(ScenePath))
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Replace LevelSelection.unity?",
                    "This rebuilds the visual hierarchy with the approved editable Level Selection layout.\n\n" +
                    "Use this only when you want to reset the scene composition.",
                    "Replace",
                    "Cancel");

                if (!replace)
                    return;
            }

            ImportSprites();

            Sprite bg = S("taj_mahal_riverside_game_landscape.png");
            Sprite header = S("ornate_blue_gold_game_banner.png");
            Sprite subtitlePlate = S("glossy_golden_game_ui_nameplate.png");
            Sprite leafLeft = S("glossy_green_leaves_with_golden_flourish.png");
            Sprite leafRight = S("glossy_green_leaf_cluster_with_gold_finial.png");
            Sprite flag = S("glossy_waving_indian_flag.png");
            Sprite heroOuter = S("glossy_blue_and_gold_game_ui_frame.png");
            Sprite heroFrame = S("glossy_blue_and_gold_ornate_game_frame.png");
            Sprite descriptionPanel = S("ornate_blue_and_gold_parchment_panel.png");
            Sprite selectedCard = S("glossy_blue_and_gold_level_card.png");
            Sprite unlockedCard = S("ornate_blue_and_gold_level_card.png");
            Sprite lockedCard = S("glossy_locked_level_card_ui.png");
            Sprite numberBadge = S("glossy_blue_and_gold_game_badge.png");
            Sprite thumbFrame = S("glossy_blue_and_gold_game_frame.png");
            Sprite namePlate = S("glossy_golden_parchment_banner.png");
            Sprite playButtonSprite = S("glossy_green_gold_trimmed_game_button.png");
            Sprite lockedButtonSprite = S("glossy_locked_pill_button.png");
            Sprite leftArrowSprite = S("glossy_golden_back_arrow_button.png");
            Sprite rightArrowSprite = S("glossy_gold_trimmed_blue_arrow_button.png");
            Sprite indiaEmblem = S("glossy_india_map_emblem.png");
            Sprite thumb1 = S("sunset_serenity_along_the_varanasi_ghats.png");
            Sprite thumb2 = S("delhi_street_festival_at_india_gate.png");
            Sprite thumb3 = S("mumbai_gateway_waterfront_adventure.png");

            Sprite progressOuter = S("glossy_blue_and_gold_country_progress_bar.png");
            Sprite progressTitlePlate = S("glossy_gold_ornate_game_banner.png");
            Sprite progressEmblemFrame = S("glossy_blue_lotus_game_emblem.png");
            Sprite progressIndiaIcon = S("glossy_tricolour_india_map_icon.png");
            Sprite progressTrack = S("glossy_blue_game_progress_bar.png");
            Sprite progressFillSprite = S("glossy_green_progress_bar_fill.png");
            Sprite progressValueBadge = S("glossy_golden_game_ui_badge.png");
            Sprite progressLeafLeft = S("glossy_green_leaf_cluster_with_gold_accents.png");
            Sprite progressLeafRight = S("glossy_green_leaf_and_gold_sprig_cluster.png");

            Sprite settingsSprite = Existing("glossy_blue_gear_settings_icon.png");
            Sprite coinSprite = Existing("glossy_gold_dollar_coin_icon.png");
            Sprite plusSprite = Existing("glossy_green_add_button.png");
            Sprite starSprite = Existing("glossy_golden_game_star_icon.png");
            Sprite lockSprite = Existing("glossy_blue_locked_level_icon.png");
            Sprite chefSprite = Existing("cheerful_chef_mascot_welcoming_gesture.png");
            Sprite bubbleSprite = Existing("glossy_chef_s_dialogue_bubble.png");
            Sprite homeIcon = Existing("Home.png");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateEventSystem();
            Canvas canvas = CreateCanvas();

            RectTransform root = R("NEW Level Selection", canvas.transform);
            Stretch(root);
            root.gameObject.AddComponent<Watermelon.LevelSelectionResponsiveLayout>();

            Image background = I("Background Artwork", root, bg, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(W, H), false);
            Stretch(background.rectTransform);
            background.raycastTarget = false;

            // TOP HUD
            RectTransform topHud = R("Top HUD", root);
            Set(topHud, new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(1080f, 136f));

            Image homeDisc = I("Home Button Frame", topHud, numberBadge, new Vector2(0f, 0.5f), new Vector2(72f, 0f), new Vector2(108f, 108f), true);
            Button homeButton = homeDisc.gameObject.AddComponent<Button>();
            homeButton.targetGraphic = homeDisc;
            if (homeIcon != null)
            {
                Image hi = I("Home Icon", homeDisc.transform, homeIcon, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f), true);
                hi.raycastTarget = false;
            }

            Image coinBar = I("Coin Counter", topHud, progressOuter, new Vector2(0f, 0.5f), new Vector2(330f, 0f), new Vector2(280f, 92f), false);
            Image coinIcon = I("Coin Icon", coinBar.transform, coinSprite, new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(66f, 66f), true);
            coinIcon.raycastTarget = false;
            TextMeshProUGUI coinText = T("Coin Value", coinBar.transform, "1,250", 31f, new Vector2(15f, 0f), new Vector2(120f, 56f));
            Button coinPlus = B("Coin Plus", coinBar.transform, plusSprite, new Vector2(1f, 0.5f), new Vector2(-38f, 0f), new Vector2(62f, 62f), true);

            Image chefBar = I("Chef Counter", topHud, progressOuter, new Vector2(1f, 0.5f), new Vector2(-325f, 0f), new Vector2(260f, 92f), false);
            Image chefMini = I("Chef Icon", chefBar.transform, chefSprite, new Vector2(0f, 0.5f), new Vector2(43f, 0f), new Vector2(62f, 68f), true);
            chefMini.raycastTarget = false;
            TextMeshProUGUI chefText = T("Chef Value", chefBar.transform, "0", 31f, new Vector2(12f, 0f), new Vector2(100f, 56f));
            Button chefPlus = B("Chef Plus", chefBar.transform, plusSprite, new Vector2(1f, 0.5f), new Vector2(-36f, 0f), new Vector2(60f, 60f), true);

            Button settingsButton = B("SettingsButton", topHud, settingsSprite, new Vector2(1f, 0.5f), new Vector2(-72f, 0f), new Vector2(104f, 104f), true);

            // COUNTRY HEADER
            RectTransform countryHeader = R("Country Header", root);
            Set(countryHeader, new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(740f, 210f));

            Image leftLeaf = I("Left Leaf", countryHeader, leafLeft, new Vector2(0f, 0.5f), new Vector2(75f, -5f), new Vector2(170f, 170f), true);
            leftLeaf.raycastTarget = false;
            Image rightLeaf = I("Right Leaf", countryHeader, leafRight, new Vector2(1f, 0.5f), new Vector2(-75f, -5f), new Vector2(170f, 170f), true);
            rightLeaf.raycastTarget = false;

            Image headerImage = I("Header Plaque", countryHeader, header, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 170f), false);
            TextMeshProUGUI countryTitle = T("Country Title", headerImage.transform, "INDIA", 66f, new Vector2(0f, 24f), new Vector2(420f, 82f));
            countryTitle.fontStyle = FontStyles.Bold;
            countryTitle.color = new Color(1f, 0.92f, 0.30f, 1f);

            Image subtitle = I("Subtitle Plate", headerImage.transform, subtitlePlate, new Vector2(0.5f, 0.5f), new Vector2(-25f, -48f), new Vector2(430f, 62f), false);
            TextMeshProUGUI subtitleText = T("Country Subtitle", subtitle.transform, "FLAVORS • CULTURE • JOURNEY", 22f, Vector2.zero, new Vector2(360f, 40f));
            subtitleText.color = new Color(0.17f, 0.14f, 0.18f, 1f);

            Image flagImage = I("Country Flag", countryHeader, flag, new Vector2(1f, 0.5f), new Vector2(-15f, -12f), new Vector2(170f, 125f), true);
            flagImage.raycastTarget = false;

            // HERO / DESCRIPTION
            Image hero = I("Hero Info Frame", root, heroOuter, new Vector2(0.5f, 1f), new Vector2(0f, -520f), new Vector2(940f, 360f), false);

            Image heroImageFrame = I("Hero Image Frame", hero.transform, heroFrame, new Vector2(0f, 0.5f), new Vector2(315f, 0f), new Vector2(590f, 300f), false);
            Image heroImage = I("Hero Image", heroImageFrame.transform, thumb1, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 250f), true);
            heroImage.raycastTarget = false;

            Image descPanel = I("Description Panel", hero.transform, descriptionPanel, new Vector2(1f, 0.5f), new Vector2(-155f, 0f), new Vector2(300f, 305f), false);
            TextMeshProUGUI descText = T("Description Text", descPanel.transform,
                "Explore India's rich food culture, vibrant cities and iconic destinations as you deliver delicious dishes across the country!",
                25f, Vector2.zero, new Vector2(235f, 240f));
            descText.textWrappingMode = TextWrappingModes.Normal;
            descText.alignment = TextAlignmentOptions.MidlineLeft;
            descText.color = new Color(0.20f, 0.14f, 0.10f, 1f);

            // LEVEL CARDS
            RectTransform cardsRoot = R("Level Cards", root);
            Set(cardsRoot, new Vector2(0.5f, 1f), new Vector2(0f, -1075f), new Vector2(1000f, 570f));

            Sprite[] thumbnails = { thumb1, thumb2, thumb3 };
            string[] cardNames = { "Varanasi Ghats", "Delhi Streets", "Mumbai Docks" };
            List<LevelSelectionLevelCard> cards = new List<LevelSelectionLevelCard>();

            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 320f;
                LevelSelectionLevelCard card = BuildCard(
                    cardsRoot,
                    i,
                    x,
                    cardNames[i],
                    thumbnails[i],
                    i == 0 ? selectedCard : (i == 1 ? unlockedCard : lockedCard),
                    selectedCard,
                    unlockedCard,
                    lockedCard,
                    numberBadge,
                    thumbFrame,
                    namePlate,
                    playButtonSprite,
                    lockedButtonSprite,
                    starSprite,
                    lockSprite);
                cards.Add(card);
            }

            Button leftArrow = B("Left Arrow", root, leftArrowSprite, new Vector2(0f, 0.5f), new Vector2(58f, -90f), new Vector2(90f, 115f), true);
            Button rightArrow = B("Right Arrow", root, rightArrowSprite, new Vector2(1f, 0.5f), new Vector2(-58f, -90f), new Vector2(90f, 115f), true);

            // GUIDE
            Image chefGuide = I("Chef Guide Mascot", root, chefSprite, new Vector2(0f, 0f), new Vector2(105f, 225f), new Vector2(215f, 290f), true);
            chefGuide.raycastTarget = false;

            Image speech = I("Chef Speech Bubble", root, bubbleSprite, new Vector2(0f, 0f), new Vector2(355f, 245f), new Vector2(360f, 135f), false);
            TextMeshProUGUI guideText = T("Guide Text", speech.transform, "Complete all 3 missions to master India's flavors!", 22f, Vector2.zero, new Vector2(290f, 85f));
            guideText.textWrappingMode = TextWrappingModes.Normal;
            guideText.color = new Color(0.12f, 0.18f, 0.32f, 1f);

            // COUNTRY PROGRESS - every generated piece remains separate/editable.
            RectTransform progressRoot = R("Country Progress", root);
            Set(progressRoot, new Vector2(1f, 0f), new Vector2(-360f, 235f), new Vector2(650f, 230f));

            Image progressBase = I("Outer Frame", progressRoot, progressOuter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 205f), false);
            Image pLeftLeaf = I("Left Leaf", progressRoot, progressLeafLeft, new Vector2(0f, 0.5f), new Vector2(70f, -8f), new Vector2(120f, 120f), true);
            pLeftLeaf.raycastTarget = false;
            Image pRightLeaf = I("Right Leaf", progressRoot, progressLeafRight, new Vector2(1f, 0.5f), new Vector2(-45f, -10f), new Vector2(110f, 110f), true);
            pRightLeaf.raycastTarget = false;

            Image emblemFrame = I("Emblem Frame", progressRoot, progressEmblemFrame, new Vector2(0f, 0.5f), new Vector2(94f, 0f), new Vector2(180f, 180f), true);
            Image emblemIcon = I("India Icon", emblemFrame.transform, progressIndiaIcon, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(115f, 115f), true);
            emblemIcon.raycastTarget = false;

            Image pTitle = I("Title Plate", progressRoot, progressTitlePlate, new Vector2(0.5f, 1f), new Vector2(80f, -40f), new Vector2(360f, 78f), false);
            TextMeshProUGUI progressTitleText = T("Progress Title", pTitle.transform, "COUNTRY PROGRESS", 24f, Vector2.zero, new Vector2(300f, 46f));
            progressTitleText.fontStyle = FontStyles.Bold;
            progressTitleText.color = new Color(0.06f, 0.18f, 0.42f, 1f);

            Image track = I("Progress Track", progressRoot, progressTrack, new Vector2(0.5f, 0.5f), new Vector2(75f, -33f), new Vector2(330f, 58f), false);
            Image fill = I("Progress Fill", track.transform, progressFillSprite, new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(0f, 38f), false);
            fill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            fill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.anchoredPosition = new Vector2(12f, 0f);
            fill.rectTransform.sizeDelta = new Vector2(0f, 38f);
            fill.type = Image.Type.Simple;
            fill.preserveAspect = false;
            fill.raycastTarget = false;

            Image valueBadge = I("Value Badge", progressRoot, progressValueBadge, new Vector2(1f, 0.5f), new Vector2(-64f, -34f), new Vector2(125f, 78f), false);
            TextMeshProUGUI progressValue = T("Progress Value", valueBadge.transform, "0/3", 30f, Vector2.zero, new Vector2(90f, 52f));
            progressValue.fontStyle = FontStyles.Bold;
            progressValue.color = new Color(0.06f, 0.18f, 0.42f, 1f);

            // BACK
            Button backButton = B("BackButton", root, leftArrowSprite, new Vector2(0f, 0f), new Vector2(135f, 70f), new Vector2(230f, 105f), true);
            TextMeshProUGUI backLabel = T("Label", backButton.transform, "BACK", 32f, new Vector2(35f, 0f), new Vector2(120f, 55f));
            backLabel.fontStyle = FontStyles.Bold;
            backLabel.color = Color.white;

            TextMeshProUGUI statusText = T("Status Text", root, "MISSION 1 SELECTED", 20f, new Vector2(0f, -825f), new Vector2(600f, 40f));
            statusText.color = new Color(1f, 1f, 1f, 0.88f);

            GameObject settingsPanel = BuildSettingsPanel(root, progressOuter,
                out Button closeSettings, out Button soundButton, out Button vibrationButton,
                out TextMeshProUGUI soundLabel, out TextMeshProUGUI vibrationLabel);
            settingsPanel.SetActive(false);

            GameObject controllerObject = new GameObject("Level Selection Controller");
            controllerObject.transform.SetParent(root, false);
            LevelSelectionController controller = controllerObject.AddComponent<LevelSelectionController>();

            controller.EditorConfigure(
                homeButton,
                backButton,
                settingsButton,
                coinPlus,
                chefPlus,
                leftArrow,
                rightArrow,
                coinText,
                chefText,
                countryTitle,
                subtitleText,
                flagImage,
                heroImage,
                descText,
                cards.ToArray(),
                thumbnails,
                guideText,
                emblemFrame,
                fill,
                progressTitleText,
                progressValue,
                statusText,
                settingsPanel,
                closeSettings,
                soundButton,
                vibrationButton,
                soundLabel,
                vibrationLabel);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureSceneInBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = root.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log("[LevelSelection] Editable level-selection scene baked and saved.");
        }

        private static LevelSelectionLevelCard BuildCard(
            RectTransform parent,
            int index,
            float x,
            string missionName,
            Sprite thumbnail,
            Sprite initialFrame,
            Sprite selectedFrame,
            Sprite unlockedFrame,
            Sprite lockedFrame,
            Sprite numberBadge,
            Sprite thumbnailFrame,
            Sprite namePlate,
            Sprite playButton,
            Sprite lockedButton,
            Sprite starSprite,
            Sprite lockSprite)
        {
            RectTransform root = R("Level " + (index + 1), parent);
            Set(root, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(300f, 530f));

            Image frame = I("Card Frame", root, initialFrame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(290f, 515f), false);
            Button selectButton = frame.gameObject.AddComponent<Button>();
            selectButton.targetGraphic = frame;

            Image badge = I("Level Number Badge", root, numberBadge, new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(94f, 94f), true);
            badge.raycastTarget = false;
            TextMeshProUGUI number = T("Level Number", badge.transform, (index + 1).ToString(), 38f, Vector2.zero, new Vector2(60f, 55f));
            number.fontStyle = FontStyles.Bold;
            number.color = Color.white;

            Image tFrame = I("Thumbnail Frame", root, thumbnailFrame, new Vector2(0.5f, 1f), new Vector2(0f, -152f), new Vector2(250f, 190f), false);
            Image tImage = I("Thumbnail", tFrame.transform, thumbnail, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(218f, 153f), true);
            tImage.raycastTarget = false;

            Image titlePlate = I("Level Name Plate", root, namePlate, new Vector2(0.5f, 0.5f), new Vector2(0f, -38f), new Vector2(250f, 62f), false);
            TextMeshProUGUI title = T("Level Name", titlePlate.transform, missionName, 23f, Vector2.zero, new Vector2(210f, 42f));
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.20f, 0.12f, 0.08f, 1f);

            Image[] stars = new Image[3];
            for (int s = 0; s < 3; s++)
            {
                stars[s] = I("Star " + (s + 1), root, starSprite, new Vector2(0.5f, 0.5f), new Vector2((s - 1) * 67f, -112f), new Vector2(52f, 52f), true);
                stars[s].raycastTarget = false;
                stars[s].color = index == 0 ? Color.white : new Color(0.16f, 0.21f, 0.30f, 0.90f);
            }

            Image actionImage = I("Action Button", root, index == 0 ? playButton : lockedButton, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(245f, 82f), false);
            Button actionButton = actionImage.gameObject.AddComponent<Button>();
            actionButton.targetGraphic = actionImage;
            TextMeshProUGUI actionText = T("Action Label", actionImage.transform, index == 0 ? "PLAY" : "LOCKED", 31f, Vector2.zero, new Vector2(180f, 55f));
            actionText.fontStyle = FontStyles.Bold;
            actionText.color = Color.white;

            GameObject lockOverlay = new GameObject("Lock Overlay", typeof(RectTransform));
            lockOverlay.transform.SetParent(tFrame.transform, false);
            RectTransform lockRect = lockOverlay.GetComponent<RectTransform>();
            Set(lockRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(95f, 95f));
            Image lockImage = lockOverlay.AddComponent<Image>();
            lockImage.sprite = lockSprite;
            lockImage.preserveAspect = true;
            lockImage.raycastTarget = false;
            lockOverlay.SetActive(index > 0);

            GameObject completed = new GameObject("Completed Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            completed.transform.SetParent(root, false);
            RectTransform completedRect = completed.GetComponent<RectTransform>();
            Set(completedRect, new Vector2(1f, 1f), new Vector2(-34f, -45f), new Vector2(54f, 54f));
            Image completedImage = completed.GetComponent<Image>();
            completedImage.sprite = starSprite;
            completedImage.raycastTarget = false;
            completed.SetActive(false);

            LevelSelectionLevelCard card = root.gameObject.AddComponent<LevelSelectionLevelCard>();
            card.EditorConfigure(
                index,
                selectButton,
                actionButton,
                frame,
                tImage,
                badge,
                number,
                title,
                stars,
                actionImage,
                actionText,
                lockOverlay,
                completed,
                selectedFrame,
                unlockedFrame,
                lockedFrame,
                playButton,
                lockedButton);

            return card;
        }

        private static GameObject BuildSettingsPanel(
            RectTransform parent,
            Sprite panelSprite,
            out Button close,
            out Button sound,
            out Button vibration,
            out TextMeshProUGUI soundLabel,
            out TextMeshProUGUI vibrationLabel)
        {
            RectTransform overlay = R("Settings Panel", parent);
            Stretch(overlay);
            Image dim = overlay.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.68f);
            dim.raycastTarget = true;

            Image panel = I("Panel", overlay, panelSprite, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650f, 620f), false);
            TextMeshProUGUI title = T("Title", panel.transform, "SETTINGS", 48f, new Vector2(0f, 190f), new Vector2(420f, 70f));
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;

            sound = TextButton(panel.transform, "Sound Button", "SOUND: ON", new Vector2(0f, 65f), out soundLabel);
            vibration = TextButton(panel.transform, "Vibration Button", "VIBRATION: ON", new Vector2(0f, -45f), out vibrationLabel);
            close = TextButton(panel.transform, "Close Button", "CLOSE", new Vector2(0f, -175f), out _);
            return overlay.gameObject;
        }

        private static Button TextButton(Transform parent, string name, string label, Vector2 pos, out TextMeshProUGUI text)
        {
            RectTransform rt = R(name, parent);
            Set(rt, new Vector2(0.5f, 0.5f), pos, new Vector2(360f, 82f));
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = new Color(0.05f, 0.30f, 0.68f, 1f);
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            text = T("Label", rt, label, 28f, Vector2.zero, new Vector2(310f, 55f));
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            return button;
        }

        private static void ImportSprites()
        {
            if (!Directory.Exists(AssetFolder))
                return;

            foreach (string file in Required)
            {
                string path = AssetFolder + "/" + file;
                if (!File.Exists(path))
                    continue;

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        private static List<string> MissingAssets()
        {
            return Required.Where(f => !File.Exists(AssetFolder + "/" + f)).ToList();
        }

        private static Sprite S(string file)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetFolder + "/" + file);
            if (sprite == null)
                throw new InvalidOperationException("Missing Level Selection sprite: " + file);
            return sprite;
        }

        private static Sprite Existing(string file)
        {
            string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(file) + " t:Sprite");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(Path.GetFileName(path), file, StringComparison.OrdinalIgnoreCase))
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
        }

        private static Canvas CreateCanvas()
        {
            GameObject go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(W, H);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            return canvas;
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.12f, 0.18f, 1f);
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static RectTransform R(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Image I(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size, bool preserve)
        {
            RectTransform rt = R(name, parent);
            Set(rt, anchor, pos, size);
            Image image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = preserve;
            image.raycastTarget = false;
            return image;
        }

        private static Button B(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size, bool preserve)
        {
            Image image = I(name, parent, sprite, anchor, pos, size, preserve);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static TextMeshProUGUI T(string name, Transform parent, string text, float fontSize, Vector2 pos, Vector2 size)
        {
            RectTransform rt = R(name, parent);
            Set(rt, new Vector2(0.5f, 0.5f), pos, size);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = false;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        private static void Set(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static void EnsureSceneInBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => string.Equals(s.path, path, StringComparison.OrdinalIgnoreCase)))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
#endif
