using HarmonyLib;
using UnityEngine;
using Verse;
using WorkTab;

namespace FavWorks.Patches.WorkTab
{
    /// <summary>
    /// Update columns if WorkTab opened and Add button in to WorkTab
    /// </summary>
    [HarmonyPatch(typeof(MainTabWindow_WorkTab), nameof(MainTabWindow_WorkTab.DoWindowContents))]
    public static class MainTabWindow_WorkTab_DoWindowContents_Patch
    {
        public static readonly Texture2D _buttonIcon;

        static MainTabWindow_WorkTab_DoWindowContents_Patch()
        {
            _buttonIcon = ContentFinder<Texture2D>.Get("favwork_worktab_icon");
        }

        [HarmonyPrefix]
        public static void DoWindowContents(ref bool ____columnsChanged)
        {
            if (Manager.Instance.ColumnsUpdated)
            {
                ____columnsChanged = true;
                Manager.Instance.ColumnsUpdated = false;
            }
        }

        [HarmonyPostfix]
        private static void AddButtonToFluffysWorktab(Rect rect)
        {
            var button = new Rect(rect.x + 190, rect.y + 5, 25, 25);
            var col = Color.white;
            if (Widgets.ButtonImage(button, _buttonIcon, col, col * 0.9f))
            {
                var window = Window.Dialog;
                if (!window.IsOpen)
                {
                    Find.WindowStack.Add(window);
                }
                else
                {
                    window.Close();
                }
            }
        }
    }
}