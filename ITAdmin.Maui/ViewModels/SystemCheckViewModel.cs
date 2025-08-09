using ITAdmin.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITAdmin.Maui.ViewModels
{
    public class SystemCheckViewModel
    {
        public DateTime CheckTime { get; set; }
        public bool BluetoothEnabled { get; set; }
        public bool UsbEnabled { get; set; }
        public bool FirewallEnabled { get; set; }

        public string BluetoothDisplay => BluetoothEnabled ? "ON" : "OFF";
        public string UsbDisplay => UsbEnabled ? "Enabled" : "Disabled";
        public string FirewallDisplay => FirewallEnabled ? "ON" : "OFF";

        public Color BluetoothColor => BluetoothEnabled ? Colors.Green : Colors.Red;
        public Color UsbColor => UsbEnabled ? Colors.Green : Colors.Red;
        public Color FirewallColor => FirewallEnabled ? Colors.Green : Colors.Red;

        public SystemCheckViewModel(SystemCheckResult result)
        {
            CheckTime = result.CheckTime;
            BluetoothEnabled = result.BluetoothEnabled;
            UsbEnabled = result.UsbEnabled;
            FirewallEnabled = result.FirewallEnabled;
        }
    }
}
