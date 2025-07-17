# Configurable Workflow Engine

This project is a .NET 8 backend service that implements a configurable state-machine workflow engine, as per the Infonetica take-home exercise.

## Features

*   Define workflows with states and actions.
*   Create instances of workflows.
*   Execute actions to transition instances between states.
*   Persists definitions and instances to local JSON files (`workflows.json`, `instances.json`).

## Quick Start

1.  **Prerequisites**: .NET 8 SDK.
2.  **Run the service**:
    ```bash
    dotnet run
    ```
3.  The service will be available at `http://localhost:5000` (or a similar port).

## API Endpoints

### Workflow Definitions

*   **Create a Definition**: `POST /definitions`
*   **Get All Definitions**: `GET /definitions`
*   **Get a Definition by ID**: `GET /definitions/{id}`

### Workflow Instances

*   **Start a New Instance**: `POST /definitions/{id}/instances`
*   **Execute an Action**: `POST /instances/{instId}/actions/{actionId}`
*   **Get an Instance by ID**: `GET /instances/{instId}`
*   **Get All Instances**: `GET /instances`

## Assumptions & Limitations

*   The service uses simple JSON file storage, which is not thread-safe for concurrent writes.
*   Error handling provides basic JSON responses with an `error` key.
*   Input validation is handled within the service layer.

## TODO

*   Add more robust concurrency control for file access.
*   Implement more detailed logging.
*   Add unit and integration tests.# infonetica_assignment
