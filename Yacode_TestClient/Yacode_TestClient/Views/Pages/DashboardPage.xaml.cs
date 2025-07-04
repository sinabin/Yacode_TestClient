using Wpf.Ui.Abstractions.Controls;
using Yacode_TestClient.ViewModels.Pages;

namespace Yacode_TestClient.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }

    }
}
