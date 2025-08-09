namespace ITAdmin.Shared.Models
{
    public class SystemCheckResult
    {
        public int Id { get; set; }
        public DateTime CheckTime { get; set; }
        public bool BluetoothEnabled { get; set; }
        public bool UsbEnabled { get; set; }
        public bool FirewallEnabled { get; set; }
        public string? Notes { get; set; }
    }
}