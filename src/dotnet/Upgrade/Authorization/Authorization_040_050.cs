﻿using FoundationaLLM.Common.Models.Configuration.Instance;
using FoundationaLLM.Common.Services.Storage;
using FoundationaLLM.Upgrade.Models._040;
using FoundationaLLM.Upgrade.Models._050;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FoundationaLLM.Upgrade.Authorization
{
    public class Authorization_040_050 : AuthorizationUpgrade
    {
        public Authorization_040_050(BlobStorageService blobStorageService,
            InstanceSettings settings,
            ILoggerFactory loggerFactory) : base(blobStorageService, settings, loggerFactory)
        {
            _blobStorageService = blobStorageService;
            _logger = loggerFactory.CreateLogger<Authorization_040_050>();

            SourceInstanceVersion = Version.Parse("0.4.0");
        }

        private ILogger<Authorization_040_050> _logger;

        public void ConfigureDefaultValues() => base.ConfigureDefaultValues();

        public async override Task<object> UpgradeDoWorkAsync(object in_source) => null;

        public override Task<object> UpgradeProperties(object source) => Task.FromResult(source);
    }
}
