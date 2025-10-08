// Copyright (c) Microsoft. All rights reserved.

// This sample shows how to create AI agent declaratively with Azure OpenAI as the backend.

using System;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.Declarative;
using Microsoft.Extensions.AI;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

// Create the chat client
IChatClient chatClient = new AzureOpenAIClient(
    new Uri(endpoint),
    new AzureCliCredential())
     .GetChatClient(deploymentName)
     .AsIChatClient();

// Define the agent using a YAML definition.
var text =
    """
    kind: Prompt
    type: chat_client_agent
    id: my_translation_agent
    name: Translation Assistant
    description: A helpful assistant that translates text to a specified language.
    model:
        kind: OpenAIResponsesModel
        id: gpt-4o
        options:
            temperature: 0.9
            topP: 0.95
    instructions: You are a helpful assistant. You answer questions in {language}. You return your answers in a JSON format.
    additionalInstructions: You must always respond in the specified language.
    tools:
      - kind: codeInterpreter
    template:
        format: PowerFx # Mustache is the other option
        parser: None # Prompty and XML are the other options
    inputSchema:
        properties:
            language: string
    outputSchema:
        properties:
            language:
                type: string
                required: true
                description: The language of the answer.
            answer:
                type: string
                required: true
                description: The answer text.
    """;

// Alternatively, you can define the response format using as YAML for better readability.
/*
var textYaml =
    """
    kind: PromptAgent
    type: chat_client_agent
    name: Assistant
    description: Helpful assistant
    instructions: You are a helpful assistant. You answer questions is the language specified by the user. You return your answers in a JSON format.
    model:
      options:
        temperature: 0.9
        top_p: 0.95
        response_format:
          type: json_schema
          json_schema:
            name: assistant_response
            strict: true
            schema:
              $schema: http://json-schema.org/draft-07/schema#
              type: object
              properties:
                language:
                  type: string
                  description: The language of the answer.
                answer:
                  type: string
                  description: The answer text.
              required:
                - language
                - answer
              additionalProperties: false
    """;
*/

// Create the agent from the YAML definition.
var agentFactory = new ChatClientAgentFactory();
var agent = await agentFactory.CreateFromYamlAsync(text, new() { ChatClient = chatClient });

// Invoke the agent and output the text result.
Console.WriteLine(await agent!.RunAsync("Tell me a joke about a pirate."));

// Invoke the agent with streaming support.
await foreach (var update in agent!.RunStreamingAsync("Tell me a joke about a pirate."))
{
    Console.Write(update);
}
