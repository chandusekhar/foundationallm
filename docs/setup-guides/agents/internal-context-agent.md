# Internal context agent

The internal context  provides a pass-through mechanism that sends the user prompt directly to the large language model (LLM) without any additional processing or context. This is useful when the user prompt is already prepared and does not require any additional context.

## Internal context agent configuration

The structure of an internal context agent is the following:

```json
{
  "name": "<name>",
  "type": "internal-context",
  "object_id": "/instances/<instance_id>/providers/FoundationaLLM.Agent/agents/<name>",
  "description": "<description>",  
  "language_model": {
    "type": "openai",
    "provider": "microsoft",
    "temperature": 0.0,
    "use_chat": true,
    "api_endpoint": "FoundationaLLM:AzureOpenAI:API:Endpoint",
    "api_key": "FoundationaLLM:AzureOpenAI:API:Key",
    "api_version": "FoundationaLLM:AzureOpenAI:API:Version",
    "version": "FoundationaLLM:AzureOpenAI:API:Completions:ModelVersion",
    "deployment": "FoundationaLLM:AzureOpenAI:API:Completions:DeploymentName"
  },
  "sessions_enabled": true,
  "conversation_history": {
    "enabled": true,
    "max_history": 5
  },
  "gatekeeper": {
    "use_system_setting": false,
    "options": [
      "ContentSafety",
      "Presidio"
    ]
  },
  "orchestrator": "LangChain"
}
```

where:

- `<name>` is the name of the agent.
- `<instance_id>` is the instance ID of the deployment.
- `<description>` is the description of the agent. Ensure that this description details the purpose of the agent.

| Parameter | Description |
| --- | --- |
| `name` | The name of the agent. |
| `type` | The type of the agent - will always be `internal-context`. |
| `object_id` | The object ID of the agent. Remove this element when creating an agent as this is generated by the Management API. |
| `description` | The description of the agent, ensure this description details the purpose of the agent. |
| `language_model` | The language model configuration. This sample demonstrates the usage of the Azure OpenAI language model. |
| `language_model.type` | The type of the language model. Currently supporting OpenAI based langauge models. |
| `language_model.provider` | The provider of the language model. Currently supporting `microsoft` or `openai`.  |
| `language_model.temperature` | The temperature value for the language model. A value between 0 and 1. Values closer to 0 return more factual information whereas values closer to 1 yield more creative responses. |
| `language_model.use_chat` | Determines the type of language model to use, as an example, when using Microsoft's Azure OpenAI, specifying `use_chat` equal to true will use the AzureChatOpenAI model vs. the AzureOpenAI model in LangChain.|
| `language_model.api_endpoint` | The configuration setting key that houses the API endpoint of the language model. The example above uses default FLLM values. Ensure this value is populated in application configuration. |
| `language_model.api_key` | The configuration setting key that houses a reference to a key vault value containing the API key for the language model service. Ensure these values are populated in key vault and app configuration. |
| `language_model.api_version` | The configuration setting key that houses the API version of the language model. The example above uses default FLLM values. Ensure this value is populated in application configuration. |
| `language_model.version` | The configuration setting key that houses the version of the language model deployment. The example above uses default FLLM values. Ensure this value is populated in application configuration. |
| `language_model.deployment` | The configuration setting key that houses the name given to the deployed language model. The example above uses default FLLM values. Ensure this value is populated in application configuration. |
| `sessions_enabled` | A boolean value that indicates whether the agent is session-less (false) or supports sessions(true). |
| `conversation_history` | The conversation history configuration. |
| `conversation_history.enabled` | Indicates if conversation history is retained for subsequent agent interactions(true). |
| `conversation_history.max_history` | indicates the number of messages to be retained. |
| `gatekeeper` | The gatekeeper configuration. |
| `gatekeeper.use_system_setting` | Indicates if the system settings are used for the gatekeeper. |
| `gatekeeper.options` | Contains the list of gatekeeper options. The sample provided overrides the system setting for gatekeeper and enables Azure Content Safety and MS Presidio in the messaging pipeline. |
| `orchestrator` | The orchestrator to be used for the agent. This can be set to `SemanticKernel` or `LangChain` |

## Managing internal context agents

This section describes how to manage internal context agents using the Management API. `{{baseUrl}}` is the base URL of the Management API. `{{instanceId}}` is the unique identifier of the FLLM instance.

### Retrieve

```http
HTTP GET {{baseUrl}}/instances/{{instanceId}}/providers/FoundationaLLM.Agent/agents
```

### Create or update

```http
HTTP POST {{baseUrl}}/instances/{{instanceId}}/providers/FoundationaLLM.Agent/agents/<name>
Content-Type: application/json

BODY
<agent_configuration>
```

where `<agent_configuration>` is the JSON agent configuration structure described above.

### Delete

```http
HTTP DELETE {{baseUrl}}/instances/{{instanceId}}/providers/FoundationaLLM.Agent/agents/<name>
```

## Validating an internal context agent

Once configured, the internal context agent can be validated using an API call to the [Core API](../exposed-apis/core-api.md) or via the [User Portal](../quickstart.md).

>**Note**: When validating through the user portal, ensure the `FoundationaLLM-AllowAgentHint` feature is enabled in the app configuration service. You may need to refresh your user portal browser for the agent to display in the agents list for selection.