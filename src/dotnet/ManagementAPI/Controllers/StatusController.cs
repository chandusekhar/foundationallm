﻿using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace FoundationaLLM.Management.API.Controllers
{
    /// <summary>
    /// Provides methods for checking the status of the service.
    /// </summary>
    [ApiVersion(1.0)]
    [ApiController]
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        /// <summary>
        /// Returns the status of the Management API service.
        /// </summary>
        [HttpGet(Name = "GetServiceStatus")]
        public IActionResult Get() =>
            Ok();

        /// <summary>
        /// Returns the allowed HTTP methods for the Management API service.
        /// </summary>
        [HttpOptions]
        public IActionResult Options()
        {
            HttpContext.Response.Headers.Append("Allow", new[] { "GET", "POST", "OPTIONS" });

            return Ok();
        }
    }
}
