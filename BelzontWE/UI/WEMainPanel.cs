using Game.UI.InGame;

namespace BelzontWE.UI
{
    public class WEMainPanel : TabbedGamePanel
    {
        public override bool blocking => true;

        public override LayoutPosition position => LayoutPosition.Center;
        public override bool retainProperties => true;
    }
}
