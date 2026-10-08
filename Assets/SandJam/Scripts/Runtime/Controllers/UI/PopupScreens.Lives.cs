using UnityEngine;

namespace SandJamTest.Scene3D
{
    // "NO MORE LIVES!" popup rebuilt from the original canvas_nolives (MainMenu scene, 1080x1920 canvas):
    // big frame with the title art, a glowing heart showing the hearts left, "Time to next life" with the
    // watch pill, and the purple Refill button (EconomyConfig.priceRefillHealth, 600 in the current build).
    // The original IAP bundle under the popup is left out (no payments in the clone).
    public sealed partial class PopupScreens
    {
        Popup noLives;
        TextMesh[] noLivesCount, noLivesTimer, noLivesInfo;
        Transform noLivesShine;
        const float LivesUnit = CanvasUnit * .9f;               // the 1142 px frame is wider than the canvas
        static readonly Vector2 LivesCentre = new Vector2(0, 366);

        // Image stretched to its RectTransform size, positioned relative to the popup frame centre.
        SpriteRenderer Ui(Popup p, string sprite, Vector2 centre, float unit, float x, float y, float w, float h, float z)
        {
            var r = Art(p, sprite, new Vector3((centre.x + x) * unit, (centre.y + y) * unit, z), w * unit);
            if (r.sprite) r.transform.localScale = new Vector3(w * unit / r.sprite.bounds.size.x, h * unit / r.sprite.bounds.size.y, 1);
            return r;
        }
        Vector3 LivesPos(float x, float y, float z) { return new Vector3((LivesCentre.x + x) * LivesUnit, (LivesCentre.y + y) * LivesUnit, z); }

        void BuildNoLives()
        {
            noLives = Blank("No more lives popup");
            noLives.RightButton.transform.parent.gameObject.SetActive(false);
            Ui(noLives, "Lives/bg-popUp-noMoreLives", LivesCentre, LivesUnit, 0, 0, 1142.4f, 1321.2f, -6.2f);
            Ui(noLives, "Lives/text-noMoreLives", LivesCentre, LivesUnit, 0, 505.6f, 899.4f, 112.6f, -6.5f);
            Ui(noLives, "Lives/bg-popUp-slot-noMoreLives", LivesCentre, LivesUnit, 0, 15.1f, 902.1f, 669.8f, -6.3f);
            noLivesShine = Ui(noLives, "Lives/light", LivesCentre, LivesUnit, 0, 118, 387, 387, -6.4f).transform;
            Ui(noLives, "Lives/icon-heard-big-enabled", LivesCentre, LivesUnit, 0, 118, 287, 262, -6.5f);
            noLivesCount = OutlinedLabel(noLives, "Hearts left", LivesPos(0, 126, -6.7f), .09f);
            SetText(OutlinedLabel(noLives, "Next life caption", LivesPos(0, -99, -6.6f), .042f), "Thời gian đến tim tiếp theo");
            Ui(noLives, "Lives/bg-watch", LivesCentre, LivesUnit, 0, -216, 420, 102, -6.4f);
            Ui(noLives, "Lives/icon-watch", LivesCentre, LivesUnit, -164.7f, -211.4f, 95, 117, -6.5f);
            noLivesTimer = OutlinedLabel(noLives, "Next life timer", LivesPos(25.9f, -216, -6.6f), .042f);
            Ui(noLives, "Lives/button-purple", LivesCentre, LivesUnit, 5, -455, 596.3f, 213.3f, -6.4f);
            Ui(noLives, "Shop/icon-coin_1", LivesCentre, LivesUnit, -190, -446, 90, 90, -6.5f);
            SetText(OutlinedLabel(noLives, "Refill label", LivesPos(30, -446, -6.6f), .048f), EconomyManager.RefillLivesPrice + " Hồi tim");
            Hit(noLives, LivesPos(5, -455, -8.5f), new Vector2(596.3f * LivesUnit, 213.3f * LivesUnit), "refill-lives");
            noLivesInfo = OutlinedLabel(noLives, "Refill info", LivesPos(0, -600, -6.6f), .034f);
            // Red exit button anchored to the frame's top-right corner.
            // (pulled slightly inside the frame: on narrow screens the corner would sit on the coin counter)
            Ui(noLives, "Lives/button-exit", LivesCentre, LivesUnit, 480, 600, 100, 116, -6.6f);
            Hit(noLives, LivesPos(480, 600, -8.5f), new Vector2(.6f, .6f), "close-failed");
        }

        void ShowNoLivesPopup()
        {
            failedIsNoLives = true;
            SetText(noLivesInfo, "");
            RefreshNoLives();
            Show(noLives);
        }
        void RefreshNoLives()
        {
            SetText(noLivesCount, LivesManager.Lives.ToString());
            SetText(noLivesTimer, LivesManager.IsFull ? "Đầy" : LivesManager.Countdown(LivesManager.UntilNext));
        }
        void UpdateNoLives()
        {
            if (Open != noLives.Root) return;
            if (noLivesShine) noLivesShine.localRotation = Quaternion.Euler(0, 0, Time.unscaledTime * 20f);
            if (Time.frameCount % 15 == 0) RefreshNoLives();
        }
    }
}
