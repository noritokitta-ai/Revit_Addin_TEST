using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CheckAddin.Checks;
using CheckAddin.Models;

namespace CheckAddin.Views;

public partial class ClearanceCheckWindow : Window
{
    private readonly UIDocument _uiDocument;
    private List<ClearanceResult> _results = new();

    private sealed record PipingSystemTypeItem(string Name, ElementId? Id);

    public ClearanceCheckWindow(UIDocument uiDocument)
    {
        InitializeComponent();
        _uiDocument = uiDocument;
        LoadPipingSystemTypes();
    }

    private void LoadPipingSystemTypes()
    {
        var systemTypes = new FilteredElementCollector(_uiDocument.Document)
            .OfClass(typeof(PipingSystemType))
            .Cast<PipingSystemType>()
            .OrderBy(t => t.Name)
            .Select(t => new PipingSystemTypeItem(t.Name, t.Id))
            .ToList();

        systemTypes.Insert(0, new PipingSystemTypeItem("(すべて)", null));

        PipingSystemTypeComboBox.ItemsSource = systemTypes;
        PipingSystemTypeComboBox.SelectedIndex = 0;
    }

    private void RunButton_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(DistanceTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out double distanceMeters)
            || distanceMeters <= 0)
        {
            ShowStatus("判定距離には0より大きい数値を入力してください。", isError: true);
            return;
        }

        Document document = _uiDocument.Document;
        View? activeView = document.ActiveView;

        if (!IsSupportedView(activeView))
        {
            ShowStatus("この画面ではチェックできません。平面図・3Dビュー・断面図などを開いた状態で実行してください。", isError: true);
            return;
        }

        double distanceFeet = UnitUtils.ConvertToInternalUnits(distanceMeters, UnitTypeId.Meters);
        ElementId? pipingSystemTypeId = (PipingSystemTypeComboBox.SelectedItem as PipingSystemTypeItem)?.Id;

        var checker = new ElectricalPipeClearanceChecker(document, activeView!);

        try
        {
            _results = checker.Run(distanceFeet, pipingSystemTypeId).ToList();
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException)
        {
            ShowStatus("この画面ではチェックできません。平面図・3Dビュー・断面図などを開いた状態で実行してください。", isError: true);
            return;
        }

        ResultsDataGrid.ItemsSource = _results;
        ShowStatus(BuildResultMessage(checker), isError: false);
    }

    private static bool IsSupportedView(View? view)
    {
        if (view == null || view.IsTemplate)
        {
            return false;
        }

        return view.ViewType switch
        {
            ViewType.Schedule or ViewType.DrawingSheet or ViewType.Legend
                or ViewType.Internal or ViewType.ProjectBrowser or ViewType.SystemBrowser
                or ViewType.Undefined => false,
            _ => true
        };
    }

    private string BuildResultMessage(ElectricalPipeClearanceChecker checker)
    {
        if (checker.ElectricalFixtureCount == 0 && checker.PipeCount == 0)
        {
            return "対象ビューに電気設備(OST_ElectricalEquipment)・配管(OST_PipeCurves)のいずれも見つかりませんでした。";
        }

        if (checker.ElectricalFixtureCount == 0)
        {
            return "対象ビューに電気設備(OST_ElectricalEquipment)が見つかりませんでした。";
        }

        if (checker.PipeCount == 0)
        {
            return "対象ビューに配管(OST_PipeCurves)が見つかりませんでした。";
        }

        return _results.Count == 0
            ? "NGとなる組み合わせは見つかりませんでした。"
            : $"{_results.Count} 件のNGが見つかりました。行をダブルクリックすると該当要素を選択・ズームします。";
    }

    private void ResultsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ResultsDataGrid.SelectedItem is not ClearanceResult result)
        {
            return;
        }

        var ids = new List<ElementId> { result.ElectricalElementId, result.PipeElementId };
        _uiDocument.Selection.SetElementIds(ids);
        _uiDocument.ShowElements(ids);
    }

    private void ShowStatus(string text, bool isError)
    {
        StatusTextBlock.Text = text;
        StatusTextBlock.Foreground = isError ? Brushes.Red : Brushes.Black;
    }
}
