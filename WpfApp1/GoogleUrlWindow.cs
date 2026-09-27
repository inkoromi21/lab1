using System.Windows;
using System.Windows.Controls;

namespace WpfApp1;

public class GoogleUrlWindow : Window {
  private readonly TextBox _urlTextBox = new TextBox();

  public string Url {
    get { return _urlTextBox.Text; }
  }

  public GoogleUrlWindow() {
    double windowWidth = 520.0;
    double windowHeight = 150.0;
    double urlTextBoxMinimumWidth = 420.0;
    double urlTextBoxMargin = 8.0;
    double panelMargin = 12.0;

    Title = "Google Таблица";
    Width = windowWidth;
    Height = windowHeight;
    WindowStartupLocation = WindowStartupLocation.CenterOwner;

    _urlTextBox.MinWidth = urlTextBoxMinimumWidth;
    _urlTextBox.Margin = new Thickness(urlTextBoxMargin);

    TextBlock instruction = new TextBlock();
    instruction.Text = "Вставьте ссылку на Google Таблицу с открытым доступом:";

    Button importButton = new Button();
    importButton.Content = "Загрузить";
    importButton.IsDefault = true;
    importButton.HorizontalAlignment = HorizontalAlignment.Right;
    importButton.Click += ImportButtonClick;

    StackPanel panel = new StackPanel();
    panel.Margin = new Thickness(panelMargin);
    panel.Children.Add(instruction);
    panel.Children.Add(_urlTextBox);
    panel.Children.Add(importButton);
    Content = panel;
  }

  private void ImportButtonClick(object sender, RoutedEventArgs eventArgs) {
    if (string.IsNullOrWhiteSpace(_urlTextBox.Text)) {
      MessageBox.Show("Введите ссылку.", "Google Таблица", MessageBoxButton.OK, MessageBoxImage.Warning);
      return;
    }
    DialogResult = true;
  }
}
