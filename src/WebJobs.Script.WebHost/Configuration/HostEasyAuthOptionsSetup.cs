﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Script.WebHost.Configuration
{
    // TODO - might not need this class (depending on whether middleware reads from env vars or not)
    public class HostEasyAuthOptionsSetup : IConfigureOptions<HostEasyAuthOptions>
    {
        private readonly IEnvironment _env;
        private readonly IConfiguration _configuration;

        public HostEasyAuthOptionsSetup(IEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
        }

        public void Configure(HostEasyAuthOptions options)
        {
            options.SiteAuthEnabled = Convert.ToBoolean(_env.GetEnvironmentVariable(EnvironmentSettingNames.EasyAuthEnabled));
            options.SiteAuthClientId = _env.GetEnvironmentVariable(EnvironmentSettingNames.EasyAuthClientId);
            // TODO - host.json, can't rely. What exactly does this code do? delete?
            IConfigurationSection jobHostSection = _configuration.GetSection(ConfigurationSectionNames.JobHost);
            var easyAuthSection = jobHostSection.GetSection(ConfigurationSectionNames.EasyAuth);
            easyAuthSection.Bind(options);
        }
    }
}
