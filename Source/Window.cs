using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FavWorks
{
    public class Window : Verse.Window
    {
        private const float ElementHeight = 30f;

        private static List<WorkTypeDef> _allWorkTypes;
        private static WorkTypeDef _currentFavWork;
        private static Vector2 _scrollPosition;

        private string _searchString = String.Empty;
        private float  _curY = 0f;

        public override Vector2 InitialSize => new Vector2(640f, 480f);

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
                yield return new FloatMenuOption(favWorkType.WorkTypeName, () => _currentFavWork = favWorkType.WorkTypeDef);
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

            Text.Font = GameFont.Small;
            GUI.BeginGroup(rect);

            _curY = 0f;
            var favsRect = new Rect(0, _curY, 200, ElementHeight);
            if (Widgets.ButtonText(favsRect, _currentFavWork == null ? "Select FavWork Group" : Manager.Instance.GetFavWorkName(_currentFavWork)))
            {
                Find.WindowStack.Add(new FloatMenu(GenFavWorkOptions().ToList()));
            }

            if (_currentFavWork != null && Manager.Instance.TryGetFavWorkType(_currentFavWork, out FavWorkType cfg))
            {
                // fav work name
                var nameRect = new Rect(favsRect.xMax + 10, _curY, 200, ElementHeight);
                cfg.WorkTypeName = Widgets.TextField(nameRect, cfg.WorkTypeName);
                _curY = nameRect.yMax + 5;

                DrawFavWork(cfg, rect.width, rect.height);

                if (Manager.Instance.HasFavWorkTypeChanges())
                {
                    Manager.Instance.ApplyWorks();
                }
            }

            GUI.EndGroup();
        }

        public void DrawFavWork(FavWorkType cfg, float width, float height)
        {
            // fast search box
            var searchRect = new Rect(0, _curY, 200, ElementHeight);
            _searchString = Widgets.TextField(searchRect, _searchString);

            // clear favor works
            var clearRect = new Rect(searchRect.xMax + 10, _curY, 200, ElementHeight);
            if (Widgets.ButtonText(clearRect, "Clear work givers"))
            {
                cfg.ClearWorkGivers();
            }
            _curY = clearRect.yMax + 5;

            _curY += 5;
            Widgets.DrawLineHorizontal(0, _curY, width);
            _curY += 5;

            Rect outRect = new Rect(x: 0f, y: _curY, width: width, height: height - _curY);
            if (String.IsNullOrEmpty(_searchString))
            {
                int linesCount = _allWorkTypes.Count +
                                 _allWorkTypes.Sum(workType => workType.workGiversByPriority.Count);

                Widgets.BeginScrollView(outRect: outRect, scrollPosition: ref _scrollPosition, 
                    viewRect: new Rect(x: 0f, y: _curY, width: width - 30f, height: linesCount * ElementHeight));

                DrawWorkTypes(cfg, width - 30f);

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

                Widgets.BeginScrollView(outRect: outRect, scrollPosition: ref _scrollPosition,
                    viewRect: new Rect(x: 0f, y: _curY, width: width - 30f, height: linesCount * ElementHeight));

                DrawWorkGivers(cfg, givers, width - 30f);

                Widgets.EndScrollView();
            }
        }

        private void DrawWorkTypes(FavWorkType cfg, float width)
        {
            foreach (var workType in _allWorkTypes)
            {
                Color backupColor = GUI.color;
                GUI.color = Color.yellow;
                var workRect = new Rect(0, _curY, width, ElementHeight);
                Widgets.Label(workRect, workType.gerundLabel);
                _curY = workRect.yMax;
                GUI.color = backupColor;

                DrawWorkGivers(cfg, workType.workGiversByPriority, width);
            }
        }

        private void DrawWorkGivers(FavWorkType cfg, List<RimWorld.WorkGiverDef> givers, float width)
        {
            foreach (var workGiver in givers)
            {
                bool active = cfg.ContainsWorkGiver(workGiver), oldState = active;
                var giverRect = new Rect(0, _curY, width, ElementHeight);
                Widgets.CheckboxLabeled(giverRect, workGiver.LabelCap, ref active);
                if (oldState != active)
                {
                    if (active) cfg.AddWorkGiver(workGiver);
                    else        cfg.RemoveWorkGiver(workGiver);
                }
                _curY = giverRect.yMax;
            }
        }
    }
}