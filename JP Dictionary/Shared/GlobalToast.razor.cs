using JP_Dictionary.Services;
using Microsoft.AspNetCore.Components;

namespace JP_Dictionary.Shared
{
    public partial class GlobalToast
    {
        #region Injections
#nullable disable
        [Inject] public ToastService ToastService { get; set; }
#nullable enable
        #endregion

        private string Message = string.Empty;
        private bool Visible = false;
        private ToastType CurrentType = ToastType.Info;
        private Timer? Timer;

        protected override void OnInitialized()
        {
            ToastService.OnShow += ShowToast;
        }

        private void ShowToast(string message, ToastType type)
        {
            Message = message;
            CurrentType = type;
            Visible = true;

            Timer?.Dispose();
            Timer = new Timer(HideToast, null, 3000, Timeout.Infinite);
            InvokeAsync(StateHasChanged);
        }

        private void HideToast(object? state)
        {
            Visible = false;
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            ToastService.OnShow -= ShowToast;
        }
    }
}
