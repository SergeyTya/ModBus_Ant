using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using modbus_ant.utils;
using Splat;

namespace modbus_ant.ViewModels;

public partial class InputPanelViewModel:ObservableValidator, IEnableLogger
{
    private List<byte> req = [];
    
    private bool _isBusy = false;
    
    [ObservableProperty]
    private SolidColorBrush _textColor = new (Colors.Chartreuse);
    
    [ObservableProperty]
    private bool _isAutoSendChecked = false;
    
    [RelayCommand]
    void  ButtonClick()
    {
        Validate();
        if(!_isBusy) Task.Run(PerformRequest);
    }
    
    [ObservableProperty]
    private string[]? _options;
    
    [ObservableProperty]
    private string[] _subOptions;
    
    [ObservableProperty]
    private string _subOption;
    
    [ObservableProperty]
    private int _optionSelected = -1;
    
    [ObservableProperty]
    //[ValidateUint]
    private string _inputText = "";
    
    [ObservableProperty]
    private bool _isValid;

    private async Task PerformRequest()
    {
        _isBusy = true;
        while (true)
        {
            var transport = IModbusTransport.GetInstance();
            if (transport is null) break;
            
            if (transport.IsOpen)
            {
                Task.Run(async () =>
                {
                    var dtr = await transport?.ExecCustomRequest(req.ToArray(), true)!;
                    return dtr;
                }).ToObservable().Take(1).Subscribe(
                    s => { ;},
                    e => this.Log().Error(e)
                );
                await Task.Delay(300).ConfigureAwait(false);
            }else{
                this.Log().Error("Port is not open ");
                break;
            }

            if (!IsAutoSendChecked) break;
       
        }
        _isBusy = false;
    }

    partial void OnOptionSelectedChanged(int value)
    {
        SubOption = SubOptions[value];
    }

    partial void OnInputTextChanged(string text)
    {
        TextColor= new SolidColorBrush(Colors.Red);
    }

    [RelayCommand]
    private void Validate()
    {
       var tmp = InputText.Split(" ");
       string validRes ="";
       req.Clear();
       foreach (var item in tmp)
       {
           if(item.Length > 2) continue;
    
           var res =
               byte.TryParse(item, System.Globalization.NumberStyles.HexNumber, null, out var result);
           if (res)
           {
               req.Add(result);
               validRes += $"{result:X2} ";
           }
           
       }
       InputText = validRes;
       TextColor= new (Colors.Chartreuse);
    }

    public InputPanelViewModel()
    {
        Options = [
            "0x3 Read Holdings", 
            "0x4 Read Inputs", 
            "0x10 Write holdings"];

        SubOptions = [
            "ID 0x03 ADRhi ADRlo CNThi CNTlo",
            "ID 0x04 ADRhi ADRlo CNThi CNTlo",
            "ID 0x04 ADRhi ADRlo CNThi CNTlo N Val1hi Val1lo ...ValNhi ValNlo "    
            ,];

        OptionSelected = 0;
        InputText = "1 3 0 0 0 1";
        Validate();
    }
}