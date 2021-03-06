﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using chocolatey;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.results;
using Svg;

namespace Wox.Plugin.Choco
{
    public class Main : IPlugin
    {
        private const string InstallCommand = "install";
        private const string UninstallCommand = "uninstall";
        private const string SearchCommand = "search";

        private string tempPath = Path.GetTempPath();

        private PluginInitContext context;

        public List<Result> Query(Query query)
        {
            return QueryAsync(query).GetAwaiter().GetResult();
        }

        private async Task<List<Result>> QueryAsync(Query query)
        {
            List<Result> results = new List<Result>();

            if (string.IsNullOrEmpty(query.Search))
            {
                results.Add(ResultForInstallCommandAutoComplete(query));
                results.Add(ResultForUninstallCommandAutoComplete(query));
                return results;
            }

            string command = query.FirstSearch.ToLower();
            if (string.IsNullOrEmpty(command)) return results;

            if (command == UninstallCommand)
            {
                return await ResultForUnInstallPackage(query);
            }
            if (command == InstallCommand)
            {
                return await ResultForInstallPackage(query);
            }

            if (InstallCommand.Contains(command))
            {
                results.Add(ResultForInstallCommandAutoComplete(query));
            }
            if (UninstallCommand.Contains(command))
            {
                results.Add(ResultForUninstallCommandAutoComplete(query));
            }

            return results;
        }

        private async Task<List<Result>> ResultForInstallPackage(Query query)
        {
            return await ChocolateyResults(config =>
            {
                config.CommandName = SearchCommand;
                config.Input = query.SecondSearch;
                config.ListCommand.OrderByPopularity = true;
            }, 10, (c, p) =>
            {
                Process.Start(new ProcessStartInfo("choco", $"{InstallCommand} {p.Name}") { Verb = "runas" });
                return true;
            });
        }

        private async Task<List<Result>> ResultForUnInstallPackage(Query query)
        {
            return await ChocolateyResults(config =>
            {
                config.CommandName = SearchCommand;
                config.Input = query.SecondSearch;
                config.ListCommand.LocalOnly = true;
            }, 10, (c, p) =>
            {
                Process.Start(new ProcessStartInfo("choco", $"{UninstallCommand} {p.Name}") { Verb = "runas" });
                return true;
            });
        }

        private async Task<List<Result>> ChocolateyResults(Action<ChocolateyConfiguration> propConfig, int count, Func<ActionContext, PackageResult, bool> action)
        {
            List<Result> results = new List<Result>();

            IEnumerable<PackageResult> packages = Lets.GetChocolatey().Set(propConfig).List<PackageResult>().Take(count);

            foreach (var package in packages)
            {
                var iconUrlString = package.Package.IconUrl.ToString();
                string iconPath;
                if (string.IsNullOrEmpty(iconUrlString))
                    iconPath = context.CurrentPluginMetadata.IcoPath;
                else
                {
                    var extension = iconUrlString.Substring(iconUrlString.LastIndexOf('.'));
                    iconPath = tempPath + package.Name + package.Version + extension;
                    if ((!File.Exists(iconPath) && extension != ".svg") || (extension == ".svg" && !File.Exists(iconPath.Replace(extension, ".png"))))
                        using (var client = new HttpClient())
                            try
                            {
                                using (var stream = await client.GetStreamAsync(package.Package.IconUrl))
                                    if (extension != ".svg")
                                        using (var fileStream = new FileStream(iconPath, FileMode.CreateNew))
                                            await stream.CopyToAsync(fileStream);
                                    else
                                        SvgDocument.Open<SvgDocument>(stream).Draw().Save(iconPath = iconPath.Replace(extension, ".png"));
                            }
                            catch (HttpRequestException)
                            {
                                iconPath = context.CurrentPluginMetadata.IcoPath;
                            }
                    else if (extension == ".svg")
                        iconPath = iconPath.Replace(extension, ".png");
                }
                results.Add(new Result
                {
                    Title = package.Package.Title,
                    SubTitle = $"{package.Version} - {package.Package.Summary}",
                    IcoPath = iconPath,
                    ContextData = package,
                    Action = c => action(c, package)
                });
            }

            return results;
        }

        private Result ResultForInstallCommandAutoComplete(Query query)
        {
            string title = $"{InstallCommand} <Package Name>";
            string subtitle = "install package";
            return ResultForCommand(query, InstallCommand, title, subtitle);
        }

        private Result ResultForUninstallCommandAutoComplete(Query query)
        {
            string title = $"{UninstallCommand} <Package Name>";
            string subtitle = "uninstall package";
            return ResultForCommand(query, UninstallCommand, title, subtitle);
        }

        private Result ResultForCommand(Query query, string command, string title, string subtitle)
        {
            string choco = query.ActionKeyword;
            const string seperater = Plugin.Query.TermSeperater;
            var result = new Result
            {
                Title = title,
                IcoPath = "Images\\icon.png",
                SubTitle = subtitle,
                Action = e =>
                {
                    context.API.ChangeQuery($"{choco}{seperater}{command}{seperater}");
                    return false;
                }
            };
            return result;
        }
        public void Init(PluginInitContext context)
        {
            this.context = context;
        }
    }
}
