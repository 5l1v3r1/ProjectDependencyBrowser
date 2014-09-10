﻿//-----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using MyToolkit.Model;
using MyToolkit.Mvvm;
using MyToolkit.Utilities;
using ProjectDependencyBrowser.ViewModels;

namespace ProjectDependencyBrowser.Views
{
    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class MainWindow : Window
    {
        /// <summary>Initializes a new instance of the <see cref="MainWindow"/> class. </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            ViewModelHelper.RegisterViewModel(Model, this);

            Closed += delegate { Model.CallOnUnloaded(); };
            Model.PropertyChanged += (sender, args) =>
            {
                if (args.IsProperty<MainWindowModel>(i => i.IsLoaded))
                {
                    Tabs.SelectedIndex = 1;
                    ProjectNameFilter.Focus(); // TODO: Fix this
                }
            };

            CheckForApplicationUpdate();
        }

        /// <summary>Gets the view model. </summary>
        public MainWindowModel Model
        {
            get { return (MainWindowModel)Resources["ViewModel"]; }
        }

        private async void CheckForApplicationUpdate()
        {
            var updater = new ApplicationUpdater(GetType().Assembly, "http://rsuter.com/Projects/ProjectDependencyBrowser/updates.xml");
            await updater.CheckForUpdate(this);
        }

        private void OnSelectDirectory(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            dlg.SelectedPath = Model.RootDirectory;
            dlg.Description = "Select root directory: ";
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                Model.RootDirectory = dlg.SelectedPath;
        }

        private void OnOpenHyperlink(object sender, RoutedEventArgs e)
        {
            var uri = ((Hyperlink)sender).NavigateUri;
            System.Diagnostics.Process.Start(uri.ToString());
        }
    }
}
