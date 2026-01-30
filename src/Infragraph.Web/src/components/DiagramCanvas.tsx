import { memo, useCallback, useEffect, useState } from 'react';
import {
  ReactFlow,
  Background,
  Controls,
  MiniMap,
  Panel,
  useNodesState,
  useEdgesState,
  useReactFlow,
  type NodeTypes,
  type DefaultEdgeOptions,
  type Node,
  type Edge,
  BackgroundVariant,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';

import { AwsResourceNode } from './AwsResourceNode';
import { AwsGroupNode } from './AwsGroupNode';
import { JunctionPointNode } from './JunctionPointNode';
import { Toolbar } from './Toolbar';
import { useElkLayout } from '../hooks/useElkLayout';
import type { DiagramNode, DiagramEdge, DiagramMetadata } from '../types/diagram';

interface DiagramCanvasProps {
  initialNodes: DiagramNode[];
  initialEdges: DiagramEdge[];
  metadata: DiagramMetadata;
  onExport?: () => void;
}

const nodeTypes: NodeTypes = {
  awsResource: AwsResourceNode,
  awsGroup: AwsGroupNode,
  junctionPoint: JunctionPointNode,
};

const defaultEdgeOptions: DefaultEdgeOptions = {
  type: 'smoothstep',
  animated: false,
  style: {
    stroke: '#6b7280',
    strokeWidth: 1.5,
  },
};

function DiagramCanvasComponent({
  initialNodes,
  initialEdges,
  metadata,
  onExport,
}: DiagramCanvasProps) {
  const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes as Node[]);
  const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges as Edge[]);
  const [isInitialLayout, setIsInitialLayout] = useState(true);

  const { layoutNodes, isLayouting } = useElkLayout();
  const { fitView } = useReactFlow();

  // Apply ELK layout when nodes/edges change
  useEffect(() => {
    if (initialNodes.length === 0) return;

    const applyLayout = async () => {
      try {
        const { nodes: layoutedNodes, edges: layoutedEdges } = await layoutNodes(
          initialNodes as Node[],
          initialEdges as Edge[],
          metadata.elkOptions
        );
        setNodes(layoutedNodes);
        setEdges(layoutedEdges);
        setIsInitialLayout(false);
      } catch (err) {
        console.error('Layout failed:', err);
        // Fall back to initial nodes if layout fails
        setNodes(initialNodes as Node[]);
        setEdges(initialEdges as Edge[]);
        setIsInitialLayout(false);
      }
    };

    applyLayout();
  }, [initialNodes, initialEdges, metadata.elkOptions, layoutNodes, setNodes, setEdges]);

  // Fit view after initial layout
  useEffect(() => {
    if (!isInitialLayout && nodes.length > 0) {
      // Small delay to ensure DOM is updated
      const timer = setTimeout(() => {
        fitView({ padding: 0.1, duration: 300 });
      }, 100);
      return () => clearTimeout(timer);
    }
  }, [isInitialLayout, nodes.length, fitView]);

  const handleFitView = useCallback(() => {
    fitView({ padding: 0.1, duration: 300 });
  }, [fitView]);

  // Custom minimap node color
  const nodeColor = useCallback((node: Node) => {
    if (node.type === 'awsGroup') {
      return '#FF9900';
    }
    const data = node.data as { service?: string } | undefined;
    const service = data?.service?.toLowerCase();
    switch (service) {
      case 'ec2':
      case 'ecs':
      case 'lambda':
        return '#FF9900';
      case 'elbv2':
        return '#8C4FFF';
      case 'rds':
      case 'dynamodb':
        return '#3B48CC';
      case 's3':
        return '#569A31';
      case 'iam':
        return '#DD344C';
      case 'sqs':
      case 'sns':
        return '#FF4F8B';
      default:
        return '#232F3E';
    }
  }, []);

  return (
    <div className="diagram-canvas">
      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        nodeTypes={nodeTypes}
        defaultEdgeOptions={defaultEdgeOptions}
        fitView
        minZoom={0.1}
        maxZoom={2}
        attributionPosition="bottom-left"
        proOptions={{ hideAttribution: true }}
      >
        <Background
          variant={BackgroundVariant.Dots}
          gap={20}
          size={1}
          color="#374151"
        />
        <MiniMap
          nodeColor={nodeColor}
          nodeStrokeWidth={3}
          zoomable
          pannable
          className="diagram-minimap"
        />
        <Controls className="diagram-controls" />

        <Panel position="top-right" className="diagram-toolbar-panel">
          <Toolbar
            onFitView={handleFitView}
            onExport={onExport}
            isLayouting={isLayouting}
          />
        </Panel>

        <Panel position="top-left" className="diagram-info-panel">
          <div className="diagram-stats">
            <span className="stat-item">
              <span className="stat-value">{metadata.includedResources}</span>
              <span className="stat-label">Resources</span>
            </span>
            <span className="stat-divider">|</span>
            <span className="stat-item">
              <span className="stat-value">{metadata.totalRelationships}</span>
              <span className="stat-label">Relationships</span>
            </span>
            <span className="stat-divider">|</span>
            <span className="stat-item">
              <span className="stat-value">{metadata.resourceTypes.length}</span>
              <span className="stat-label">Types</span>
            </span>
          </div>
        </Panel>
      </ReactFlow>

      {isLayouting && (
        <div className="diagram-loading-overlay" role="status" aria-live="polite">
          <div className="diagram-loading-spinner" />
          <span>Calculating layout...</span>
        </div>
      )}
    </div>
  );
}

export const DiagramCanvas = memo(DiagramCanvasComponent);
