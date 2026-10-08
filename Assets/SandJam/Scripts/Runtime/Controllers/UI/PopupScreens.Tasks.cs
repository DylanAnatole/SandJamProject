using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // DAILY REWARDS and DAILY / WEEKLY TASKS panels rebuilt from the original Canvas_DailyRewards /
    // Canvas_DailyTask (MainMenu scene) and their design mockups (Texture2D/Frame 1-11, Frame 2), plus the
    // three Home widgets (RewardWidget, DailyTaskWidget, WeeklyTaskWidget) at their original positions.
    // Rules live in DailyRewards and TaskBoard (Managers).
    public sealed partial class PopupScreens
    {
        const float PanelUnit = 4.55f / 1147f;      // original panel pixels → popup units
        const float PanelY = -.25f;                 // panel centre in the popup
        Popup rewardsPanel, tasksPanel;
        readonly List<GameObject> rewardsDynamic = new List<GameObject>(), tasksDynamic = new List<GameObject>();
        TextMesh[] rewardsTimer, rewardsInfo, tasksTimer;
        TaskBoard.Kind shownKind = TaskBoard.Kind.Daily;
        GameObject dailyBadge, tasksBadge, weeklyBadge;

        Vector3 PanelPos(float x, float y, float z) { return new Vector3(x * PanelUnit, PanelY + y * PanelUnit, z); }
        SpriteRenderer PanelArt(Popup p, string sprite, float x, float y, float w, float h, float z, List<GameObject> owner = null)
        {
            var r = Art(p, "Tasks/" + sprite, PanelPos(x, y, z), w * PanelUnit);
            if (r.sprite) r.transform.localScale = new Vector3(w * PanelUnit / r.sprite.bounds.size.x, h * PanelUnit / r.sprite.bounds.size.y, 1);
            if (owner != null) owner.Add(r.gameObject);
            return r;
        }
        TextMesh[] PanelText(Popup p, string text, float x, float y, float size, float z, List<GameObject> owner = null, bool outlined = true, Color? color = null)
        {
            var labels = outlined ? OutlinedLabel(p, "Panel text", PanelPos(x, y, z), size)
                                  : new[] { Label(p, "Panel text", PanelPos(x, y, z), text, size, color ?? Color.white) };
            SetText(labels, text);
            if (owner != null) owner.Add(labels[0].gameObject);
            return labels;
        }
        void PanelHit(Popup p, float x, float y, float w, float h, string action, List<GameObject> owner = null)
        {
            var hit = Hit(p, PanelPos(x, y, -8.5f), new Vector2(w * PanelUnit, h * PanelUnit), action);
            if (owner != null) owner.Add(hit);
        }
        void PanelPopupBase(Popup p)
        {
            p.RightButton.transform.parent.gameObject.SetActive(false);
            PanelArt(p, "Close_Button", 448.4f, 821.4f, 104, 106, -6.6f);
            PanelHit(p, 448.4f, 821.4f, 160, 160, "close");
        }

        // ---------- Daily rewards ----------
        void BuildRewardsPanel()
        {
            rewardsPanel = Blank("Daily rewards panel");
            PanelArt(rewardsPanel, "Daily Rewards BG", 0, 0, 1147, 1865, -6.2f);
            PanelArt(rewardsPanel, "Icon", -456.5f, 787.5f, 414, 421, -6.55f);
            PanelPopupBase(rewardsPanel);
            PanelArt(rewardsPanel, "Rewards BG", 0, -13, 1076, 1145, -6.3f);
            PanelArt(rewardsPanel, "Timer_Bg", 0, 605.5f, 330, 118, -6.4f);
            rewardsTimer = PanelText(rewardsPanel, "", 45, 601, .034f, -6.5f);
            rewardsInfo = PanelText(rewardsPanel, "", 0, -726, .036f, -6.5f);
        }

        void RefreshRewardsPanel()
        {
            foreach (var o in rewardsDynamic) if (o) Destroy(o);
            rewardsDynamic.Clear();
            bool canClaim = DailyRewards.CanClaim; int next = DailyRewards.NextDay;
            for (int day = 0; day < 7; day++)
            {
                bool seven = day == 6;
                float x = seven ? 0 : (day % 3 - 1) * 340, y = -13 + (seven ? -360 : day < 3 ? 360 : 0);
                float w = seven ? 978 : 313, h = seven ? 324 : 336;
                // Today's claimed cell shows "TODAY" + tick; after day 7 (next wrapped to 0) the whole cycle is done.
                bool doneToday = !canClaim && day == (next + 6) % 7;
                bool claimed = doneToday || (next == 0 ? !canClaim : day < next);
                bool claimable = canClaim && day == next;
                string cell = seven ? (claimable ? "Day_Seven_Active" : "Day_Seven_Deactive")
                                    : claimed || doneToday ? "Reward_Cell_Bg_Pressed" : claimable ? "Reward_Cell_Bg_Active" : "Reward_Cell_Bg_Deactive";
                var bg = PanelArt(rewardsPanel, cell, x, y, w, h, -6.4f, rewardsDynamic);
                if (claimed) bg.color = new Color(.45f, .5f, .6f); // collected cells are greyed out (mockup "TODAY" cell)
                string title = doneToday ? "HÔM NAY" : "NGÀY " + (day + 1);
                PanelText(rewardsPanel, title, x, y + h / 2 - 48, .034f, -6.6f, rewardsDynamic);
                if (claimed) { PanelArt(rewardsPanel, "icon-done", x, y - 30, 130, 110, -6.5f, rewardsDynamic); continue; }
                // Gift contents: icon + amount, side by side.
                var gift = DailyRewards.Days[day];
                var items = new List<KeyValuePair<string, string>>();
                if (gift.Coins > 0) items.Add(new KeyValuePair<string, string>("Shop/icon-coin_1", gift.Coins.ToString()));
                if (gift.Flares > 0) items.Add(new KeyValuePair<string, string>("Tasks/FlareIcon", "x" + gift.Flares));
                if (gift.Swaps > 0) items.Add(new KeyValuePair<string, string>("Tasks/SwapIcon", "x" + gift.Swaps));
                if (gift.Selects > 0) items.Add(new KeyValuePair<string, string>("Tasks/HandIcon", "x" + gift.Selects));
                float step = seven ? 200 : 125;
                for (int i = 0; i < items.Count; i++)
                {
                    float ix = x + (i - (items.Count - 1) / 2f) * step;
                    // Booster icons have wide transparent margins; draw them larger than the coin.
                    var icon = Art(rewardsPanel, items[i].Key, PanelPos(ix, y - 10, -6.5f), (items[i].Key.StartsWith("Shop") ? 95 : 150) * PanelUnit);
                    rewardsDynamic.Add(icon.gameObject);
                    PanelText(rewardsPanel, items[i].Value, ix, y - 95, .03f, -6.6f, rewardsDynamic);
                }
                if (claimable) PanelHit(rewardsPanel, x, y, w, h, "daily-claim", rewardsDynamic);
            }
            SetText(rewardsInfo, canClaim ? "Chạm vào ô quà để nhận!" : "Quà chưa sẵn sàng!\nQuay lại vào ngày mai nhé");
            SetText(rewardsTimer, Clock(DailyRewards.UntilNext));
        }
        static string Clock(System.TimeSpan t) { return string.Format("{0}H {1:00}M", (int)t.TotalHours, t.Minutes); }

        public void OpenDailyRewards() { if (Open) return; Show(rewardsPanel); RefreshRewardsPanel(); }

        // ---------- Daily / weekly tasks ----------
        void BuildTasksPanel()
        {
            tasksPanel = Blank("Tasks panel");
            tasksPanel.RightButton.transform.parent.gameObject.SetActive(false);
        }

        void RefreshTasksPanel()
        {
            foreach (var o in tasksDynamic) if (o) Destroy(o);
            tasksDynamic.Clear();
            var p = tasksPanel; var d = tasksDynamic;
            bool weekly = shownKind == TaskBoard.Kind.Weekly;
            var board = TaskBoard.Board(shownKind);
            int rows = board.ids.Count;
            // Weekly panel art is taller (6 tasks); both share the same top section.
            // The weekly art is taller; it is fitted to the daily panel size so both tabs stay on screen.
            float panelH = 1738, listH = 967;
            float top = 869;                                     // keep the header where the daily panel has it
            float centre = top - panelH / 2;
            PanelArt(p, weekly ? "WeeklyTask-2" : "DAILYTASK-2", 0, centre, 1147, panelH, -6.2f, d);
            PanelArt(p, weekly ? "WeeklyTaskIcon" : "Icon", -456.5f, 724, 414, 421, -6.55f, d);
            PanelArt(p, "Close_Button", 448.4f, 757.9f, 104, 106, -6.6f, d);
            PanelHit(p, 448.4f, 757.9f, 160, 160, "close", d);
            // Star bar with milestone chest.
            int max = TaskBoard.MaxStars(shownKind); float fill = max > 0 ? Mathf.Clamp01((float)board.stars / max) : 0;
            PanelArt(p, weekly ? "WeeklyTask_LoaderBG" : "DailyTask_Slider_BG", 0, 445.8f, 1105, 271, -6.3f, d);
            PanelArt(p, "Frame 5", 0, 445.8f, 996, 123, -6.35f, d);
            PanelArt(p, "Frame 4", 0, 445.8f, 956, 84, -6.4f, d);
            if (fill > 0) PanelArt(p, "Frame 7", -478 + 478 * fill, 445.8f, 956 * fill, 85, -6.45f, d);
            foreach (var m in TaskBoard.MilestonesOf(shownKind))
                if (m.Fraction < 1) PanelArt(p, "Frame 3", -478 + 956 * m.Fraction, 445.8f, 12, 70, -6.47f, d);
            PanelArt(p, "Frame 6", 418, 445.8f, 160, 150, -6.5f, d);
            PanelHit(p, 418, 445.8f, 200, 200, "tasks-claim", d);
            PanelArt(p, "Frame", 313.9f, 529.8f, 142, 108, -6.55f, d);
            PanelText(p, board.stars.ToString(), 290, 535, .03f, -6.6f, d);
            if (TaskBoard.HasUnclaimed(shownKind)) PanelText(p, "Chạm rương để nhận quà!", 0, 352, .03f, -6.6f, d);
            // Task list.
            float listCentre = 254.5f - listH / 2;
            PanelArt(p, weekly ? "WeeklyTaskTaskBG" : "DailyTask_Task_BG", 0, listCentre, 1076, listH, -6.3f, d);
            PanelArt(p, "Timer_Bg", 0, 300.5f, 330, 118, -6.4f, d);
            tasksTimer = PanelText(p, Clock(TaskBoard.Remaining(shownKind)), 45, 296, .034f, -6.5f, d);
            float pitch = (listH - 60) / rows, rowScale = Mathf.Min(1, pitch / 226.75f);
            for (int i = 0; i < rows; i++)
            {
                var task = TaskBoard.Find(board.ids[i]); int progress = board.progress[i]; bool done = progress >= task.Target;
                float y = 254.5f - 30 - pitch * (i + .5f), h = 206 * rowScale;
                PanelArt(p, "DailyTask_TaskBG", 0, y, 955, h, -6.4f, d);
                float iconSize = (task.Icon.EndsWith("Icon") && task.Icon != "UseExtraSlotIcon" ? 165 : 120) * rowScale; // booster icons carry wide margins
                PanelArt(p, task.Icon, -400, y, iconSize, iconSize, -6.45f, d);
                PanelText(p, task.Name, -90, y + 45 * rowScale, .034f * Mathf.Max(.8f, rowScale), -6.5f, d, false, new Color(.1f, .3f, .55f));
                float bx = -90, bw = 560 * (rowScale < 1 ? .95f : 1), bh = 63 * rowScale;
                PanelArt(p, "Frame 13", bx, y - 35 * rowScale, bw, bh, -6.45f, d);
                float f = Mathf.Clamp01((float)progress / task.Target);
                if (f > 0) PanelArt(p, "Frame 12", bx - bw / 2 + bw * f / 2, y - 35 * rowScale, bw * f, bh, -6.47f, d);
                PanelText(p, done ? "XONG" : Short(progress) + "/" + Short(task.Target), bx, y - 37 * rowScale, .03f * Mathf.Max(.8f, rowScale), -6.5f, d, false, new Color(.85f, .3f, .1f));
                // (the original sprite names are swapped: "StarDisableSprite" is the gold star)
                var star = PanelArt(p, "StarDisableSprite", 385, y + 22 * rowScale, 100 * rowScale, 100 * rowScale, -6.45f, d);
                if (done) PanelArt(p, "icon-done", 430, y - 10 * rowScale, 70 * rowScale, 60 * rowScale, -6.48f, d);
                PanelText(p, task.Stars.ToString(), 385, y - 55 * rowScale, .032f * Mathf.Max(.8f, rowScale), -6.5f, d);
            }
            // Daily / weekly tabs under the panel.
            float tabY = top - panelH - 40;
            var dailyTab = PanelArt(p, "DailyTask_Slider_BG", -170, tabY, 330, 110, -6.25f, d);
            var weeklyTab = PanelArt(p, "WeeklyTask_LoaderBG", 170, tabY, 330, 110, -6.25f, d);
            (weekly ? dailyTab : weeklyTab).color = new Color(.75f, .75f, .8f);
            PanelText(p, "NGÀY", -170, tabY, .04f, -6.5f, d);
            PanelText(p, "TUẦN", 170, tabY, .04f, -6.5f, d);
            PanelHit(p, -170, tabY, 330, 120, "tasks-daily", d);
            PanelHit(p, 170, tabY, 330, 120, "tasks-weekly", d);
        }
        void RefreshPopupCoins()
        {
            if (!Open) return;
            foreach (var label in Open.GetComponentsInChildren<TextMesh>(true)) if (label.name == "2040") label.text = Campaign.Coins.ToString();
        }
        static string Short(int n) { return n >= 10000 ? (n / 1000) + "K" : n.ToString(); }

        public void OpenTasks(TaskBoard.Kind kind) { if (Open) return; shownKind = kind; Show(tasksPanel); RefreshTasksPanel(); }

        bool HandlePanels(string action)
        {
            switch (action)
            {
                case "daily-claim": DailyRewards.Claim(); RefreshRewardsPanel(); RefreshPopupCoins(); return true;
                case "tasks-claim": TaskBoard.ClaimMilestones(shownKind); RefreshTasksPanel(); RefreshPopupCoins(); return true;
                case "tasks-daily": shownKind = TaskBoard.Kind.Daily; RefreshTasksPanel(); return true;
                case "tasks-weekly": shownKind = TaskBoard.Kind.Weekly; RefreshTasksPanel(); return true;
            }
            return true;
        }

        void UpdatePanels()
        {
            if (Time.frameCount % 30 == 0)
            {
                if (Open == rewardsPanel.Root) SetText(rewardsTimer, Clock(DailyRewards.UntilNext));
                if (Open == tasksPanel.Root && tasksTimer != null) SetText(tasksTimer, Clock(TaskBoard.Remaining(shownKind)));
                if (dailyBadge) dailyBadge.SetActive(DailyRewards.CanClaim);
                if (tasksBadge) tasksBadge.SetActive(TaskBoard.HasUnclaimed(TaskBoard.Kind.Daily));
                if (weeklyBadge) weeklyBadge.SetActive(TaskBoard.HasUnclaimed(TaskBoard.Kind.Weekly));
            }
        }

        // ---------- Home widgets (original RewardWidget / DailyTaskWidget / WeeklyTaskWidget) ----------
        const float HomeUnit = 10.75f / 2796f;   // main canvas 1292x2796 → world units
        // The current build has these features switched off remotely (recording 2026-10-08: the Home screen shows
        // only the level sign and the Sand Quest chest), so the widgets are hidden unless this is turned on.
        public static bool ShowTaskWidgets = false;
        void BuildHomeWidgets()
        {
            if (!ShowTaskWidgets) return;
            var home = screen.Home.transform;
            dailyBadge = HomeWidget(home, "DailyRewardWidgetActive", 1.78f, -108, "QUÀ", "daily-rewards");
            tasksBadge = HomeWidget(home, "DailyTaskWidgetActive", -1.78f, -108, "NGÀY", "daily-tasks");
            weeklyBadge = HomeWidget(home, "WeeklyTaskWidgetActive", -1.78f, 290, "TUẦN", "weekly-tasks");
        }
        GameObject HomeWidget(Transform home, string sprite, float x, float canvasY, string label, string action)
        {
            var root = new GameObject("Widget - " + action) { layer = home.gameObject.layer };
            root.transform.SetParent(home, false);
            root.transform.position = new Vector3(x, canvasY * HomeUnit, -7f);
            var art = new GameObject(sprite) { layer = root.layer }.AddComponent<SpriteRenderer>();
            art.transform.SetParent(root.transform, false);
            art.sprite = Resources.Load<Sprite>("Tasks/" + sprite);
            if (art.sprite) { float s = 300 * HomeUnit / art.sprite.bounds.size.x; art.transform.localScale = new Vector3(s, s, 1); }
            art.transform.localPosition = new Vector3(0, .1f, 0);
            var text = new GameObject("Label") { layer = root.layer }.AddComponent<TextMesh>();
            text.transform.SetParent(root.transform, false); text.transform.localPosition = new Vector3(0, -.38f, -.1f);
            text.text = label; text.fontSize = 64; text.characterSize = .032f; text.anchor = TextAnchor.MiddleCenter; text.fontStyle = FontStyle.Bold;
            var font = screen.Controller.InterfaceFont; if (font) { text.font = font; text.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            var shadow = Instantiate(text.gameObject, text.transform); shadow.name = "Text outline";
            shadow.transform.localPosition = new Vector3(.03f, -.03f, .05f) / text.transform.lossyScale.x; shadow.transform.localScale = Vector3.one;
            shadow.GetComponent<TextMesh>().color = new Color(.12f, .2f, .4f);
            // Red "!" notification dot (original notf / indicator).
            var badge = new GameObject("Notification") { layer = root.layer };
            badge.transform.SetParent(root.transform, false); badge.transform.localPosition = new Vector3(.42f, .52f, -.15f);
            var dot = new GameObject("circle") { layer = root.layer }.AddComponent<SpriteRenderer>();
            dot.transform.SetParent(badge.transform, false); dot.sprite = Resources.Load<Sprite>("Tasks/circle"); dot.color = new Color(1, .29f, .29f);
            if (dot.sprite) { float s = .3f / dot.sprite.bounds.size.x; dot.transform.localScale = new Vector3(s, s, 1); }
            var mark = new GameObject("indicator") { layer = root.layer }.AddComponent<SpriteRenderer>();
            mark.transform.SetParent(badge.transform, false); mark.transform.localPosition = new Vector3(0, 0, -.05f); mark.sprite = Resources.Load<Sprite>("Tasks/indicator");
            if (mark.sprite) { float s = .22f / mark.sprite.bounds.size.y; mark.transform.localScale = new Vector3(s, s, 1); }
            var hit = new GameObject("Hit area - " + action) { layer = 9 };
            hit.transform.SetParent(root.transform, false);
            hit.AddComponent<BoxCollider>().size = new Vector3(1.1f, 1.25f, .2f);
            hit.AddComponent<VideoUiButton>().Action = action;
            return badge;
        }
    }
}
