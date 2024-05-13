﻿using FoundationaLLM.Common.Models.Configuration.Instance;
using FoundationaLLM.Common.Services.Storage;
using FoundationaLLM.Upgrade.Models._050;
using FoundationaLLM.Upgrade.Models._060;
using FoundationaLLM.Upgrade.Vectorization.Indexing;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FoundationaLLM.Upgrade.Vectorization.ContentSource
{
    public class IndexingProfile_050_060 : ContentSourceProfileUpgrade
    {
        public IndexingProfile_050_060(BlobStorageService blobStorageService,
            InstanceSettings settings,
            ILoggerFactory loggerFactory) : base(blobStorageService, settings, loggerFactory)
        {
            _blobStorageService = blobStorageService;
            _logger = loggerFactory.CreateLogger<IndexingProfile_050_060>();

            TypeName = "ContentSourceProfile";

            SourceInstanceVersion = Version.Parse("0.5.0");

            SourceType = typeof(IndexingProfile050);
            TargetType = typeof(IndexingProfile060);
        }

        private ILogger<IndexingProfile_050_060> _logger;

        public void ConfigureDefaultValues() => base.ConfigureDefaultValues();

        public override Task<Dictionary<string, string>> LoadArtifacts() => base.LoadArtifacts();

        public async override Task<object> UpgradeDoWorkAsync(object in_agent)
        {
            ConfigureDefaultValues();

            string strAgent = JsonSerializer.Serialize(in_agent);

            IndexingProfile050 source = JsonSerializer.Deserialize<IndexingProfile050>(strAgent);

            IndexingProfile060 target = JsonSerializer.Deserialize<IndexingProfile060>(strAgent);

            if (source.Version == SourceInstanceVersion)
            {
                SetDefaultValues(target);

                target.Version = Version.Parse("0.6.0");

                _logger.LogInformation($"Upgraded {TypeName} {source.Name} from version {source.Version} to version {target.Version}");
            }

            return target;
        }

        public override Task<object> UpgradeProperties(object agent) => Task.FromResult(agent);
    }
}
