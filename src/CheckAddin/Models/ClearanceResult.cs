using Autodesk.Revit.DB;

namespace CheckAddin.Models;

/// <summary>電気設備・配管のNGペア1件分の結果</summary>
public class ClearanceResult
{
    public required ElementId ElectricalElementId { get; init; }
    public required string ElectricalFamilyName { get; init; }
    public required string ElectricalLevel { get; init; }
    public required ElementId PipeElementId { get; init; }
    public required string PipeSystemName { get; init; }
    public required string PipeLevel { get; init; }
    public required double DistanceFeet { get; init; }

    public string ElectricalIdDisplay => ElectricalElementId.Value.ToString();
    public string PipeIdDisplay => PipeElementId.Value.ToString();
    public double DistanceMeters => UnitUtils.ConvertFromInternalUnits(DistanceFeet, UnitTypeId.Meters);
    public string DistanceMetersDisplay => DistanceMeters.ToString("F3");
}
