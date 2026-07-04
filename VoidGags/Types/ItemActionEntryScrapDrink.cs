namespace VoidGags.Types
{
    public class ItemActionEntryScrapDrink(XUiController _controller) : ItemActionEntryScrap(_controller)
    {
        public override void OnActivated()
        {
            var controller = (XUiC_ItemStack)ItemController;
            controller.LockChangedEvent += ItemStackController_LockChangedEvent;
            ItemStack itemStack = controller.ItemStack.Clone();

            var jar = ItemClass.GetItem("drinkJarEmpty");

            recipe = new Recipe()
            {
                itemValueType = jar.type,
                count = itemStack.count,
                craftExpGain = 0,
                craftingTime = 2 * itemStack.count,
                scrapable = true,
                IsScrap = true,
            };
            recipe.ingredients.Add(new ItemStack(itemStack.itemValue, itemStack.count));

            var wg = ItemController.xui.FindWindowGroupByName("workstation_workbench");
            if (wg == null || !wg.WindowGroup.isShowing)
            {
                wg = ItemController.xui.FindWindowGroupByName("crafting");
            }
            var craftingWindow = wg.GetChildByType<XUiC_CraftingWindowGroup>();

            if (!craftingWindow.AddItemToQueue(recipe, 1))
            {
                warnQueueFull();
                return;
            }

            // clear ItemStack
            controller.ItemStack = ItemStack.Empty.Clone();
            controller.WindowGroup.Controller.SetAllChildrenDirty();
        }
    }
}
