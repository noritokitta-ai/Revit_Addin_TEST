using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using CheckAddin.Models;

namespace CheckAddin.Checks;

/// <summary>
/// 電気設備(OST_ElectricalEquipment)の直上に配管(OST_PipeCurves)が
/// 指定距離以内で存在するかを、BoundingBoxによる簡易判定で検出する。
/// </summary>
public class ElectricalPipeClearanceChecker
{
    private readonly Document _document;
    private readonly View _view;

    public ElectricalPipeClearanceChecker(Document document, View view)
    {
        _document = document;
        _view = view;
    }

    /// <summary>直近のRun()で対象ビューから取得できた電気設備の件数</summary>
    public int ElectricalFixtureCount { get; private set; }

    /// <summary>直近のRun()で対象ビューから取得できた配管の件数</summary>
    public int PipeCount { get; private set; }

    /// <param name="maxDistanceFeet">判定距離(内部単位=フィート)</param>
    /// <param name="pipingSystemTypeId">絞り込む配管システムの種類。nullの場合は全システムが対象。</param>
    public IReadOnlyList<ClearanceResult> Run(double maxDistanceFeet, ElementId? pipingSystemTypeId = null)
    {
        var results = new List<ClearanceResult>();

        var electricalFixtures = new FilteredElementCollector(_document, _view.Id)
            .OfCategory(BuiltInCategory.OST_ElectricalEquipment)
            .WhereElementIsNotElementType()
            .ToList();

        var pipes = new FilteredElementCollector(_document, _view.Id)
            .OfCategory(BuiltInCategory.OST_PipeCurves)
            .WhereElementIsNotElementType()
            .Where(pipe => pipingSystemTypeId == null || (pipe as Pipe)?.MEPSystem?.GetTypeId() == pipingSystemTypeId)
            .ToList();

        ElectricalFixtureCount = electricalFixtures.Count;
        PipeCount = pipes.Count;

        if (electricalFixtures.Count == 0 || pipes.Count == 0)
        {
            return results;
        }

        var pipeBoxes = pipes
            .Select(pipe => (Element: pipe, Box: pipe.get_BoundingBox(null)))
            .Where(p => p.Box != null)
            .ToList();

        foreach (var fixture in electricalFixtures)
        {
            var fixtureBox = fixture.get_BoundingBox(null);
            if (fixtureBox == null)
            {
                continue;
            }

            foreach (var (pipe, pipeBox) in pipeBoxes)
            {
                if (!OverlapsInPlan(fixtureBox, pipeBox!))
                {
                    continue;
                }

                // 配管が電気設備の「直上」にある場合のみ対象(下端-上端の差が0以上)
                double verticalDistance = pipeBox!.Min.Z - fixtureBox.Max.Z;
                if (verticalDistance < 0 || verticalDistance > maxDistanceFeet)
                {
                    continue;
                }

                results.Add(BuildResult(fixture, pipe, verticalDistance));
            }
        }

        return results;
    }

    private static bool OverlapsInPlan(BoundingBoxXYZ a, BoundingBoxXYZ b)
    {
        bool xOverlap = a.Min.X <= b.Max.X && a.Max.X >= b.Min.X;
        bool yOverlap = a.Min.Y <= b.Max.Y && a.Max.Y >= b.Min.Y;
        return xOverlap && yOverlap;
    }

    private ClearanceResult BuildResult(Element fixture, Element pipe, double verticalDistanceFeet)
    {
        string systemName = (pipe as Pipe)?.MEPSystem?.Name ?? "-";

        return new ClearanceResult
        {
            ElectricalElementId = fixture.Id,
            ElectricalFamilyName = (fixture as FamilyInstance)?.Symbol?.Family?.Name ?? fixture.Name,
            ElectricalLevel = GetLevelName(fixture),
            PipeElementId = pipe.Id,
            PipeSystemName = systemName,
            PipeLevel = GetLevelName(pipe),
            DistanceFeet = verticalDistanceFeet
        };
    }

    private string GetLevelName(Element element)
    {
        ElementId levelId = element.LevelId;
        if (levelId == ElementId.InvalidElementId)
        {
            return "-";
        }

        return (_document.GetElement(levelId) as Level)?.Name ?? "-";
    }
}
