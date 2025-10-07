// Copyright (c) Microsoft. All rights reserved.
using Azure.AI.Agents.Persistent;
using Azure.Core;
using Microsoft.Agents.Declarative;
using Microsoft.Bot.ObjectModel;

namespace Microsoft.Extensions.AI.Agents.AzureAI;

/// <summary>
/// Provides an <see cref="AgentFactory"/> which creates instances of <see cref="ChatClientAgent"/> using a <see cref="PersistentAgentsClient"/>.
/// </summary>
public sealed class AzureFoundryAgentFactory : AgentFactory
{
    /// <summary>
    /// The type of the chat client agent.
    /// </summary>
    public const string AzureFoundryAgentType = "azure_foundry_agent";

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAIAgentFactory"/> class.
    /// </summary>
    public AzureFoundryAgentFactory()
        : base([AzureFoundryAgentType])
    {
    }

    /// <inheritdoc/>
    public override async Task<AIAgent?> TryCreateAsync(PromptAgent promptAgent, AgentCreationOptions agentCreationOptions, CancellationToken cancellationToken = default)
    {
        //Throw.IfNull(promptAgent);

        ChatClientAgent? agent = null;
        PersistentAgentsClient? persistentAgentsClient;
        if (this.IsSupported(promptAgent))
        {
            persistentAgentsClient = agentCreationOptions.ServiceProvider?.GetService(typeof(PersistentAgentsClient)) as PersistentAgentsClient;
            if (persistentAgentsClient is null)
            {
                var endpoint = promptAgent.Model?.Connection?.GetEndpoint();
                if (string.IsNullOrEmpty(endpoint))
                {
                    throw new InvalidOperationException("The endpoint must be specified in the agent definition model connection to create an PersistentAgentsClient.");
                }

                if (agentCreationOptions.ServiceProvider?.GetService(typeof(TokenCredential)) is not TokenCredential credential)
                {
                    throw new InvalidOperationException("A TokenCredential must be registered in the service provider to create an AzureOpenAIClient.");
                }

                persistentAgentsClient = new PersistentAgentsClient(endpoint, credential);
            }

            var id = promptAgent.Id;
            if (!string.IsNullOrEmpty(id))
            {
                agent = await persistentAgentsClient.GetAIAgentAsync(id, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var model = promptAgent.Model?.Id;
                if (string.IsNullOrEmpty(model))
                {
                    throw new InvalidOperationException("The model id must be specified in the agent definition model to create a foundry agent.");
                }

                agent = await persistentAgentsClient.CreateAIAgentAsync(
                    model: model,
                    name: promptAgent.Name,
                    instructions: promptAgent.Instructions?.ToTemplateString(),
                    tools: promptAgent.GetFoundryToolDefinitions(),
                    toolResources: promptAgent.GetFoundryToolResources(),
                    temperature: promptAgent.GetTemperature(),
                    topP: promptAgent.GetTopP(),
                    responseFormat: promptAgent.GetResponseFormat(),
                    metadata: promptAgent.Metadata?.ToDictionary(),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        return agent;
    }
}
