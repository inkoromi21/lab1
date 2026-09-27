using System.Collections.ObjectModel;

namespace WpfApp1;

public class ChartData {
  public ObservableCollection<BarItem> Bars { get; } = new ObservableCollection<BarItem>();
}
