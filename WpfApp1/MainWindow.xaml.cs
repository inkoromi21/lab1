using Microsoft.Win32;
using Microsoft.VisualBasic.FileIO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace WpfApp1;

public partial class MainWindow : Window {
  private const int MaximumInputCount = 50000;
  private const int MaximumBogoElementCount = 11;
  private const int MinimumBogoIterations = 1;
  private const int MaximumBogoIterations = 10000000;
  private const int MaximumAnimatedElementCount = 49;

  private readonly ObservableCollection<NumberRow> _numberRows = new ObservableCollection<NumberRow>();
  private readonly ObservableCollection<SortResult> _sortResults = new ObservableCollection<SortResult>();
  private readonly ChartData _chartData = new ChartData();
  private readonly Random _random = new Random();
  private bool _isRunning;

  public MainWindow() {
    InitializeComponent();
    NumbersGrid.ItemsSource = _numberRows;
    ResultsGrid.ItemsSource = _sortResults;
    ChartBars.ItemsSource = _chartData.Bars;
    CreateInitialValues();
  }

  private void GenerateClick(object sender, RoutedEventArgs eventArgs) {
    GenerateValues();
  }

  private void ImportFileClick(object sender, RoutedEventArgs eventArgs) {
    OpenFileDialog dialog = new OpenFileDialog();
    dialog.Filter = "Таблицы и текст|*.csv;*.txt;*.xlsx|CSV|*.csv|Excel|*.xlsx";

    if (dialog.ShowDialog() != true) {
      return;
    }

    try {
      string fileExtension = Path.GetExtension(dialog.FileName);
      List<double> values;

      if (fileExtension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) {
        values = ReadExcelValues(dialog.FileName);
      } else if (fileExtension.Equals(".csv", StringComparison.OrdinalIgnoreCase)) {
        string fileText = File.ReadAllText(dialog.FileName);
        values = ReadCsvValues(fileText, DetectCsvDelimiter(fileText));
      } else {
        values = ReadTextValues(File.ReadAllText(dialog.FileName));
      }

      LoadValues(values);
      StatusText.Text = "Данные загружены из файла.";
    } catch (Exception exception) {
      ShowInputError("Не удалось прочитать файл: " + exception.Message);
    }
  }

  private async void ImportGoogleClick(object sender, RoutedEventArgs eventArgs) {
    GoogleUrlWindow dialog = new GoogleUrlWindow();
    dialog.Owner = this;

    if (dialog.ShowDialog() != true) {
      return;
    }

    try {
      string sourceUrl = CreateGoogleCsvUrl(dialog.Url);
      HttpClient httpClient = new HttpClient();
      string fileText = await httpClient.GetStringAsync(sourceUrl);
      httpClient.Dispose();

      LoadValues(ReadCsvValues(fileText, ","));
      StatusText.Text = "Данные загружены из Google Таблицы.";
    } catch (Exception exception) {
      ShowInputError("Не удалось загрузить таблицу. Откройте доступ по ссылке.\n" + exception.Message);
    }
  }

