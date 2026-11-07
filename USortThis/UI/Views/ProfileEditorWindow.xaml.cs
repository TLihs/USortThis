using System;
using System.Windows;
using USortThis.Core.ViewModels;

namespace USortThis.UI.Views
{
    /// <summary>
    /// Interaction logic for ProfileEditorWindow.xaml
    /// </summary>
    public partial class ProfileEditorWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileEditorWindow"/> class.
        /// </summary>
        public ProfileEditorWindow()
        {
            InitializeComponent();
            DataContextChanged += ProfileEditorWindow_DataContextChanged;
        }

        /// <summary>
        /// Gets or sets the ViewModel associated with this window.
        /// </summary>
        public ProfileEditorWindowViewModel Window
        {
            get => (ProfileEditorWindowViewModel)DataContext;
            set => DataContext = value;
        }

        /// <summary>
        /// Handles the DataContextChanged event of the ProfileEditorWindow control.
        /// Unsubscribes the old ViewModel from the RequestClose event and subscribes the new ViewModel to the RequestClose event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event data.</param>
        private void ProfileEditorWindow_DataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ProfileEditorWindowViewModel oldVm)
                oldVm.RequestClose -= OnViewModelRequestClose;

            if (e.NewValue is ProfileEditorWindowViewModel newVm)
                newVm.RequestClose += OnViewModelRequestClose;
        }

        /// <summary>
        /// Closes the window when the ViewModel requests it.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnViewModelRequestClose(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() => Close());
        }

        /// <summary>
        /// Handles the SelectionChanged event of the List_Rules control.
        /// Updates the SelectedRule in the ViewModel when the selection changes.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event data.</param>
        private void List_Rules_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DataContext is ProfileEditorWindowViewModel vm)
            {
                vm.SelectedRule = List_Rules.SelectedItem as USortThis.Core.Models.FilterRuleModel;
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the List_Criteria control.
        /// Updates the SelectedCriteria in the ViewModel when the selection changes.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event data.</param>
        private void List_Criteria_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DataContext is ProfileEditorWindowViewModel vm)
            {
                vm.SelectedCriteria = List_Criteria.SelectedItem as USortThis.Core.ViewModels.FilterRuleCriteriaViewModel;
            }
        }
    }
}