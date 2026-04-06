using StardewModdingAPI;
using StardewValley.Menus;

namespace Playable_Piano.UI
{
    internal abstract class BaseUI : IClickableMenu
    {
        protected abstract PlayablePiano mainMod { get; set; }
        public abstract void handleButton(SButton button);
    }
}