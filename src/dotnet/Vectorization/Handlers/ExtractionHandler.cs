﻿using FoundationaLLM.Vectorization.Interfaces;
using FoundationaLLM.Vectorization.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoundationaLLM.Vectorization.Handlers
{
    public class ExtractionHandler : IVectorizationStepHandler
    {
        private readonly string _stepId = "extract";

        public async Task<VectorizationState> Invoke(VectorizationRequest request, VectorizationState state, CancellationToken cancellationToken)
        {
            var step = request[_stepId];

            await Task.CompletedTask;
            return state;
        }
    }
}
