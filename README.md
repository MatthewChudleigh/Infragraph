# Infragraph

A modular C# data processing pipeline that transforms Former2 AWS infrastructure JSON exports into interactive React Flow diagrams.

## Overview

Infragraph parses AWS infrastructure data exported from [Former2](https://former2.com). 

- extracts relationships between resources
- builds a graph model with hierarchical grouping
- renders it to React Flow format for visualization

```
Former2 JSON → Parse → Model → Extract Relations → Build Graph → Render → React Flow Diagram
```

## Features

- **Former2 JSON Parsing** - Streams and parses Former2 export format
- **Typed Resource Models** - Domain models for 30+ AWS resource types
- **Relationship Extraction** - Automatic discovery of connections between resources
- **Hierarchical Grouping** - VPC/Subnet and service-based grouping strategies
- **React Flow Output** - Ready-to-render diagram format with styling
- **REST API** - Simple HTTP endpoints for integration

## Project Structure

```
src/
├── Infragraph.Common/           # Interfaces & shared models
│   ├── Abstractions/            # Pipeline interfaces
│   ├── Models/                  # Domain, Graph, ReactFlow models
│   └── Configuration/           # Options and resource type registry
├── Infragraph.Core/             # Business logic
│   ├── Parsing/                 # Former2 JSON parser
│   ├── Modeling/                # Resource model factory
│   ├── Relationships/           # Relationship extractors
│   ├── Graph/                   # Graph builder
│   ├── Layout/                  # Grouping strategies
│   └── Pipeline/                # Pipeline orchestrator
├── Infragraph.Rendering/        # Output renderers
│   ├── ReactFlow/               # React Flow renderer
│   └── Export/                  # SVG and PNG exporters
├── Infragraph.Server/           # ASP.NET Core API
│   ├── Configuration/           # DI registration
│   └── Endpoints/               # REST endpoints
├── Infragraph.AppHost/          # .NET Aspire host
└── Infragraph.Web/              # React frontend with React Flow
```

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- Node.js 18+ (for frontend)

### Running the Server

```bash
cd Infragraph
dotnet run --project src/Infragraph.AppHost
```

The API will be available at `http://localhost:5000`.

### Using the API

#### Generate a Diagram

```bash
curl -X POST http://localhost:5379/api/diagram \
  -H "Content-Type: application/json" \
  -d @your-former2-export.json
```

#### Analyze Resources

```bash
curl -X POST http://localhost:5379/api/diagram/analyze \
  -H "Content-Type: application/json" \
  -d @your-former2-export.json
```

#### List Supported Resource Types

```bash
curl http://localhost:5000/api/resources/types
```

### Query Parameters

The `/api/diagram` endpoint supports the following query parameters:

| Parameter      | Description                          | Example                             |
| -------------- | ------------------------------------ | ----------------------------------- |
| `includeTypes` | Only include specific resource types | `?includeTypes=ec2.vpc,ec2.subnet`  |
| `excludeTypes` | Exclude specific resource types      | `?excludeTypes=iam.user,iam.policy` |
| `regions`      | Filter by AWS regions                | `?regions=ap-southeast-2`           |
| `showIsolated` | Include nodes without relationships  | `?showIsolated=true`                |
| `grouping`     | Grouping strategies to apply         | `?grouping=vpc,service`             |

### Exporting Diagrams

Export a positioned diagram to SVG or PNG:

```bash
# Export to SVG
curl -X POST http://localhost:5379/api/export/svg \
  -H "Content-Type: application/json" \
  -d '{"diagram": {...}, "options": {"title": "My Diagram"}}' \
  -o diagram.svg

# Export to PNG
curl -X POST http://localhost:5379/api/export/png \
  -H "Content-Type: application/json" \
  -d '{"diagram": {...}, "options": {"scale": 2.0}}' \
  -o diagram.png
```

#### Export Options

| Option             | Type    | Default   | Description                                |
| ------------------ | ------- | --------- | ------------------------------------------ |
| `backgroundColor`  | string  | `#ffffff` | Background color (CSS color)               |
| `padding`          | number  | `40`      | Padding around diagram in pixels           |
| `scale`            | number  | `1.0`     | Scale factor (2.0 for high-DPI)            |
| `includeTitle`     | boolean | `true`    | Include title at top                       |
| `title`            | string  | auto      | Custom title text                          |
| `includeMetadata`  | boolean | `true`    | Include resource/relationship counts       |
| `fontFamily`       | string  | system    | Font family for text                       |
| `includeEdgeLabels`| boolean | `true`    | Include labels on edges                    |
| `quality`          | number  | `1.0`     | PNG quality (0.0 to 1.0)                   |

## Supported Resource Types

### Networking
- `ec2.vpc` - VPC
- `ec2.subnet` - Subnet
- `ec2.securitygroup` - Security Group
- `ec2.routetable` - Route Table
- `ec2.internetgateway` - Internet Gateway
- `ec2.natgateway` - NAT Gateway
- `ec2.transitgateway` - Transit Gateway

### Compute
- `ec2.instance` - EC2 Instance
- `ecs.cluster` - ECS Cluster
- `ecs.service` - ECS Service
- `ecs.taskdefinition` - Task Definition
- `lambda.function` - Lambda Function

### Load Balancing
- `elbv2.loadbalancer` - Application/Network Load Balancer
- `elbv2.targetgroup` - Target Group
- `elbv2.listener` - Listener

### IAM
- `iam.role` - IAM Role
- `iam.user` - IAM User
- `iam.policy` - IAM Policy
- `iam.instanceprofile` - Instance Profile

### Storage & Database
- `s3.bucket` - S3 Bucket
- `rds.dbinstance` - RDS Instance
- `dynamodb.table` - DynamoDB Table

### Messaging
- `sqs.queue` - SQS Queue
- `sns.topic` - SNS Topic

## Relationship Types

The pipeline extracts the following relationship types:

| Type         | Description                   | Example                         |
| ------------ | ----------------------------- | ------------------------------- |
| `Contains`   | Parent contains child         | VPC → Subnet                    |
| `BelongsTo`  | Resource belongs to another   | Subnet → VPC                    |
| `Uses`       | Resource uses another         | ECS Service → Security Group    |
| `AttachedTo` | Resource attached to another  | ECS Service → Target Group      |
| `References` | Resource references another   | Security Group → Security Group |
| `Assumes`    | Resource assumes a role       | Task Definition → IAM Role      |
| `RoutesTo`   | Route table routes to gateway | Route Table → NAT Gateway       |
| `Targets`    | Listener forwards to target   | Listener → Target Group         |

## Architecture

### Pipeline Flow

1. **Parse** - `Former2Parser` reads JSON and yields `Former2Resource` objects
2. **Model** - `ResourceModelFactory` creates typed `AwsResource` domain models
3. **Extract** - `IRelationshipExtractor` implementations discover relationships
4. **Build** - `GraphBuilder` creates `InfraGraph` with nodes, edges, and groups
5. **Render** - `ReactFlowRenderer` converts to React Flow format

### Dependency Injection

Services are registered in `ServiceRegistration.cs`:

```csharp
services.AddInfragraphServices();
```

This registers:
- `IResourceParser` → `Former2Parser`
- `IResourceModelFactory` → `ResourceModelFactory`
- `IRelationshipExtractor` → Multiple extractors
- `IGroupingStrategy` → `VpcGrouper`, `ServiceGrouper`
- `IGraphBuilder` → `GraphBuilder`
- `IRenderer<ReactFlowDiagram>` → `ReactFlowRenderer`
- `IDiagramPipeline` → `DiagramPipeline`

## Output Format

The API returns React Flow-compatible JSON:

```json
{
  "nodes": [
    {
      "id": "vpc-123",
      "type": "awsResource",
      "position": { "x": 0, "y": 0 },
      "data": {
        "label": "my-vpc",
        "resourceType": "ec2.vpc",
        "service": "ec2"
      }
    }
  ],
  "edges": [
    {
      "id": "e-0",
      "source": "subnet-456",
      "target": "vpc-123",
      "type": "smoothstep",
      "label": "in VPC"
    }
  ],
  "metadata": {
    "totalResources": 437,
    "includedResources": 84,
    "totalRelationships": 108,
    "resourceTypes": ["ec2.vpc", "ec2.subnet", ...],
    "elkOptions": {
      "algorithm": "layered",
      "direction": "DOWN"
    }
  }
}
```

## Frontend

The Infragraph.Web project provides a complete React frontend for visualizing AWS infrastructure diagrams.

### Features

- **File Upload**: Drag-and-drop or click to upload Former2 JSON exports
- **ELK Layout**: Automatic hierarchical layout using ELK.js algorithm
- **Custom Nodes**: AWS service-specific node styling with icons and colors
- **Interactive Canvas**: Zoom, pan, minimap, and fit-to-view controls
- **Grouping Options**: Toggle VPC and service grouping, show isolated resources

### Running the Frontend

```bash
cd src/Infragraph.Web
npm install
npm run dev
```

The frontend will be available at `http://localhost:5173` and proxies API calls to the backend.

### Project Structure

```
src/Infragraph.Web/src/
├── App.tsx                    # Main application component
├── App.css                    # Styling with AWS color theme
├── components/
│   ├── DiagramCanvas.tsx      # React Flow canvas wrapper
│   ├── AwsResourceNode.tsx    # Custom node for AWS resources
│   ├── AwsGroupNode.tsx       # Custom node for groups (VPC, subnet)
│   ├── Toolbar.tsx            # Zoom, fit, export controls
│   └── FileUpload.tsx         # Drag-and-drop file upload
├── hooks/
│   └── useElkLayout.ts        # ELK.js layout computation
├── types/
│   └── diagram.ts             # TypeScript types and AWS styling
└── api/
    └── diagram.ts             # API client functions
```

## Development

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Adding a New Resource Type

1. Add the resource model to `Infragraph.Common/Models/Domain/`
2. Create a handler in `Infragraph.Core/Modeling/ResourceTypes/`
3. Register the handler in `ResourceModelFactory`
4. Add relationship extraction in `Infragraph.Core/Relationships/Extractors/`
5. Add to `SupportedResourceTypes` in `ResourceTypeInfo.cs`

## License

MIT