  private async void RunClick(object sender, RoutedEventArgs eventArgs) {
    if (_isRunning) {
      return;
    }

    List<double> sourceValues;
    List<SortRequest> selectedAlgorithms = GetSelectedAlgorithms();
    int bogoLimit;

    if (!TryReadGridValues(out sourceValues)) {
      return;
    }

    if (sourceValues.Count == 0) {
      ShowInputError("Добавьте хотя бы одно число.");
      return;
    }

    if (selectedAlgorithms.Count == 0) {
      ShowInputError("Выберите хотя бы один алгоритм.");
      return;
    }

    if (!TryReadInteger(BogoLimitBox.Text, MinimumBogoIterations, MaximumBogoIterations, "Лимит BOGO", out bogoLimit)) {
      return;
    }

    if (BogoCheck.IsChecked == true && sourceValues.Count > MaximumBogoElementCount) {
      ShowInputError("BOGO-сортировка доступна только для 11 чисел или меньше.");
      return;
    }

    bool isAscending = AscendingRadio.IsChecked == true;
    bool showAnimation = AnimationCheck.IsChecked == true;

    _isRunning = true;
    InputPanel.IsEnabled = false;

    try {
      _sortResults.Clear();
      _chartData.Bars.Clear();

      foreach (SortRequest request in selectedAlgorithms) {
        List<double> sortedValues = new List<double>(sourceValues);
        int randomSeed = _random.Next();
        Stopwatch stopwatch = Stopwatch.StartNew();
        SortExecution execution = request.SortMethod(sortedValues, isAscending, bogoLimit, false, randomSeed);
        stopwatch.Stop();
        double elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        SortExecution animationExecution = execution;

        if (showAnimation && sourceValues.Count <= MaximumAnimatedElementCount) {
          List<double> animationValues = new List<double>(sourceValues);
          animationExecution = request.SortMethod(animationValues, isAscending, bogoLimit, true, randomSeed);
        }

        await AnimateAlgorithm(request.AlgorithmName, sourceValues, sortedValues, animationExecution);
        AddSortResult(request.AlgorithmName, sourceValues.Count, elapsedMilliseconds, execution);
      }

      StatusText.Text = "Сортировка завершена. Быстрее в этом запуске: " + GetFastestAlgorithmName() + ".";
    } catch (Exception exception) {
      StatusText.Text = "Сортировка прервана из-за ошибки.";
      ShowInputError("Не удалось выполнить сортировку: " + exception.Message);
    } finally {
      InputPanel.IsEnabled = true;
      _isRunning = false;
    }
  }

  private void ClearClick(object sender, RoutedEventArgs eventArgs) {
    _numberRows.Clear();
    _sortResults.Clear();
    _chartData.Bars.Clear();
    ChartTitleText.Text = "Выберите алгоритмы и запустите сортировку";
    StatusText.Text = "Данные очищены.";
  }

  private void ChartScrollViewerSizeChanged(object sender, SizeChangedEventArgs eventArgs) {
    UpdateChartBarWidths();
  }

  private void NumberBoxPreviewTextInput(object sender, TextCompositionEventArgs eventArgs) {
    TextBox numberBox = (TextBox)sender;
    string candidate = GetEditedNumber(numberBox, eventArgs.Text);
    eventArgs.Handled = !IsAllowedNumberInput(candidate);
  }

  private void NumberBoxPasting(object sender, DataObjectPastingEventArgs eventArgs) {
    TextBox numberBox = (TextBox)sender;
    string? pastedText = eventArgs.DataObject.GetData(DataFormats.UnicodeText) as string;

    if (pastedText == null) {
      pastedText = eventArgs.DataObject.GetData(DataFormats.Text) as string;
    }

    if (pastedText == null || !IsAllowedNumberInput(GetEditedNumber(numberBox, pastedText))) {
      eventArgs.CancelCommand();
    }
  }

  private static string GetEditedNumber(TextBox numberBox, string newText) {
    string remainingText = numberBox.Text.Remove(numberBox.SelectionStart, numberBox.SelectionLength);
    return remainingText.Insert(numberBox.SelectionStart, newText);
  }

  private static bool IsAllowedNumberInput(string text) {
    return Regex.IsMatch(text, @"\A-?[0-9]*[.,]?[0-9]*\z");
  }

  private void GenerateValues() {
    int valueCount;
    int decimalPlaces = 4;
    double minimumValue;
    double maximumValue;

    if (!TryReadInteger(CountBox.Text, 1, MaximumInputCount, "Количество", out valueCount)) {
      return;
    }

    if (!TryReadNumber(MinBox.Text, "Минимум", out minimumValue)) {
      return;
    }

    if (!TryReadNumber(MaxBox.Text, "Максимум", out maximumValue)) {
      return;
    }

    if (minimumValue >= maximumValue) {
      ShowInputError("Минимальное значение должно быть меньше максимального.");
      return;
    }

    _numberRows.Clear();

    for (int elementIndex = 0; elementIndex < valueCount; ++elementIndex) {
      double value = minimumValue + _random.NextDouble() * (maximumValue - minimumValue);
      NumberRow row = new NumberRow();
      row.ValueText = Math.Round(value, decimalPlaces).ToString(CultureInfo.CurrentCulture);
      _numberRows.Add(row);
    }

    StatusText.Text = "Сгенерировано чисел: " + valueCount + ".";
  }

