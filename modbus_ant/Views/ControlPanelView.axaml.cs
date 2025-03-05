using System;
using System.IO.Ports;
using Avalonia;
using Avalonia.Controls;
using modbus_ant.ViewModels;


namespace modbus_ant.Views
{
    public partial class ControlPanelView : UserControl
    {
        public ControlPanelView()
        {
            InitializeComponent();
        }

        private void DropOpenEvent(object? sender, EventArgs e)
        {
            ((ControlPanelViewModel) this.DataContext).RTUOptions = SerialPort.GetPortNames();
        }
    }
}
