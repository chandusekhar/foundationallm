﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FoundationaLLM.Common.Models.Authentication
{
    /// <summary>
    /// The result of an object query.
    /// </summary>
    public class ObjectQueryResult
    {
        /// <summary>
        /// The unique identifier of the object.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The display name of the object.
        /// </summary>
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// The type of the object.
        /// </summary>
        [JsonPropertyName("object_type")]
        public string? ObjectType { get; set; }
    }
}
