using System;
using System.Windows;
using USortThis.Core.ViewModels;

namespace USortThis.UI.Views
{
    public partial class ProfileEditorWindow : Window
    {
        public ProfileEditorWindow()
        {
            InitializeComponent();
            DataContextChanged += ProfileEditorWindow_DataContextChanged;
        }

        public ProfileEditorWindowViewModel Window
        {
            get => (ProfileEditorWindowViewModel)DataContext;
            set => DataContext = value;
        }

        private void ProfileEditorWindow_DataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ProfileEditorWindowViewModel oldVm)
                oldVm.RequestClose -= OnViewModelRequestClose;

            if (e.NewValue is ProfileEditorWindowViewModel newVm)
                newVm.RequestClose += OnViewModelRequestClose;
        }

        private void OnViewModelRequestClose(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() => Close());
        }

        private void List_Rules_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DataContext is ProfileEditorWindowViewModel vm)
            {
                vm.SelectedRule = List_Rules.SelectedItem as USortThis.Core.Models.FilterRuleModel;
            }
        }

        private void List_Criteria_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DataContext is ProfileEditorWindowViewModel vm)
            {
                vm.SelectedCriteria = List_Criteria.SelectedItem as USortThis.Core.ViewModels.FilterRuleCriteriaViewModel;
            }
        }
    }
}