  private bool TryReadGridValues(out List<double> values) {
    values = new List<double>();

    if (!NumbersGrid.CommitEdit(DataGridEditingUnit.Cell, true) ||
        !NumbersGrid.CommitEdit(DataGridEditingUnit.Row, true)) {
      ShowInputError("Завершите ввод числа в таблице.");
      return false;
    }

    for (int rowIndex = 0; rowIndex < _numberRows.Count; ++rowIndex) {
      string valueText = _numberRows[rowIndex].ValueText;

      if (valueText == "" || valueText == "-" || valueText == "." || valueText == "," ||
          valueText == "-." || valueText == "-,") {
        continue;
      }

      double value;

      if (!IsAllowedNumberInput(valueText) || !TryParseNumber(valueText, out value)) {
        ShowInputError("Строка " + (rowIndex + 1) + ": введите число без букв и посторонних символов.");
        return false;
      }

      values.Add(value);
    }

    return true;
  }

  private List<SortRequest> GetSelectedAlgorithms() {
    List<SortRequest> algorithms = new List<SortRequest>();

    if (BubbleCheck.IsChecked == true) {
      algorithms.Add(new SortRequest("Пузырьковая сортировка", SortingAlgorithms.BubbleSort));
    }

    if (InsertionCheck.IsChecked == true) {
      algorithms.Add(new SortRequest("Сортировка вставками", SortingAlgorithms.InsertionSort));
    }

    if (ShakerCheck.IsChecked == true) {
      algorithms.Add(new SortRequest("Шейкерная сортировка", SortingAlgorithms.ShakerSort));
    }

    if (QuickCheck.IsChecked == true) {
      algorithms.Add(new SortRequest("Быстрая сортировка", SortingAlgorithms.QuickSort));
    }

    if (BogoCheck.IsChecked == true) {
      algorithms.Add(new SortRequest("BOGO-сортировка", SortingAlgorithms.BogoSort));
    }

    return algorithms;
  }

  private void AddSortResult(string algorithmName, int elementCount, double elapsedMilliseconds, SortExecution execution) {
    SortResult result = new SortResult();
    result.AlgorithmName = algorithmName;
    result.ElementCount = elementCount;
    result.ElapsedMilliseconds = elapsedMilliseconds;
    result.IterationCount = execution.IterationCount;
    result.Status = execution.IsCompleted ? "Отсортировано" : "Остановлено по лимиту BOGO";
    _sortResults.Add(result);
  }

  private async Task AnimateAlgorithm(string algorithmName, List<double> sourceValues, List<double> finalValues, SortExecution execution) {
    int animationDelayMilliseconds = 90;
    if (sourceValues.Count > MaximumAnimatedElementCount) {
      _chartData.Bars.Clear();
      ChartTitleText.Text = algorithmName + ": график доступен только для набора меньше 50 чисел.";
      return;
    }

    ChartTitleText.Text = algorithmName + ": исходный массив";
    UpdateBars(sourceValues);

    if (AnimationCheck.IsChecked != true || execution.Snapshots.Count == 0) {
      UpdateBars(finalValues);
      ChartTitleText.Text = algorithmName + ": итоговый массив";
      return;
    }

    foreach (List<double> frame in execution.Snapshots) {
      UpdateBars(frame);
      ChartTitleText.Text = algorithmName + ": выполняется сортировка";
      await Task.Delay(animationDelayMilliseconds);
    }

    UpdateBars(finalValues);

    if (execution.IsCompleted) {
      ChartTitleText.Text = algorithmName + ": сортировка завершена";
    } else {
      ChartTitleText.Text = algorithmName + ": достигнут лимит BOGO";
    }
  }

