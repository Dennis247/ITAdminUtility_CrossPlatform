namespace ITAdmin.Maui.Services
{
    public interface ISystemChecker
    {
        Task<bool> IsBluetoothEnabledAsync();
        Task<bool> IsUsbEnabledAsync();
        Task<bool> IsFirewallEnabledAsync();
    }
}