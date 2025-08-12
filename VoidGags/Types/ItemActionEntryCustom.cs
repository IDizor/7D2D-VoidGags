using System;

namespace VoidGags.Types
{
    public class ItemActionEntryCustom : BaseItemActionEntry
    {
        private Action action;

        public ItemActionEntryCustom(XUiController itemController,
            Action action,
            string actionName = "btnBack",
            string spriteName = "ui_game_symbol_arrow_left")
            : base(itemController, actionName, spriteName)
        {
            this.action = action;
        }

        public override void OnActivated()
        {
            action?.Invoke();
            action = null;
        }
    }
}
