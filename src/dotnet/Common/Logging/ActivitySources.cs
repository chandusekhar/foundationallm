﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoundationaLLM.Common.Logging
{
    public class ActivitySources
    {
        public static readonly ActivitySource AgentFactoryAPIActivitySource = new("FoundationaLLM.AgentFactoryAPI");
        public static readonly ActivitySource ChatActivitySource = new("FoundationaLLM.Chat");
        public static readonly ActivitySource CoreAPIActivitySource = new("FoundationaLLM.CoreAPI");
        public static readonly ActivitySource GatekeeperAPIActivitySource = new("FoundationaLLM.GatekeeperAPI");
        public static readonly ActivitySource SemanticKernelAPIActivitySource = new("FoundationaLLM.SemanticKernelAPI");
    }
}