  private void UpdateBars(List<double> values) {
    double maximumBarHeight = 122.0;
    double minimumBarHeight = 3.0;
    double minimumBarWidth = 4.0;
    string labelNumberFormat = "0.##";
    double minimumValue = values[0];
    double maximumValue = values[0];

    foreach (double value in values) {
      minimumValue = Math.Min(minimumValue, value);
      maximumValue = Math.Max(maximumValue, value);
    }

    double valueRange = maximumValue - minimumValue;
    _chartData.Bars.Clear();

    foreach (double value in values) {
      BarItem bar = new BarItem();
      double height = minimumBarHeight;

      if (valueRange > 0.0) {
        height = minimumBarHeight + (value - minimumValue) / valueRange * (maximumBarHeight - minimumBarHeight);
      }

      bar.Height = height;
      bar.Width = minimumBarWidth;
      bar.Label = value.ToString(labelNumberFormat, CultureInfo.CurrentCulture);
      _chartData.Bars.Add(bar);
    }

    UpdateChartBarWidths();
  }

  private void UpdateChartBarWidths() {
    double minimumBarWidth = 4.0;
    double barHorizontalMargin = 2.0;
    int barCount = _chartData.Bars.Count;
    double chartWidth = ChartScrollViewer.ActualWidth;

    if (barCount == 0 || chartWidth <= 0.0) {
      return;
    }

    double barWidth = chartWidth / barCount - barHorizontalMargin;
    barWidth = Math.Max(minimumBarWidth, barWidth);

    foreach (BarItem bar in _chartData.Bars) {
      bar.Width = barWidth;
    }
  }

  private void LoadValues(List<double> values) {
    if (values.Count == 0) {
      throw new InvalidOperationException("Числа не найдены.");
    }

    _numberRows.Clear();

    foreach (double value in values) {
      NumberRow row = new NumberRow();
      row.ValueText = value.ToString(CultureInfo.CurrentCulture);
      _numberRows.Add(row);
    }
  }

  private static List<double> ReadTextValues(string text) {
    List<double> values = new List<double>();
    using StringReader reader = new StringReader(text);
    string? line;

    while ((line = reader.ReadLine()) != null) {
      string[] fields = Regex.Split(line.Trim(), @"[;\s]+");
      AddNumericRow(fields, values);
    }

    return values;
  }

  private static List<double> ReadCsvValues(string text, string delimiter) {
    List<double> values = new List<double>();
    using StringReader reader = new StringReader(text);
    using TextFieldParser parser = new TextFieldParser(reader);
    parser.SetDelimiters(delimiter);
    parser.HasFieldsEnclosedInQuotes = true;

    while (!parser.EndOfData) {
      string[]? fields = parser.ReadFields();

      if (fields != null) {
        AddNumericRow(fields, values);
      }
    }

    return values;
  }

  private static string DetectCsvDelimiter(string text) {
    using StringReader reader = new StringReader(text);
    string? line;

    while ((line = reader.ReadLine()) != null) {
      if (line.Trim().Length == 0) {
        continue;
      }

      if (line.Contains(';')) {
        return ";";
      }

      if (line.Contains(',')) {
        return ",";
      }
    }

    return ",";
  }

  private static void AddNumericRow(string[] fields, List<double> values) {
    List<double> rowValues = new List<double>();

    foreach (string field in fields) {
      string valueText = field.Trim();

      if (valueText.Length == 0) {
        continue;
      }

      double value;

      if (!TryParseNumber(valueText, out value)) {
        return;
      }

      rowValues.Add(value);
    }

    values.AddRange(rowValues);
  }

  private static string CreateGoogleCsvUrl(string sourceUrl) {
    string googleSheetIdPattern = @"/spreadsheets/d/([\w-]+)";
    string googleCsvUrlFormat = "https://docs.google.com/spreadsheets/d/{0}/export?format=csv";
    Match match = Regex.Match(sourceUrl, googleSheetIdPattern);

    if (match.Success) {
      return string.Format(googleCsvUrlFormat, match.Groups[1].Value);
    }

    return sourceUrl;
  }

