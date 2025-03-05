using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace modbus_ant.Views;

public partial class InputPanelView: UserControl
{
    public InputPanelView()
    {
        InitializeComponent();
    }
    
    void TextBoxKeyUpEvent(object sender, KeyEventArgs e) {

        if (e.Key == Avalonia.Input.Key.Space)
        {
            ((TextBox)sender).SelectAll();
            ((TextBox)sender).SelectionStart = ((TextBox)sender).SelectionEnd;
        }

    }
}