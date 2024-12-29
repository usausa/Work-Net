using MauiSampleNet9.Models;
using MauiSampleNet9.PageModels;

namespace MauiSampleNet9.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}