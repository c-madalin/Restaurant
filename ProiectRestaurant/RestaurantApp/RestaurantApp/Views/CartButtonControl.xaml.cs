using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RestaurantApp.Views
{
    public partial class CartButtonControl : UserControl
    {
        public static readonly DependencyProperty ItemCountProperty =
            DependencyProperty.Register("ItemCount", typeof(int), typeof(CartButtonControl), new PropertyMetadata(0));

        public static readonly DependencyProperty CartClickCommandProperty =
            DependencyProperty.Register("CartClickCommand", typeof(ICommand), typeof(CartButtonControl), new PropertyMetadata(null));

        //public int ItemCount
        //{
        //    get { return (int)GetValue(ItemCountProperty); }
        //    set { SetValue(ItemCountProperty, value); }
        //}

        public ICommand CartClickCommand
        {
            get { return (ICommand)GetValue(CartClickCommandProperty); }
            set { SetValue(CartClickCommandProperty, value); }
        }

        //public CartButtonControl()
        //{
        //    InitializeComponent();
        //    DataContext = this;
        //}

        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            if (CartClickCommand != null && CartClickCommand.CanExecute(null))
            {
                CartClickCommand.Execute(null);
            }
        }
    }
}