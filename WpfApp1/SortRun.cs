using System.Diagnostics;

namespace WpfApp1;

public class SortRun {
  private readonly SortRequest _request;
  private readonly List<double> _values;
  private readonly bool _isAscending;
  private readonly int _bogoLimit;
  private readonly bool _captureSnapshots;
  private readonly int _randomSeed;

  public double ElapsedMilliseconds { get; private set; }

  public SortRun(
    SortRequest request,
    List<double> values,
    bool isAscending,
    int bogoLimit,
    bool captureSnapshots,
    int randomSeed
  ) {
    _request = request;
    _values = values;
    _isAscending = isAscending;
    _bogoLimit = bogoLimit;
    _captureSnapshots = captureSnapshots;
    _randomSeed = randomSeed;
  }

  public SortExecution Execute() {
    if (_captureSnapshots) {
      return _request.SortMethod(_values, _isAscending, _bogoLimit, true, _randomSeed);
    }

    Stopwatch stopwatch = Stopwatch.StartNew();
    SortExecution execution = _request.SortMethod(_values, _isAscending, _bogoLimit, false, _randomSeed);
    stopwatch.Stop();
    ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

    return execution;
  }
}
