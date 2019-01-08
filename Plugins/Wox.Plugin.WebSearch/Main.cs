using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Wox.Infrastructure;
using Wox.Infrastructure.Storage;

namespace Wox.Plugin.WebSearch
{
    public class Main : IPlugin, ISettingProvider, IPluginI18n, ISavable, IResultUpdated
    {
        private PluginInitContext _context;

        private readonly Settings _settings;
        private readonly SettingsViewModel _viewModel;
        private CancellationTokenSource _updateSource;
        private CancellationToken _updateToken;

        public const string Images = "Images";
        public static string ImagesDirectory;

        public void Save()
        {
            _viewModel.Save();
        }

        private string GetBrowserPath()
        {
            RegistryKey hRoot = Registry.ClassesRoot;
            RegistryKey command = hRoot.OpenSubKey("http\\shell\\open\\command", false);
            string registData = command.GetValue("").ToString();
            Match m = Regex.Match(registData, "\"(.*?)\"");
            string browserPath = m.Groups[1].Value;
            return browserPath;
        }

        public List<Result> Query(Query query)
        {
            _updateSource?.Cancel();
            _updateSource = new CancellationTokenSource();
            _updateToken = _updateSource.Token;

            SearchSource searchSource =
                _settings.SearchSources.FirstOrDefault(o => o.ActionKeyword == query.ActionKeyword && o.Enabled);

            string browserPath = GetBrowserPath();
            if (searchSource != null)
            {
                string keyword = query.Search;
                string title = keyword;
                string subtitle = _context.API.GetTranslation("wox_plugin_websearch_search") + " " + searchSource.Title;
                if (string.IsNullOrEmpty(keyword))
                {
                    var result = new Result
                    {
                        Title = subtitle,
                        SubTitle = string.Empty,
                        IcoPath = searchSource.IconPath,
                        Action = c =>
                        {
                            if (browserPath.Length == 0)
                            {
                                Process.Start(searchSource.Url);
                            }
                            else
                            {
                                Process.Start(browserPath, searchSource.Url);
                            }

                            return true;
                        }
                    };
                    return new List<Result> {result};
                }
                else
                {
                    var results = new List<Result>();
                    var result = new Result
                    {
                        Title = title,
                        SubTitle = subtitle,
                        Score = 6,
                        IcoPath = searchSource.IconPath,
                        Action = c =>
                        {
                            if (browserPath.Length == 0)
                            {
                                Process.Start(searchSource.Url.Replace("{q}", Uri.EscapeDataString(keyword)));
                            }
                            else
                            {
                                Process.Start(browserPath, searchSource.Url.Replace("{q}", Uri.EscapeDataString(keyword)));
                            }

                            return true;
                        }
                    };
                    results.Add(result);
                    UpdateResultsFromSuggestion(results, keyword, subtitle, searchSource, query);
                    return results;
                }
            }
            else
            {
                return new List<Result>();
            }
        }

        private void UpdateResultsFromSuggestion(List<Result> results, string keyword, string subtitle,
            SearchSource searchSource, Query query)
        {
            if (_settings.EnableSuggestion)
            {
                const int waittime = 300;
                var task = Task.Run(async () =>
                {
                    var suggestions = await Suggestions(keyword, subtitle, searchSource);
                    results.AddRange(suggestions);
                }, _updateToken);

                if (!task.Wait(waittime))
                {
                    task.ContinueWith(_ => ResultsUpdated?.Invoke(this, new ResultUpdatedEventArgs
                    {
                        Results = results,
                        Query = query
                    }), _updateToken);
                }
            }
        }

        private async Task<IEnumerable<Result>> Suggestions(string keyword, string subtitle, SearchSource searchSource)
        {
            var source = _settings.SelectedSuggestion;
            if (source != null)
            {
                var suggestions = await source.Suggestions(keyword);
                var resultsFromSuggestion = suggestions.Select(o => new Result
                {
                    Title = o,
                    SubTitle = subtitle,
                    Score = 5,
                    IcoPath = searchSource.IconPath,
                    Action = c =>
                    {
                        string browserPath = GetBrowserPath();
                        if (browserPath.Length == 0)
                        {
                            Process.Start(searchSource.Url.Replace("{q}", Uri.EscapeDataString(o)));
                        }
                        else
                        {
                            Process.Start(browserPath, searchSource.Url.Replace("{q}", Uri.EscapeDataString(o)));
                        }

                        return true;
                    }
                });
                return resultsFromSuggestion;
            }
            return new List<Result>();
        }

        public Main()
        {
            _viewModel = new SettingsViewModel();
            _settings = _viewModel.Settings;
        }

        public void Init(PluginInitContext context)
        {
            _context = context;
            var pluginDirectory = _context.CurrentPluginMetadata.PluginDirectory;
            var bundledImagesDirectory = Path.Combine(pluginDirectory, Images);
            ImagesDirectory = Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, Images);
            Helper.ValidateDataDirectory(bundledImagesDirectory, ImagesDirectory);
        }

        #region ISettingProvider Members

        public Control CreateSettingPanel()
        {
            return new SettingsControl(_context, _viewModel);
        }

        #endregion

        public string GetTranslatedPluginTitle()
        {
            return _context.API.GetTranslation("wox_plugin_websearch_plugin_name");
        }

        public string GetTranslatedPluginDescription()
        {
            return _context.API.GetTranslation("wox_plugin_websearch_plugin_description");
        }

        public event ResultUpdatedEventHandler ResultsUpdated;
    }
}