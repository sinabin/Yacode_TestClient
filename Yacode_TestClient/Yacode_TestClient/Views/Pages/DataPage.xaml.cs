using Wpf.Ui.Abstractions.Controls;
using Yacode_TestClient.ViewModels.Pages;

namespace Yacode_TestClient.Views.Pages
{
    public partial class DataPage : INavigableView<DataViewModel>
    {
        public DataViewModel ViewModel { get; }

        public DataPage(DataViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
