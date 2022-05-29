using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FavWorks
{
    public class Window : Verse.Window
    {
        private static Window? _dialog;
        public static Window Dialog => _dialog ??= new Window();

        private const float ElementHeight = 30f;

        private static List<WorkTypeDef>? _allWorkTypes;
        private static Vector2 _scrollPosition;
        public static WorkTypeDef? CurrentFavWork;

        private string _searchString = String.Empty;
        private float  _curY = 0f;
        private bool _showActiveWorks = false;

        public override Vector2 InitialSize => new(640f, 480f);

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
                yield return new FloatMenuOption(favWorkType.WorkTypeName, () => CurrentFavWork = favWorkType.WorkTypeDef);
            }
        }
        
        public override void DoWindowContents(Rect rect)
        {
            _allWorkTypes ??= InitWorkTypes();

            Text.Font = GameFont.Small;
            GUI.BeginGroup(rect);

            _curY = 0f;
            var favsRect = new Rect(0, _curY, 200, ElementHeight);
            if (Widgets.ButtonText(favsRect, CurrentFavWork == null ? "UI.SelectGroup".Translate().ToString() : Manager.Instance.GetFavWorkName(CurrentFavWork)))
            {
                Find.WindowStack.Add(new FloatMenu(GenFavWorkOptions().ToList()));
            }

            if (CurrentFavWork != null && Manager.Instance.TryGetFavWorkType(CurrentFavWork, out FavWorkType cfg))
            {
                // fav work name
                var nameRect = new Rect(favsRect.xMax + 10, _curY, 200, ElementHeight);
                cfg.WorkTypeName = Widgets.TextField(nameRect, cfg.WorkTypeName);
                if (String.IsNullOrEmpty(cfg.WorkTypeName))
                {
                    Widgets.Label(nameRect, "UI.GroupName".Translate());
                }
                _curY = nameRect.yMax + 5;

                DrawFavWork(cfg, rect.width, rect.height);

                if (Manager.Instance.HasFavWorkTypeChanges())
                {
                    Manager.Instance.ApplyWorks();
                }
            }

            GUI.EndGroup();
        }

        private static List<WorkTypeDef> InitWorkTypes()
        {
            return DefDatabase<WorkTypeDef>.AllDefs
                .Where(workTypeDef => !workTypeDef.IsFavWorkDef())
                .ToList();
        }

        public void DrawFavWork(FavWorkType cfg, float width, float height)
        {
            // fast search box
            var searchRect = new Rect(0, _curY, 200, ElementHeight);
            _searchString = Widgets.TextField(searchRect, _searchString);
            if (String.IsNullOrEmpty(_searchString))
            {
                Widgets.Label(searchRect, "UI.QuickSearch".Translate());
            }

            // show active works
            var showActiveRect = new Rect(searchRect.xMax + 10, _curY, 200, ElementHeight);
            Widgets.CheckboxLabeled(showActiveRect, "UI.ShowActiveWorks".Translate(), ref _showActiveWorks);

            // clear favor works
            var clearRect = new Rect(showActiveRect.xMax + 10, _curY, 150, ElementHeight);
            if (Widgets.ButtonText(clearRect, "UI.ClearGivers".Translate()))
            {
                cfg.ClearWorkGivers();
            }
            _curY = clearRect.yMax + 5;

            _curY += 5;
            Widgets.DrawLineHorizontal(0, _curY, width);
            _curY += 5;

            Rect outRect = new(x: 0f, y: _curY, width: width, height: height - _curY);
            if (_showActiveWorks)
            {
                var givers = _allWorkTypes
                    .SelectMany(workType => workType.workGiversByPriority)
                    .Where(giver => giver.label != null && cfg.ContainsWorkGiver(giver))
                    .ToList();

                Widgets.BeginScrollView(outRect: outRect, scrollPosition: ref _scrollPosition,
                    viewRect: new Rect(x: 0f, y: _curY, width: width - 30f, height: givers.Count * ElementHeight));

                DrawWorkGivers(cfg, givers, width - 30f);

                Widgets.EndScrollView();
            }

            else if (!String.IsNullOrEmpty(_searchString))
            {
                string lowSearchString = _searchString.ToLower();
                var givers = _allWorkTypes
                    .SelectMany(workType => workType.workGiversByPriority)
                    .Where(giver => giver.label?.ToLower().Contains(lowSearchString) ?? false)
                    .ToList();

                Widgets.BeginScrollView(outRect: outRect, scrollPosition: ref _scrollPosition,
                    viewRect: new Rect(x: 0f, y: _curY, width: width - 30f, height: givers.Count * ElementHeight));

                DrawWorkGivers(cfg, givers, width - 30f);

                Widgets.EndScrollView();
            }

            else
            {
                int linesCount = _allWorkTypes.Count +
                                 _allWorkTypes.Sum(workType => workType.workGiversByPriority.Count);

                Widgets.BeginScrollView(outRect: outRect, scrollPosition: ref _scrollPosition,
                    viewRect: new Rect(x: 0f, y: _curY, width: width - 30f, height: linesCount * ElementHeight));

                DrawWorkTypes(cfg, width - 30f);

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