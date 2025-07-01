namespace JP_Dictionary.Services
{
    public class ToastService
    {
        public event Action<string, ToastType>? OnShow;

        public void ShowToast(string message, ToastType type = ToastType.Info)
        {
            OnShow?.Invoke(message, type);
        }

        public void ShowSuccess(string message) => ShowToast(message, ToastType.Success);
        public void ShowWarning(string message) => ShowToast(message, ToastType.Warning);
        public void ShowError(string message) => ShowToast(message, ToastType.Error);
        public void ShowInfo(string message) => ShowToast(message, ToastType.Info);
    }

    public enum ToastType
    {
        Success,
        Warning,
        Error,
        Info
    }
}
