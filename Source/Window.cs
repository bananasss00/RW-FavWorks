using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;
using WorkTab;

namespace FavPriorities
{
    public class Window : Verse.Window
    {
        private static List<WorkTypeDef> _allWorkTypes;
        private Vector2 _scrollPosition;
        private string _searchString = String.Empty;
        private WorkTypeDef _currentFavWork;

        public override Vector2 InitialSize { get => new Vector2(640f, 480f); }

        public Window()
        {
            preventCameraMotion = false;
            absorbInputAroundWindow = false;
            draggable = true;
            doCloseX = true;
        }

        private IEnumerable<FloatMenuOption> GenFavWorkOptions()
        {
            foreach (var favWorkType in Manager.Instance.GetAllFavWorkTypes())
            {
                yield return new FloatMenuOption(favWorkType.WorkTypeName, () => _currentFavWork = favWorkType.workTypeDef);
            }
        }
        
        public override void DoWindowContents(Rect rect)
        {
            if (_allWorkTypes == null)
            {
                _allWorkTypes = DefDatabase<WorkTypeDef>.AllDefs
                    .Where(workTypeDef => !workTypeDef.IsFavWorkDef())
                    .ToList();
            }

            GUI.BeginGroup(rect);

            float x = 0, y = 0;

            var favsRect = new Rect(x, y, 200, Text.LineHeight);
            if (Widgets.ButtonText(favsRect, _currentFavWork == null ? "Select FavWork Group" : Manager.Instance.GetFavWorkName(_currentFavWork)))
            {
                Find.WindowStack.Add(new FloatMenu(GenFavWorkOptions().ToList()));
            }

            if (_currentFavWork != null && Manager.Instance.TryGetFavWorkType(_currentFavWork, out FavWorkType cfg))
            {
                // fav work name
                var nameRect = new Rect(favsRect.xMax, y, 200, Text.LineHeight);
                cfg.WorkTypeName = Widgets.TextField(nameRect, cfg.WorkTypeName);
                y = nameRect.yMax + 5;

                DrawFavWork(cfg, x, y, rect.width, rect.height);

                if (Manager.Instance.HasFavWorkTypeChanges())
                {
                    Manager.Instance.ApplyWorks();
                }
            }

            GUI.EndGroup();
        }

        public void DrawFavWork(FavWorkType cfg, float x, float y, float width, float height)
        {
            // fast search box
            var searchRect = new Rect(x, y, 200, Text.LineHeight);
            _searchString = Widgets.TextField(searchRect, _searchString);

            // clear favor works
            var clearRect = new Rect(searchRect.xMax + 10, y, 200, Text.LineHeight);
            if (Widgets.ButtonText(clearRect, "Clear work givers"))
            {
                cfg.ClearWorkGivers();
            }
            y = clearRect.yMax + 5;

            x = 0;
            Widgets.DrawLineHorizontal(x, y, width);
            y += 5;

            if (String.IsNullOrEmpty(_searchString))
            {
                int linesCount = _allWorkTypes.Count +
                                 _allWorkTypes.Sum(workType => workType.workGiversByPriority.Count);
                Rect outRect = new Rect(x: 0f, y: y, width: width, height: height - y);
                Rect viewRect = new Rect(x: 0f, y: y, width: width - 30f, height: linesCount * Text.LineHeight  - y);
                Widgets.BeginScrollView(outRect: outRect, scrollPosition: ref _scrollPosition, viewRect: viewRect);
                foreach (var workType in _allWorkTypes)
                {
                    var workRect = new Rect(x, y, width - 30f, Text.LineHeight);
                    Widgets.TextArea(workRect, workType.gerundLabel);
                    y = workRect.yMax;

                    foreach (var workGiver in workType.workGiversByPriority)
                    {
                        var giverRect = new Rect(x, y, width - 30f, Text.LineHeight);
                        bool active = cfg.ContainsWorkGiver(workGiver), oldState = active;
                        Widgets.CheckboxLabeled(giverRect, workGiver.LabelCap, ref active);
                        if (oldState != active)
                        {
                            if (active) cfg.AddWorkGiver(workGiver);
                            else cfg.RemoveWorkGiver(workGiver);
                        }

                        y = giverRect.yMax;
                    }
                }

                Widgets.EndScrollView();
            }
            else
            {
                string lowSearchString = _searchString.ToLower();
                var givers = _allWorkTypes
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
                    bool active = cfg.ContainsWorkGiver(workGiver), oldState = active;
                    Widgets.CheckboxLabeled(giverRect, workGiver.LabelCap, ref active);
                    if (oldState != active)
                    {
                        if (active) cfg.AddWorkGiver(workGiver);
                        else cfg.RemoveWorkGiver(workGiver);
                    }

                    y = giverRect.yMax;
                }
                Widgets.EndScrollView();
            }
        }
    }
}