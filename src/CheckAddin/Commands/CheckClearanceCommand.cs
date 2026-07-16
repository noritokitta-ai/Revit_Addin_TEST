using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CheckAddin.Views;

namespace CheckAddin.Commands;

[Transaction(TransactionMode.ReadOnly)]
[Regeneration(RegenerationOption.Manual)]
public class CheckClearanceCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument? uiDocument = commandData.Application.ActiveUIDocument;
        if (uiDocument?.Document == null)
        {
            message = "有効なRevitドキュメントが開かれていません。";
            return Result.Failed;
        }

        var window = new ClearanceCheckWindow(uiDocument);
        window.ShowDialog();

        return Result.Succeeded;
    }
}