  private static List<double> ReadExcelValues(string filePath) {
    string firstExcelSheetPath = "xl/worksheets/sheet1.xml";
    XNamespace sheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    List<double> values = new List<double>();
    using ZipArchive archive = ZipFile.OpenRead(filePath);
    ZipArchiveEntry? sheet = archive.GetEntry(firstExcelSheetPath);

    if (sheet == null) {
      throw new InvalidOperationException("Первый лист Excel не найден.");
    }

    List<string> sharedStrings = ReadSharedStrings(archive, sheetNamespace);
    using Stream sheetStream = sheet.Open();
    XDocument document = XDocument.Load(sheetStream);

    foreach (XElement row in document.Descendants(sheetNamespace + "row")) {
      List<string> fields = new List<string>();

      foreach (XElement cell in row.Elements(sheetNamespace + "c")) {
        fields.Add(ReadExcelCell(cell, sharedStrings, sheetNamespace));
      }

      AddNumericRow(fields.ToArray(), values);
    }

    return values;
  }

  private static List<string> ReadSharedStrings(ZipArchive archive, XNamespace sheetNamespace) {
    List<string> sharedStrings = new List<string>();
    ZipArchiveEntry? entry = archive.GetEntry("xl/sharedStrings.xml");

    if (entry == null) {
      return sharedStrings;
    }

    using Stream stringStream = entry.Open();
    XDocument document = XDocument.Load(stringStream);

    foreach (XElement item in document.Descendants(sheetNamespace + "si")) {
      string text = "";

      foreach (XElement part in item.Descendants(sheetNamespace + "t")) {
        text += part.Value;
      }

      sharedStrings.Add(text);
    }

    return sharedStrings;
  }

  private static string ReadExcelCell(XElement cell, List<string> sharedStrings, XNamespace sheetNamespace) {
    string cellType = (string?)cell.Attribute("t") ?? "";
    string valueText = cell.Element(sheetNamespace + "v")?.Value ?? "";

    if (cellType == "s") {
      int stringIndex;

      if (int.TryParse(valueText, out stringIndex) && stringIndex >= 0 && stringIndex < sharedStrings.Count) {
        return sharedStrings[stringIndex];
      }

      return "Неверное текстовое значение";
    }

    if (cellType == "inlineStr") {
      valueText = "";

      foreach (XElement part in cell.Descendants(sheetNamespace + "t")) {
        valueText += part.Value;
      }
    }

    if (cellType == "b") {
      return valueText == "1" ? "TRUE" : "FALSE";
    }

    return valueText;
  }

  private static double ParseNumber(string text) {
    double value;

    if (TryParseNumber(text, out value)) {
      return value;
    }

    throw new FormatException("Значение «" + text + "» не является числом.");
  }

  private static bool TryParseNumber(string text, out double value) {
    if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) && double.IsFinite(value)) {
      return true;
    }

    return double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
           double.IsFinite(value);
  }

  private bool TryReadNumber(string text, string label, out double value) {
    try {
      value = ParseNumber(text);
      return true;
    } catch (FormatException) {
      value = 0.0;
      ShowInputError("Поле «" + label + "» должно быть числом.");
      return false;
    }
  }

  private bool TryReadInteger(string text, int minimum, int maximum, string label, out int value) {
    if (int.TryParse(text, out value) && value >= minimum && value <= maximum) {
      return true;
    }

    ShowInputError("Поле «" + label + "»: целое число от " + minimum + " до " + maximum + ".");
    return false;
  }

  private void ShowInputError(string message) {
    MessageBox.Show(message, "Некорректные данные", MessageBoxButton.OK, MessageBoxImage.Warning);
  }

  private string GetFastestAlgorithmName() {
    SortResult? fastestResult = null;

    foreach (SortResult result in _sortResults) {
      if (result.Status != "Отсортировано") {
        continue;
      }

      if (fastestResult == null || result.ElapsedMilliseconds < fastestResult.ElapsedMilliseconds) {
        fastestResult = result;
      }
    }

    if (fastestResult == null) {
      return "нет завершённых алгоритмов";
    }

    return fastestResult.AlgorithmName;
  }

  private void CreateInitialValues() {
    int initialValueCount = 12;
    double randomValueRange = 100.0;
    double randomMinimumValue = -50.0;
    int decimalPlaces = 2;

    for (int elementIndex = 0; elementIndex < initialValueCount; ++elementIndex) {
      NumberRow row = new NumberRow();
      row.ValueText = Math.Round(_random.NextDouble() * randomValueRange + randomMinimumValue, decimalPlaces).ToString(CultureInfo.CurrentCulture);
      _numberRows.Add(row);
    }
  }
}
