namespace WpfApp1;

public class SortExecution {
  private const int MaximumSnapshotElementCount = 49;
  private const int MaximumSnapshotCount = 50;

  public bool IsCompleted { get; set; } = true;
  public long IterationCount { get; set; }
  public List<List<double>> Snapshots { get; } = new List<List<double>>();

  public void SaveSnapshot(List<double> values) {
    if (values.Count <= MaximumSnapshotElementCount && Snapshots.Count < MaximumSnapshotCount) {
      Snapshots.Add(new List<double>(values));
    }
  }
}
