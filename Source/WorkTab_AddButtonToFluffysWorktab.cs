using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WorkTab;

namespace FavWorks
{
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(MainTabWindow_WorkTab), nameof(MainTabWindow_WorkTab.DoWindowContents))]
    public static class WorkTab_AddButtonToFluffysWorktab
    {
        public static readonly Texture2D _buttonIcon;

        static WorkTab_AddButtonToFluffysWorktab()
        {
            _buttonIcon = ContentFinder<Texture2D>.Get("favwork_worktab_icon");
        }

        [HarmonyPostfix]
        private static void Postfix(Rect rect)
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