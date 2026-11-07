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
    /// <summary>
    /// Selects the appropriate DataTemplate for a given filter rule criterion model.
    /// </summary>
    public class FilterRuleCriteriaTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Gets or sets the template used when no specific template is defined for the criterion model.
        /// </summary>
        public DataTemplate? NoneTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template used for criterion models of type <see cref="FilterRuleCriteriaModelExtension"/>.
        /// </summary>
        public DataTemplate? ExtensionTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template used for criterion models of type <see cref="FilterRuleCriteriaModelFileName"/>.
        /// </summary>
        public DataTemplate? FileNameTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template used for criterion models of type <see cref="FilterRuleCriteriaModelMinDate"/>.
        /// </summary>
        public DataTemplate? MinDateTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template used for criterion models of type <see cref="FilterRuleCriteriaModelMaxDate"/>.
        /// </summary>
        public DataTemplate? MaxDateTemplate { get; set; }

        /// <summary>
        /// Selects a DataTemplate based on the criterion model type.
        ///
        /// <para>
        /// The selection is based on the following mappings:
        /// - <see cref="FilterRuleCriteriaModelNone"/>: NoneTemplate
        /// - <see cref="FilterRuleCriteriaModelExtension"/>: ExtensionTemplate
        /// - <see cref="FilterRuleCriteriaModelFileName"/>: FileNameTemplate
        /// - <see cref="FilterRuleCriteriaModelMinDate"/>: MinDateTemplate
        /// - <see cref="FilterRuleCriteriaModelMaxDate"/>: MaxDateTemplate
        ///
        /// If the criterion model type does not match any of the specified types, NoneTemplate is returned as the default.
        /// </para>
        /// </summary>
        /// <param name="item">The item for which to select the template.</param>
        /// <param name="container">The container within which the item is displayed.</param>
        /// <returns>
        /// The DataTemplate that corresponds to the criterion model type, or NoneTemplate if there is no match.
        /// </returns>
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
