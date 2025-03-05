using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace modbus_ant.Views;

public partial class LogPanelView: UserControl
{
    public LogPanelView()
    {
        InitializeComponent();
    }

    private void MenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        TextBlockLog.Copy();
    }
}