using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FavPriorities
{
    [DefOf]
    public static class FavPrioritiesDefOf
    {
        public static KeyBindingDef FavPrioritiesWindowOpen;
    }

    public class FavComponent : GameComponent
    {
        public FavComponent() { }
        public FavComponent(Game game) { this.game = game; }

        public override void GameComponentOnGUI()
        {
            if (FavPrioritiesDefOf.FavPrioritiesWindowOpen != null && FavPrioritiesDefOf.FavPrioritiesWindowOpen.IsDownEvent)
            {
                if (Find.WindowStack.Windows.Count(window => window is Window) <= 0)
                {
                    Find.WindowStack.Add(new Window());
                }
            }
        }

        public Game game;
    }

    public class Window : Verse.Window
    {
        private Vector2 _scrollPosition;
        private string _searchString = String.Empty;

        public override Vector2 InitialSize { get => new Vector2(640f, 480f); }

        public Window()
        {
            optionalTitle = "FavPriorities.Window";
            preventCameraMotion = false;
            absorbInputAroundWindow = false;
            draggable = true;
            doCloseX = true;
        }
        
        public override void DoWindowContents(Rect rect)
        {
            GUI.BeginGroup(rect);

            float x = 0, y = 0;

            // fast search box
            var searchRect = new Rect(x, y, 200, Text.LineHeight);
            _searchString = Widgets.TextField(searchRect, _searchString);
            // clear favor works
            var clearRect = new Rect(searchRect.xMax + 10, y, 200, Text.LineHeight);
            if (Widgets.ButtonText(clearRect, "Clear fav works"))
            {
                Controller.FavWorks.Clear();
                Controller.ApplyWorks();
            }
            y = clearRect.yMax + 5;

            x = 0;
            Widgets.DrawLineHorizontal(x, y, rect.width);
            y += 5;

            
            bool favWorksChanged = false;

            if (String.IsNullOrEmpty(_searchString))
            {
                int linesCount = Controller.AllWorkTypes.Count +
                                 Controller.AllWorkTypes.Sum(workType => workType.workGiversByPriority.Count);
                Rect outRect = new Rect(x: 0f, y: y, width: rect.width, height: rect.height - y);
                Rect viewRect = new Rect(x: 0f, y: y, width: rect.width - 30f, height: linesCount * Text.LineHeight  - y);
                Widgets.BeginScrollView(outRect: outRect, scrollPosition: ref _scrollPosition, viewRect: viewRect);
                foreach (var workType in Controller.AllWorkTypes)
                {
                    var workRect = new Rect(x, y, rect.width - 30f, Text.LineHeight);
                    Widgets.TextArea(workRect, workType.gerundLabel);
                    y = workRect.yMax;

                    foreach (var workGiver in workType.workGiversByPriority)
                    {
                        var giverRect = new Rect(x, y, rect.width - 30f, Text.LineHeight);
                        bool active = Controller.FavWorks.Contains(workGiver), oldState = active;
                        Widgets.CheckboxLabeled(giverRect, workGiver.LabelCap, ref active);
                        if (oldState != active)
                        {
                            if (active)
                                Controller.FavWorks.Add(workGiver);
                            else
                                Controller.FavWorks.Remove(workGiver);

                            favWorksChanged = true;
                        }

                        y = giverRect.yMax;
                    }
                }

                Widgets.EndScrollView();
            }
            else
            {
                string lowSearchString = _searchString.ToLower();
                var givers = Controller.AllWorkTypes
                    .SelectMany(workType => workType.workGiversByPriority)
                    .Where(giver => giver.label.ToLower().Contains(lowSearchString))
                    .ToList();

                int linesCount = givers.Count;
                Rect outRect = new Rect(x: 0f, y: y, width: rect.width - y, height: rect.height);
                Rect viewRect = new Rect(x: 0f, y: y, width: rect.width - y - 30f, height: linesCount * Text.LineHeight);
                Widgets.BeginScrollView(outRect: outRect, scrollPosition: ref _scrollPosition, viewRect: viewRect);
                foreach (var workGiver in givers)
                {
                    var giverRect = new Rect(x, y, rect.width - 30f, Text.LineHeight);
                    bool active = Controller.FavWorks.Contains(workGiver), oldState = active;
                    Widgets.CheckboxLabeled(giverRect, workGiver.LabelCap, ref active);
                    if (oldState != active)
                    {
                        if (active)
                            Controller.FavWorks.Add(workGiver);
                        else
                            Controller.FavWorks.Remove(workGiver);

                        favWorksChanged = true;
                    }

                    y = giverRect.yMax;
                }
                Widgets.EndScrollView();
            }

            if (favWorksChanged)
            {
                Controller.ApplyWorks();
            }
            
            GUI.EndGroup();
        }
    }
}