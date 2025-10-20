using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FaceVerificationTest.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private string errorMessage;

        public BaseViewModel()
        {
        }

        /// <summary>
        /// Hàm tiện ích chạy async task có trạng thái IsBusy
        /// </summary>
        /// <param name="task">Task cần chạy</param>
        /// <param name="errorHandler">Hàm xử lý lỗi</param>
        /// <returns></returns>
        /// <summary>
        /// Hàm tiện ích chạy async task có trạng thái IsBusy
        /// </summary>
        protected async Task RunSafeAsync(Func<Task> task, Func<Exception, Task> errorHandler = null)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                await task();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                if (errorHandler != null)
                    await errorHandler(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Ví dụ Command chung có thể dùng cho ViewModel con
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await RunSafeAsync(async () =>
            {
                // Chỗ này để override trong ViewModel con
                await Task.Delay(1000);
            });
        }
    }
}
