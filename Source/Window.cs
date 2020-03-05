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

        public override void ExposeData(){
            base.ExposeData();
            Scribe_Collections.Look( ref Controller.FavWorkTypeDefs, "FavWorkTypeDefs", LookMode.Value, LookMode.Deep );
            Controller.ApplyWorks();
        }

        public Game game;
    }

    public class Window : Verse.Window
    {
        private Vector2 _scrollPosition;
        private string _searchString = String.Empty;
        private WorkTypeDef _currentFavWork;

        public override Vector2 InitialSize { get => new Vector2(640f, 480f); }

        public Window()
        {
            optionalTitle = "FavPriorities.Window";
            preventCameraMotion = false;
            absorbInputAroundWindow = false;
            draggable = true;
            doCloseX = true;
        }

        private IEnumerable<FloatMenuOption> GenFavWorkOptions()
        {
            foreach (var val in Controller.FavWorkTypeDefs.Values)
            {
                yield return new FloatMenuOption(val.favWorkType.labelShort, () => _currentFavWork = val.favWorkType);
            }
        }
        
        public override void DoWindowContents(Rect rect)
        {
            GUI.BeginGroup(rect);

            float x = 0, y = 0;

            var favsRect = new Rect(x, y, 200, Text.LineHeight);
            if (Widgets.ButtonText(favsRect, _currentFavWork == null ? "Select FavWork Group" : _currentFavWork.labelShort))
            {
                Find.WindowStack.Add(new FloatMenu(GenFavWorkOptions().ToList()));
            }
            y = favsRect.yMax;

            if (_currentFavWork != null)
                DrawFavWork(x, y, rect.width, rect.height);
            
            GUI.EndGroup();
        }

        public void DrawFavWork(float x, float y, float width, float height)
        {
            var cfg = Controller.FavWorkTypeDefs[_currentFavWork.defName];

            // fav work name
            var nameRect = new Rect(x, y, 200, Text.LineHeight);
            cfg.name = Widgets.TextField(nameRect, cfg.name);
            y = nameRect.yMax + 5;
            // fast search box
            var searchRect = new Rect(x, y, 200, Text.LineHeight);
            _searchString = Widgets.TextField(searchRect, _searchString);
            // clear favor works
            var clearRect = new Rect(searchRect.xMax + 10, y, 200, Text.LineHeight);
            if (Widgets.ButtonText(clearRect, "Clear fav works"))
            {
                cfg.FavWorks.Clear();
                Controller.ApplyWorks();
            }
            y = clearRect.yMax + 5;

            x = 0;
            Widgets.DrawLineHorizontal(x, y, width);
            y += 5;

            
            bool favWorksChanged = false;

            if (String.IsNullOrEmpty(_searchString))
            {
                int linesCount = Controller.AllWorkTypes.Count +
                                 Controller.AllWorkTypes.Sum(workType => workType.workGiversByPriority.Count);
                Rect outRect = new Rect(x: 0f, y: y, width: width, height: height - y);
                Rect viewRect = new Rect(x: 0f, y: y, width: width - 30f, height: linesCount * Text.LineHeight  - y);
                Widgets.BeginScrollView(outRect: outRect, scrollPosition: ref _scrollPosition, viewRect: viewRect);
                foreach (var workType in Controller.AllWorkTypes)
                {
                    var workRect = new Rect(x, y, width - 30f, Text.LineHeight);
                    Widgets.TextArea(workRect, workType.gerundLabel);
                    y = workRect.yMax;

                    foreach (var workGiver in workType.workGiversByPriority)
                    {
                        var giverRect = new Rect(x, y, width - 30f, Text.LineHeight);
                        bool active = cfg.FavWorks.Contains(workGiver), oldState = active;
                        Widgets.CheckboxLabeled(giverRect, workGiver.LabelCap, ref active);
                        if (oldState != active)
                        {
                            if (active)
                            {
                                cfg.FavWorks.Add(workGiver);
                                Log.Warning($"enabled {workGiver.label}");
                            }
                            else
                                cfg.FavWorks.Remove(workGiver);

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
                    .Where(giver => giver.label?.ToLower().Contains(lowSearchString) ?? false)
                    .ToList();

                int linesCount = givers.Count;
                Rect outRect = new Rect(x: 0f, y: y, width: width, height: height - y);
                Rect viewRect = new Rect(x: 0f, y: y, width: width - 30f, height: linesCount * Text.LineHeight - y);
                Widgets.BeginScrollView(outRect: outRect, scrollPosition: ref _scrollPosition, viewRect: viewRect);
                foreach (var workGiver in givers)
                {
                    var giverRect = new Rect(x, y, width - 30f, Text.LineHeight);
                    bool active = cfg.FavWorks.Contains(workGiver), oldState = active;
                    Widgets.CheckboxLabeled(giverRect, workGiver.LabelCap, ref active);
                    if (oldState != active)
                    {
                        if (active)
                            cfg.FavWorks.Add(workGiver);
                        else
                            cfg.FavWorks.Remove(workGiver);

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
        }
    }
}