# Infragraph Implementation Plan

## Overview

Build a modular C# data processing pipeline that:
1. Parses Former2 AWS infrastructure JSON
2. Extracts relationships between resources
3. Builds a graph model with grouping (VPC, service)
4. Applies layout via ELK.js (client-side)
5. Renders to React Flow diagram format

## Architecture

```
Server:   Former2 JSON → Parse → Model → Extract Relations → Build Graph → Render → API Response
Client:   API Response → ELK.js Layout → React Flow Render
```

## Implementation Status

### Phase 1: Foundation ✅ COMPLETE

- [x] Create interfaces in `Infragraph.Common/Abstractions/`
- [x] Create model classes in `Infragraph.Common/Models/`
- [x] Create configuration classes in `Infragraph.Common/Configuration/`
- [x] Implement `Former2Parser` in `Infragraph.Core/Parsing/`
- [x] Implement `ResourceModelFactory` with priority resource types

### Phase 2: Relationship Extraction ✅ COMPLETE

- [x] Implement `VpcSubnetExtractor`
- [x] Implement `SecurityGroupExtractor`
- [x] Implement `EcsServiceExtractor`
- [x] Implement `ElbTargetGroupExtractor`
- [x] Implement `IamRoleExtractor`
- [x] Implement `ComputeExtractor` (EC2, Lambda)

### Phase 3: Graph Building ✅ COMPLETE

- [x] Implement `GraphBuilder`
- [x] Implement `VpcGrouper` strategy
- [x] Implement `ServiceGrouper` strategy
- [x] Create `Infragraph.Rendering` project
- [x] Implement `ReactFlowRenderer`

### Phase 4: Layout Engine (Client-Side) ✅ COMPLETE

- [x] Add `elkjs` and `@xyflow/react` to `Infragraph.Web/package.json`
- [x] Create `useElkLayout` hook in React
- [x] Implement layout computation on frontend
- [x] Handle hierarchical/nested node layout

### Phase 5: API & Frontend ✅ COMPLETE

**Server (Complete):**
- [x] Add `DiagramEndpoints` to Server
- [x] Add `ResourceEndpoints` to Server
- [x] Configure DI in `ServiceRegistration`

**Frontend (Complete):**
- [x] Add React Flow components to `Infragraph.Web`
- [x] Create custom `AwsResourceNode` component
- [x] Create custom `AwsGroupNode` component
- [x] Create diagram toolbar (zoom, fit, export)
- [x] Add file upload for Former2 JSON
- [x] Implement grouping options UI (VPC, service, isolated)

### Phase 6: Polish & Export ✅ COMPLETE

- [x] Add SVG export endpoint (`POST /api/export/svg`)
- [x] Add PNG export endpoint (`POST /api/export/png`)
- [x] Create `Infragraph.Rendering/Export/` exporters
- [x] Create `ExportRequest` and `ExportOptions` models
- [x] Register exporters in DI container

### Phase 7: Tests ❌ NOT STARTED

- [ ] Performance optimization for large graphs
- [ ] Unit tests for parsing
- [ ] Unit tests for relationship extraction
- [ ] Unit tests for graph building
- [ ] Integration tests with sample data

---

## Remaining Work

### Priority 1: Frontend React Flow Integration ✅ COMPLETE

**Files created:**

```
src/Infragraph.Web/
├── src/
│   ├── App.tsx                    # Main app with React Flow ✅
│   ├── components/
│   │   ├── DiagramCanvas.tsx      # React Flow canvas wrapper ✅
│   │   ├── AwsResourceNode.tsx    # Custom node for AWS resources ✅
│   │   ├── AwsGroupNode.tsx       # Custom node for VPC/subnet groups ✅
│   │   ├── Toolbar.tsx            # Zoom, fit, export controls ✅
│   │   └── FileUpload.tsx         # Former2 JSON upload ✅
│   ├── hooks/
│   │   └── useElkLayout.ts        # ELK.js layout computation ✅
│   ├── types/
│   │   └── diagram.ts             # TypeScript types for API response ✅
│   └── api/
│       └── diagram.ts             # API client functions ✅
└── package.json                   # Dependencies added ✅
```

**Dependencies added:**
```json
{
  "@xyflow/react": "^12.0.0",
  "elkjs": "^0.9.0"
}
```

### Priority 2: Custom Node Components ✅ COMPLETE

