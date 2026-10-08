using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Sand Quest screens rebuilt from the original art (Resources/SandQuest) and design mockups:
    // the "Sand Quest has started!" offer, the desert journey (10 stepping stones to the chest, Level x/10,
    // Players left, 24h timer) and the victory / failure results. Rules live in SandQuest (Managers).
    public sealed partial class PopupScreens
    {
        Popup questOffer, questMap, questResult;
        TextMesh[] questOfferTimer, questLevel, questPlayers, questTimer, questResultTitle, questResultBody, questResultPrize;
        Transform questMarker;
        SpriteRenderer questResultBand, questResultPicture;
        Sprite questVictoryBand, questFailureBand;
        const float MapWidth = 5.6f, MapHeight = 9.1f, MapCenterY = -.65f;
        // Stepping stones on the mockup (sand-quest-2.png, 923x2000 preview pixels): start rock, 10 stones, chest.
        static readonly Vector2[] Stones =
        {
            new Vector2(760, 1880), new Vector2(560, 1700), new Vector2(280, 1610), new Vector2(660, 1550), new Vector2(435, 1475),
            new Vector2(230, 1390), new Vector2(510, 1350), new Vector2(670, 1265), new Vector2(465, 1235), new Vector2(255, 1210), new Vector2(440, 1120),
        };

        void BuildSandQuest()
        {
            // Offer popup.
            questOffer = Blank("Sand Quest offer popup");
            Art(questOffer, "SandQuest/bg-sandQuest-popUp", new Vector3(0, .25f, -6.2f), 3.9f);
            SetText(OutlinedLabel(questOffer, "Quest title", new Vector3(0, 2.42f, -6.8f), .068f), "Sand Quest");
            Art(questOffer, "SandQuest/quest-Victory", new Vector3(0, .82f, -6.4f), 2.55f);
            questOfferTimer = TimerPill(questOffer, new Vector3(0, -.48f, -6.6f));
            SetText(OutlinedLabel(questOffer, "Quest offer text", new Vector3(0, -1.08f, -6.8f), .03f), "Sand Quest đã bắt đầu! Thắng 10 màn\nđể hoàn thành thử thách!");
            GreenButton(questOffer, -1.85f, "quest-start", "Bắt đầu");
            CloseX(questOffer, new Vector3(1.72f, 2.62f, -8.5f), "quest-close");

            // Journey screen.
            questMap = Blank("Sand Quest map");
            var body = Art(questMap, "SandQuest/quest-map", new Vector3(0, MapCenterY, -6.2f), MapWidth);
            if (body.sprite) body.transform.localScale = new Vector3(MapWidth / body.sprite.bounds.size.x, MapHeight / body.sprite.bounds.size.y, 1);
            Art(questMap, "SandQuest/bg-sandQuest", new Vector3(0, 3.82f, -6.3f), MapWidth);
            SetText(OutlinedLabel(questMap, "Quest title", new Vector3(0, 4.62f, -6.8f), .07f), "Sand Quest");
            Label(questMap, "Quest goal", new Vector3(0, 3.9f, -6.8f), "Thắng 10 màn để hoàn thành thử thách!", .03f, new Color(.85f, .45f, .12f));
            questLevel = InfoPanel(questMap, new Vector3(-1.3f, 3.18f, -6.5f), "Màn");
            questPlayers = InfoPanel(questMap, new Vector3(1.3f, 3.18f, -6.5f), "Người chơi");
            questTimer = TimerPill(questMap, new Vector3(0, 2.42f, -6.6f));
            var sign = Art(questMap, "SandQuest/icon-signBoard", MapPoint(new Vector2(165, 800), -6.4f), 1.35f);
            SetText(OutlinedLabel(questMap, "Grand prize", sign.transform.localPosition + new Vector3(-.02f, .12f, -.2f), .028f), "Giải lớn\n" + SandQuest.GrandPrize);
            var marker = Art(questMap, "SandQuest/frame-avatar-yellow", MapPoint(Stones[0], -6.6f), .62f);
            SetText(OutlinedLabel(questMap, "You", marker.transform.localPosition + new Vector3(0, -.42f, -.1f), .03f), "BẠN");
            questMap.Root.transform.Find("You").SetParent(marker.transform, true);
            questMarker = marker.transform;
            CloseX(questMap, new Vector3(2.3f, 4.62f, -8.5f), "quest-close");
            // Full-screen page: no green button or coin counter; the house on the left goes back home.
            questMap.RightButton.transform.parent.gameObject.SetActive(false);
            foreach (var name in new[] { "bg-currency", "button-plus_0", "icon-coin", "2040" })
            { var t = questMap.Root.transform.Find(name); if (t) t.gameObject.SetActive(false); }
            var home = Child<BoxCollider>(questMap, "Hit area - quest-close", MapPoint(new Vector2(65, 1000), -8.5f));
            home.gameObject.layer = 9; home.size = new Vector3(.8f, .8f, .2f);
            home.gameObject.AddComponent<VideoUiButton>().Action = "quest-close";

            // Result popup (green band = victory, grey band = failure).
            questResult = Blank("Sand Quest result popup");
            questVictoryBand = Resources.Load<Sprite>("SandQuest/bg-sandQuest-victory-rev");
            questFailureBand = Resources.Load<Sprite>("SandQuest/bg-sandQuest-failure-rev");
            questResultBand = Art(questResult, "SandQuest/bg-sandQuest-victory-rev", new Vector3(0, .25f, -6.2f), 3.9f);
            questResultTitle = OutlinedLabel(questResult, "Result title", new Vector3(0, 2.42f, -6.8f), .06f);
            questResultPicture = Art(questResult, "SandQuest/quest-victory_2", new Vector3(0, .95f, -6.4f), 2.2f);
            questResultPrize = OutlinedLabel(questResult, "Result prize", new Vector3(0, -.45f, -6.8f), .06f);
            questResultBody = OutlinedLabel(questResult, "Result text", new Vector3(0, -1.08f, -6.8f), .03f);
            GreenButton(questResult, -1.85f, "quest-claim", "Nhận");
        }

        // A popup clone with only the dark backdrop, the coin counter and the green button group kept.
        Popup Blank(string name)
        {
            var p = Build(name);
            var keep = new[] { "Dark backdrop", "bg-currency", "button-plus_0", "icon-coin", "2040", "Next" };
            foreach (Transform child in p.Root.transform)
                if (System.Array.IndexOf(keep, child.name) < 0) child.gameObject.SetActive(false);
            return p;
        }
        void GreenButton(Popup p, float y, string action, string text)
        {
            foreach (Transform child in p.RightButton.transform.parent)
                child.localPosition = new Vector3(0, y, child.localPosition.z);
            p.RightButton.transform.parent.localScale = new Vector3(1.05f, 1.05f, 1);
            Configure(p.RightButton, p.Right, action, text);
        }
        void CloseX(Popup p, Vector3 local, string action)
        {
            var panel = Child<SpriteRenderer>(p, "Close button", local);
            panel.sprite = p.Ribbon.sprite; panel.color = new Color(.92f, .16f, .2f);
            float s = .5f / Mathf.Max(.01f, panel.sprite.bounds.size.y);
            panel.transform.localScale = new Vector3(s * panel.sprite.bounds.size.y / Mathf.Max(.01f, panel.sprite.bounds.size.x), s, 1);
            Label(p, "Close X", local + new Vector3(0, 0, -.2f), "X", .06f, Color.white);
            var hit = Child<BoxCollider>(p, "Hit area - " + action, local + new Vector3(0, 0, -.5f));
            hit.gameObject.layer = 9; hit.size = new Vector3(.6f, .6f, .2f);
            hit.gameObject.AddComponent<VideoUiButton>().Action = action;
        }
        TextMesh[] TimerPill(Popup p, Vector3 local)
        {
            Art(p, "SandQuest/bg-questTime", local, 1.75f);
            Art(p, "SandQuest/icon-questTime", local + new Vector3(-.82f, .03f, -.05f), .42f);
            return OutlinedLabel(p, "Quest timer", local + new Vector3(.12f, 0, -.2f), .036f);
        }
        TextMesh[] InfoPanel(Popup p, Vector3 local, string caption)
        {
            Art(p, "SandQuest/bg-questLevel", local, 1.9f);
            SetText(OutlinedLabel(p, caption + " caption", local + new Vector3(0, .26f, -.2f), .036f), caption);
            return OutlinedLabel(p, caption + " value", local + new Vector3(0, -.22f, -.2f), .042f);
        }
        // Preview-pixel coordinates on the mockup → popup-local position on the map body.
        static Vector3 MapPoint(Vector2 preview, float z)
        {
            float u = preview.x * 1.024f / 945f, v = (preview.y * 1.024f - 512f) / 1536f;
            return new Vector3((u - .5f) * MapWidth, MapCenterY + (.5f - v) * MapHeight, z);
        }

        // Home: the offer greets the player; a finished run shows its result first.
        public void ShowSandQuestOnHome()
        {
            if (Open || !screen.Levels) return;
            if (SandQuest.ResultPending) { ShowQuestResult(); return; }
            if (SandQuest.ShouldOffer) { SandQuest.MarkOffered(); Show(questOffer); RefreshQuestTexts(); }
        }
        // Home chest ("JOIN"): the journey while joined, otherwise the offer.
        public void OpenSandQuest()
        {
            if (Open) return;
            if (SandQuest.ResultPending) ShowQuestResult();
            else if (SandQuest.Joined) ShowQuestMap();
            else if (SandQuest.Unlocked) { Show(questOffer); RefreshQuestTexts(); }
        }
        void ShowQuestMap()
        {
            int step = Mathf.Clamp(SandQuest.StepsDone, 0, Stones.Length - 1);
            var pos = MapPoint(Stones[step], questMarker.localPosition.z);
            questMarker.localPosition = pos + new Vector3(0, .32f, 0);
            Show(questMap); RefreshQuestTexts();
        }
        void ShowQuestResult()
        {
            bool won = SandQuest.Won;
            questResultBand.sprite = won ? questVictoryBand : questFailureBand;
            // Original result art: open chest with cheering cubes / sad cubes in the rain.
            questResultPicture.sprite = Resources.Load<Sprite>(won ? "SandQuest/quest-victory_2" : "SandQuest/quest-Fail");
            SetText(questResultTitle, won ? "Chiến thắng!" : "Thất bại");
            SetText(questResultPrize, won ? "+" + SandQuest.Prize : "");
            SetText(questResultBody, won ? "Bạn chia giải " + SandQuest.GrandPrize + " xu với\n" + (SandQuest.Players - 1) + " người chơi khác!" : "Bạn đã thua một màn hoặc hết giờ.\nHãy thử lại lần sau!");
            Configure(questResult.RightButton, questResult.Right, "quest-claim", won ? "Nhận" : "OK");
            Show(questResult);
        }

        bool HandleSandQuest(string action)
        {
            switch (action)
            {
                case "quest-close": Close(); return true;
                case "quest-start": SandQuest.Join(); Close(); ShowQuestMap(); return true;
                case "quest-claim": SandQuest.Claim(); Close(); return true;
            }
            return true;
        }

        void UpdateSandQuest()
        {
            if (Time.frameCount % 10 == 0) RefreshQuestTexts();
        }
        void RefreshQuestTexts()
        {
            if (Open == questOffer.Root) SetText(questOfferTimer, SandQuest.Clock(SandQuest.Duration));
            else if (Open == questMap.Root)
            {
                SetText(questTimer, SandQuest.Clock(SandQuest.Remaining));
                SetText(questLevel, SandQuest.StepsDone + "/" + SandQuest.Steps);
                SetText(questPlayers, SandQuest.Players + "/" + SandQuest.StartPlayers);
            }
        }
    }
}
