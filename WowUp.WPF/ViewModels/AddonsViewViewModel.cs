﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WowUp.WPF.Extensions;
using WowUp.WPF.Models;
using WowUp.WPF.Services.Contracts;
using WowUp.WPF.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Windows.Controls;
using System.Collections.Generic;
using WowUp.WPF.Views;
using System.Windows;
using WowUp.WPF.Errors;

namespace WowUp.WPF.ViewModels
{
    public class AddonsViewViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IWarcraftService _warcraftService;
        private readonly IAddonService _addonService;

        private int _selectedWowIndex = 0;
        public int SelectedWowIndex
        {
            get => _selectedWowIndex;
            set { SetProperty(ref _selectedWowIndex, value); }
        }

        private bool _showEmptyLabel;
        public bool ShowEmptyLabel
        {
            get => _showEmptyLabel;
            set { SetProperty(ref _showEmptyLabel, value); }
        }

        private bool _showResults;
        public bool ShowResults
        {
            get => _showResults;
            set { SetProperty(ref _showResults, value); }
        }

        private bool _enableUpdateAll;
        public bool EnableUpdateAll
        {
            get => _enableUpdateAll;
            set { SetProperty(ref _enableUpdateAll, value); }
        }

        private IList<WowClientType> _clientTypes = new List<WowClientType>();
        private IList<string> _clientNames = new List<string>();

        public Command LoadItemsCommand { get; set; }
        public Command RefreshCommand { get; set; }
        public Command RescanCommand { get; set; }
        public Command UpdateAllCommand { get; set; }
        public Command InstallCommand { get; set; }

        public ObservableCollection<ComboBoxItem> ClientNames { get; set; }
        public ObservableCollection<AddonListItemViewModel> DisplayAddons { get; set; }

        public WowClientType SelectedClientType => _clientTypes[SelectedWowIndex];

        public AddonsViewViewModel(
            IServiceProvider serviceProvider,
            IAddonService addonService,
            IWarcraftService warcraftService)
        {
            _addonService = addonService;
            _warcraftService = warcraftService;
            _serviceProvider = serviceProvider;

            Initialize();
        }

        public async void Initialize()
        {
            ClientNames = new ObservableCollection<ComboBoxItem>();
            DisplayAddons = new ObservableCollection<AddonListItemViewModel>();
            LoadItemsCommand = new Command(async () => await LoadItems());
            RefreshCommand = new Command(async () => await LoadItems());
            RescanCommand = new Command(async () => await LoadItems(true));
            UpdateAllCommand = new Command(async () => await UpdateAll());
            InstallCommand = new Command(async () => await InstallNewAddon());

            _clientTypes = await _warcraftService.GetWowClients();
            _clientNames = await _warcraftService.GetWowClientNames();

            for(var i = 0; i < _clientNames.Count; i += 1)
            {
                var clientName = _clientNames[i];
                ClientNames.Add(new ComboBoxItem
                {
                    Content = clientName
                });
            }
        }

        public async Task UpdateAll()
        {
            EnableUpdateAll = false;

            await DisplayAddons
                .Where(addon => addon.CanUpdate || addon.CanInstall)
                .ForEachAsync(2, async addon =>
                {
                    await addon.InstallAddon();
                });

            EnableUpdateAll = true;
        }

        private async Task InstallNewAddon()
        {
            // Instantiate the dialog box
            var dlg = _serviceProvider.GetService<InstallUrlWindow>();

            // Configure the dialog box
            dlg.Owner = Application.Current.MainWindow;

            // Open the dialog box modally
            if (dlg.ShowDialog() == false)
            {
                return;
            }

            var result = (dlg.DataContext as InstallUrlDialogViewModel).Input;
            if (string.IsNullOrEmpty(result))
            {
                return;
            }

            Uri uri;
            try
            {
                uri = new Uri(result);
            }
            catch (Exception)
            {
                MessageBox.Show("Input was not a valid URL.");
                return;
            }

            try
            {
                await _addonService.InstallAddon(uri, SelectedClientType);
            }
            catch (AddonNotFoundException)
            {
                MessageBox.Show("Addon not found");
            }
            catch (AddonAlreadyInstalledException)
            {
                MessageBox.Show("Addon already installed");
            }

            await LoadItems();
        }

        public async Task LoadItems(bool forceReload = false)
        {
            IsBusy = true;
            EnableUpdateAll = false;
            ShowResults = false;
            ShowEmptyLabel = false;

            try
            {
                DisplayAddons.Clear();

                var wowType = _clientTypes[SelectedWowIndex];

                var addons = await _addonService.GetAddons(wowType, forceReload);
                addons = addons.OrderBy(addon => addon.GetDisplayState())
                    .ThenBy(addon => addon.Name)
                    .ToList();

                foreach (var addon in addons)
                {
                    if (string.IsNullOrEmpty(addon.LatestVersion))
                    {
                        continue;
                    }

                    var viewModel = _serviceProvider.GetService<AddonListItemViewModel>();
                    viewModel.Addon = addon;

                    DisplayAddons.Add(viewModel);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "LoadItems");
            }
            finally
            {
                IsBusy = false;
                ShowResults = DisplayAddons.Any();
                ShowEmptyLabel = !DisplayAddons.Any();
                EnableUpdateAll = DisplayAddons.Any(addon => addon.CanUpdate || addon.CanInstall);
            }
        }

        public class ComboData
        {
            public int Id { get; set; }
            public string Value { get; set; }
        }
    }
}
