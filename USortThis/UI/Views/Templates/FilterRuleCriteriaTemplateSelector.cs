using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.UI.Views.Templates
{
    public class FilterRuleCriteriaTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? NoneTemplate { get; set; }
        public DataTemplate? ExtensionTemplate { get; set; }
        public DataTemplate? FileNameTemplate { get; set; }
        public DataTemplate? MinDateTemplate { get; set; }
        public DataTemplate? MaxDateTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is FilterRuleCriteriaViewModel vm)
            {
                return vm.Model switch
                {
                    FilterRuleCriteriaModelNone => NoneTemplate!,
                    FilterRuleCriteriaModelExtension => ExtensionTemplate!,
                    FilterRuleCriteriaModelFileName => FileNameTemplate!,
                    FilterRuleCriteriaModelMinDate => MinDateTemplate!,
                    FilterRuleCriteriaModelMaxDate => MaxDateTemplate!,
                    _ => NoneTemplate!
                };
            }

            return base.SelectTemplate(item, container);
        }
    }
}
