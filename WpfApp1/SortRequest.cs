namespace WpfApp1;

public class SortRequest {
  public string AlgorithmName { get; }
  public Func<List<double>, bool, int, bool, int, SortExecution> SortMethod { get; }

  public SortRequest(string algorithmName, Func<List<double>, bool, int, bool, int, SortExecution> sortMethod) {
    AlgorithmName = algorithmName;
    SortMethod = sortMethod;
  }
}
