﻿using FoundationaLLM.Common.Models.Configuration.Instance;
using FoundationaLLM.Common.Services.Storage;
using FoundationaLLM.Upgrade;
using FoundationaLLM.Upgrade.DataSource;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace FoundationaLLM.Upgrade.Authorization
{
    public class AuthorizationUpgrade : Upgrade
    {
        public AuthorizationUpgrade(BlobStorageService blobStorageService,
            InstanceSettings instanceSettings,
            ILoggerFactory loggerFactory)
        {
            ObjectStartUpgradeVersion = Version.Parse("0.4.0");

            _blobStorageService = blobStorageService;
            _datasources = new Dictionary<string, string>();
            _dataSourceObjects = new Dictionary<string, object>();
            _instanceSettings = instanceSettings;

            TargetInstanceVersion = Version.Parse(_instanceSettings.Version);

            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<AuthorizationUpgrade>();

            _defaultValues = new Dictionary<string, object>();
        }

        protected ILogger<AuthorizationUpgrade> _logger;
        protected BlobStorageService _blobStorageService;

        protected Dictionary<string, string> _datasources { get; set; }
        protected Dictionary<string, object> _dataSourceObjects { get; set; }

        public void ConfigureDefaultValues()
        {
            if (_defaultValues == null)
                _defaultValues = new Dictionary<string, object>();
        }

        public async Task<Dictionary<string, string>> LoadArtifacts()
        {
            var fileContent = await _blobStorageService.ReadFileAsync("resource-provider", $"FoundationaLLM.{TypeName}/_data-source-references.json");

            JsonElement referenceStore = JsonSerializer.Deserialize<dynamic>(Encoding.UTF8.GetString(fileContent.ToArray()));

            JsonElement data = referenceStore.GetProperty("DataSourceReferences");

            foreach (var reference in data.EnumerateArray())
            {
                string fileName = reference.GetProperty("Filename").ToString();
                string agent = Encoding.UTF8.GetString(_blobStorageService.ReadFileAsync("resource-provider", fileName).Result.ToArray());
                _datasources.Add(fileName, agent);
            }

            return _datasources;
        }

        public virtual async Task<object> UpgradeProperties(object agent) => Task.FromResult(agent);

        public async override Task LoadAsync() =>
            await LoadArtifacts();

        public async override Task SaveAsync()
        {

        }

        public async override Task UpgradeAsync()
        {
            await LoadAsync();

            foreach (string name in _datasources.Keys)
            {
                string strSource = _datasources[name];

                object source = JsonSerializer.Deserialize<object>(strSource);

                Version agentVersion = GetObjectVersion(source);

                Version targetVersion = FLLMVersions.NextVersion(agentVersion);

                while (agentVersion < TargetInstanceVersion)
                {
                    //Load the target agent upgrade class
                    try
                    {
                        string type = $"FoundationaLLM.Common.Upgrade.Authorization.Authorization_{agentVersion.ToString().Replace(".", "")}_{targetVersion.ToString().Replace(".", "")}";
                        Type t = Type.GetType(type);

                        if (t == null)
                        {
                            _logger.LogWarning($"No upgrade path found for {name} from {agentVersion} to {targetVersion}");

                            targetVersion = FLLMVersions.NextVersion(targetVersion);

                            //reached the end...
                            if (targetVersion == Version.Parse("0.0.0"))
                                break;

                            continue;
                        }

                        var upgrader = (DatasourceUpgrade)Activator.CreateInstance(t, new object[] { _blobStorageService, _instanceSettings, _loggerFactory });

                        source = await upgrader.UpgradeDoWorkAsync(source);

                        agentVersion = GetObjectVersion(source);

                        targetVersion = FLLMVersions.NextVersion(agentVersion);
                    }
                    catch (Exception ex)
                    {
                        //try to move to the next version to see if an upgrade path exists
                        targetVersion = FLLMVersions.NextVersion(targetVersion);
                    }
                }

                _dataSourceObjects.Add(name, source);
            }

            await SaveAsync();

            return;
        }

        public override Task<object> UpgradeDoWorkAsync(object agent) => throw new NotImplementedException();
    }
}
