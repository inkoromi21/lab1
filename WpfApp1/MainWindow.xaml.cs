using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;

namespace WpfApp1;

public partial class MainWindow : Window {
  private const int MaximumInputCount = 100000;
  private const int MaximumBogoElementCount = 11;
  private const int MinimumBogoIterations = 1;
  private const int MaximumBogoIterations = 10000000;
  private const int MaximumAnimatedElementCount = 49;

  private readonly ObservableCollection<NumberRow> _numberRows = new ObservableCollection<NumberRow>();
  private readonly ObservableCollection<SortResult> _sortResults = new ObservableCollection<SortResult>();
  private readonly ChartData _chartData = new ChartData();
  private readonly Random _random = new Random();

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
      string fileText;

      if (fileExtension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) {
        fileText = ReadExcelValues(dialog.FileName);
      } else {
        fileText = File.ReadAllText(dialog.FileName);
      }

      LoadValues(fileText);
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

      LoadValues(fileText);
      StatusText.Text = "Данные загружены из Google Таблицы.";
    } catch (Exception exception) {
      ShowInputError("Не удалось загрузить таблицу. Откройте доступ по ссылке.\n" + exception.Message);
    }
  }

  private async void RunClick(object sender, RoutedEventArgs eventArgs) {
    List<double> sourceValues = ReadGridValues();
    List<SortRequest> selectedAlgorithms = GetSelectedAlgorithms();
    int bogoLimit;

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

    _sortResults.Clear();
    _chartData.Bars.Clear();
    bool isAscending = AscendingRadio.IsChecked == true;

    foreach (SortRequest request in selectedAlgorithms) {
      List<double> sortedValues = new List<double>(sourceValues);
      DateTime startTime = DateTime.Now;
      SortExecution execution = request.SortMethod(sortedValues, isAscending, bogoLimit);
      DateTime endTime = DateTime.Now;
      double elapsedMilliseconds = (endTime - startTime).TotalMilliseconds;

      await AnimateAlgorithm(request.AlgorithmName, sourceValues, sortedValues, execution);
      AddSortResult(request.AlgorithmName, sourceValues.Count, elapsedMilliseconds, execution);
    }

    StatusText.Text = "Сортировка завершена. Самый быстрый: " + GetFastestAlgorithmName() + ".";
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
      row.Value = Math.Round(value, decimalPlaces);
      _numberRows.Add(row);
    }

    StatusText.Text = "Сгенерировано чисел: " + valueCount + ".";
  }

  private List<double> ReadGridValues() {
    List<double> values = new List<double>();

    foreach (NumberRow row in _numberRows) {
      values.Add(row.Value);
    }

    return values;
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
    result.ElapsedMilliseconds = Math.Round(elapsedMilliseconds, 4);
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

  private void LoadValues(string text) {
    string numberTokenPattern = "[;\\s,]+";
    string[] tokens = Regex.Split(text, numberTokenPattern);
    List<double> values = new List<double>();

    foreach (string token in tokens) {
      if (token.Length > 0) {
        values.Add(ParseNumber(token));
      }
    }

    if (values.Count == 0) {
      throw new InvalidOperationException("Числа не найдены.");
    }

    _numberRows.Clear();

    foreach (double value in values) {
      NumberRow row = new NumberRow();
      row.Value = value;
      _numberRows.Add(row);
    }
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

  private static string ReadExcelValues(string filePath) {
    string firstExcelSheetPath = "xl/worksheets/sheet1.xml";
    string excelValuePattern = @"<v>(.*?)</v>";
    using ZipArchive archive = ZipFile.OpenRead(filePath);
    ZipArchiveEntry? sheet = archive.GetEntry(firstExcelSheetPath);

    if (sheet == null) {
      throw new InvalidOperationException("Первый лист Excel не найден.");
    }

    using StreamReader reader = new StreamReader(sheet.Open());
    string xml = reader.ReadToEnd();
    MatchCollection values = Regex.Matches(xml, excelValuePattern);
    List<string> numberTexts = new List<string>();

    foreach (Match value in values) {
      numberTexts.Add(value.Groups[1].Value);
    }

    return string.Join(' ', numberTexts);
  }

  private static double ParseNumber(string text) {
    double value;

    if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)) {
      return value;
    }

    if (double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value)) {
      return value;
    }

    throw new FormatException("Значение «" + text + "» не является числом.");
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
      row.Value = Math.Round(_random.NextDouble() * randomValueRange + randomMinimumValue, decimalPlaces);
      _numberRows.Add(row);
    }
  }
}