**AwsResourceNode.tsx:** ✅
- Display resource icon based on service type
- Show resource name and type
- Color-coded border by service (EC2/ECS orange, ELB purple, RDS blue, S3 green, IAM red)
- Hover tooltip with details (ARN, region, tags)

**AwsGroupNode.tsx:** ✅
- VPC style: orange dashed border
- Subnet style: lighter orange dotted border
- Security group style: red solid border
- Service/cluster styles with appropriate colors
- Label with resource count badge

### Priority 3: Export Functionality

**Server endpoint:**
```csharp
// POST /api/export/svg
app.MapPost("/api/export/svg", async (HttpRequest request, IDiagramPipeline pipeline) => {
    var diagram = await pipeline.GenerateAsync(request.Body);
    var svg = SvgExporter.Export(diagram);
    return Results.File(svg, "image/svg+xml", "diagram.svg");
});
```

**Export implementation:**
```
src/Infragraph.Rendering/
└── Export/
    ├── SvgExporter.cs
    └── PngExporter.cs
```

### Priority 4: Unit Tests

**Test files to create:**
```
test/Infragraph.Core.Tests/
├── Parsing/
│   └── Former2ParserTests.cs
├── Modeling/
│   └── ResourceModelFactoryTests.cs
├── Relationships/
│   ├── VpcSubnetExtractorTests.cs
│   ├── EcsServiceExtractorTests.cs
│   └── ...
├── Graph/
│   └── GraphBuilderTests.cs
└── Pipeline/
    └── DiagramPipelineTests.cs
```

---

## API Reference

### Implemented Endpoints

| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| POST | `/api/diagram` | Generate React Flow diagram | ✅ |
| POST | `/api/diagram/analyze` | Resource/relationship analysis | ✅ |
| GET | `/api/resources/types` | List supported resource types | ✅ |
| GET | `/api/resources/types/{type}` | Get resource type info | ✅ |
| GET | `/api/resources/categories` | List resource categories | ✅ |

### Export Endpoints

| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| POST | `/api/export/svg` | Export diagram as SVG | ✅ |
| POST | `/api/export/png` | Export diagram as PNG | ✅ |

---

## Supported Resource Types

### Currently Implemented

| Category | Types |
|----------|-------|
| VPC/Network | `ec2.vpc`, `ec2.subnet`, `ec2.securitygroup`, `ec2.routetable`, `ec2.internetgateway`, `ec2.natgateway`, `ec2.transitgateway` |
| Compute | `ec2.instance`, `ecs.cluster`, `ecs.service`, `ecs.taskdefinition`, `lambda.function` |
| Load Balancing | `elbv2.loadbalancer`, `elbv2.targetgroup`, `elbv2.listener` |
| IAM | `iam.role`, `iam.user`, `iam.policy`, `iam.instanceprofile` |
| Storage | `s3.bucket`, `rds.dbinstance`, `dynamodb.table` |
| Messaging | `sqs.queue`, `sns.topic` |

### Future Additions

- `apigateway.restapi`, `apigatewayv2.api`
- `cloudfront.distribution`
- `elasticache.cluster`
- `eks.cluster`, `eks.nodegroup`
- `secretsmanager.secret`
- `kms.key`

---

## Relationship Types Extracted

| Type | Description | Example |
|------|-------------|---------|
| `Contains` | Parent contains child | VPC → Subnet |
| `BelongsTo` | Resource belongs to another | Subnet → VPC |
| `Uses` | Resource uses another | ECS Service → Security Group |
| `AttachedTo` | Resource attached to another | ECS Service → Target Group |
| `References` | Resource references another | Security Group → Security Group |
| `Assumes` | Resource assumes a role | Task Definition → IAM Role |
| `RoutesTo` | Route table routes to gateway | Route Table → NAT Gateway |
| `Targets` | Listener forwards to target | Listener → Target Group |

---

## Verification Checklist

### Current State
- [x] Server builds without errors
- [x] API accepts Former2 JSON and returns React Flow format
- [x] Relationships extracted correctly
- [x] VPC/Subnet grouping works
- [x] Sample data (brand-poker-prod.json) processes successfully
  - 437 total resources
  - 84 nodes (with relationships)
  - 108 edges

### Remaining Verification
- [x] Frontend renders diagram correctly (build passes)
- [x] ELK.js layout hook implemented
- [x] Custom nodes display AWS service icons
- [x] Export produces valid SVG/PNG (Phase 6)
- [ ] Performance acceptable for 500+ resources
- [ ] Unit test coverage > 80% (Phase 7)
