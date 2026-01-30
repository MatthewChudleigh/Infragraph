import { useCallback, useState } from 'react';
import ELK, { type ElkNode, type ElkExtendedEdge, type LayoutOptions } from 'elkjs/lib/elk.bundled.js';
import type { Node, Edge } from '@xyflow/react';
import type { ElkOptions, AwsResourceData } from '../types/diagram';
// import type { InlineChild } from '../types/diagram';
// import { detectConsolidationCandidates, applyEdgeConsolidation } from '../utils/edgeConsolidation';

const elk = new ELK();

interface ElkLayoutResult {
  nodes: Node[];
  edges: Edge[];
}

interface UseElkLayoutResult {
  layoutNodes: (
    nodes: Node[],
    edges: Edge[],
    options?: ElkOptions
  ) => Promise<ElkLayoutResult>;
  isLayouting: boolean;
  error: Error | null;
}

const DEFAULT_NODE_WIDTH = 180;
const DEFAULT_NODE_HEIGHT = 60;
const DEFAULT_GROUP_PADDING = 60;

const JUNCTION_POINT_SIZE = 16;

function getNodeDimensions(node: Node): { width: number; height: number } {
  const isGroup = node.type === 'awsGroup';
  const isJunction = node.type === 'junctionPoint';

  if (isJunction) {
    return { width: JUNCTION_POINT_SIZE, height: JUNCTION_POINT_SIZE };
  }

  const data = node.data as AwsResourceData | undefined;
  const hasInlineChildren = data?.inlineChildren && data.inlineChildren.length > 0;

  // Add extra height for nodes with inline children
  const inlineChildrenHeight = hasInlineChildren
    ? (data.inlineChildren!.length * 20) + 24 // 20px per child + 24px for separator
    : 0;

  return {
    width: node.width ?? (isGroup ? 350 : DEFAULT_NODE_WIDTH),
    height: node.height ?? (isGroup ? 250 : DEFAULT_NODE_HEIGHT + inlineChildrenHeight),
  };
}

// NOTE: Single-child merging and edge consolidation features are disabled
// due to layout issues. The code is preserved below for future implementation.
//
// /**
//  * Detect nodes with exactly one incoming edge and no outgoing edges.
//  * These "single-child" nodes will be merged inline with their parent.
//  */
// function detectSingleChildNodes(
//   nodes: Node[],
//   edges: Edge[]
// ): Map<string, string> {
//   const incomingEdges = new Map<string, string[]>();
//   const outgoingEdges = new Map<string, string[]>();
//
//   for (const edge of edges) {
//     if (!outgoingEdges.has(edge.source)) outgoingEdges.set(edge.source, []);
//     outgoingEdges.get(edge.source)!.push(edge.target);
//
//     if (!incomingEdges.has(edge.target)) incomingEdges.set(edge.target, []);
//     incomingEdges.get(edge.target)!.push(edge.source);
//   }
//
//   const singleChildMap = new Map<string, string>();
//
//   for (const node of nodes) {
//     // Skip group nodes - they shouldn't be merged
//     if (node.type === 'awsGroup') continue;
//
//     const incoming = incomingEdges.get(node.id) ?? [];
//     const outgoing = outgoingEdges.get(node.id) ?? [];
//
//     // Single parent, no children - candidate for merging
//     if (incoming.length === 1 && outgoing.length === 0) {
//       const parentId = incoming[0];
//       const parentNode = nodes.find(n => n.id === parentId);
//
//       // Only merge if parent is not a group node
//       if (parentNode && parentNode.type !== 'awsGroup') {
//         singleChildMap.set(node.id, parentId);
//       }
//     }
//   }
//
//   return singleChildMap;
// }
//
// /**
//  * Merge single-child nodes into their parent nodes as inline children.
//  * Returns updated nodes and edges with single-child nodes removed.
//  */
// function mergeSingleChildNodes(
//   nodes: Node[],
//   edges: Edge[],
//   singleChildMap: Map<string, string>
// ): { nodes: Node[]; edges: Edge[] } {
//   const childrenToRemove = new Set(singleChildMap.keys());
//
//   // Build a map of parent -> children
//   const parentChildrenMap = new Map<string, Node[]>();
//   for (const [childId, parentId] of singleChildMap) {
//     const childNode = nodes.find(n => n.id === childId);
//     if (childNode) {
//       if (!parentChildrenMap.has(parentId)) {
//         parentChildrenMap.set(parentId, []);
//       }
//       parentChildrenMap.get(parentId)!.push(childNode);
//     }
//   }
//
//   // Update parent nodes to include child info
//   const updatedNodes = nodes
//     .filter(n => !childrenToRemove.has(n.id))
//     .map(node => {
//       const children = parentChildrenMap.get(node.id);
//
//       if (children && children.length > 0) {
//         const inlineChildren: InlineChild[] = children.map(c => ({
//           id: c.id,
//           label: String((c.data as Record<string, unknown>).label ?? c.id),
//           type: String((c.data as Record<string, unknown>).resourceType ?? 'unknown'),
//         }));
//
//         return {
//           ...node,
//           data: {
//             ...node.data,
//             inlineChildren,
//           },
//         };
//       }
//       return node;
//     });
//
//   // Remove edges to/from merged children
//   const updatedEdges = edges.filter(
//     e => !childrenToRemove.has(e.source) && !childrenToRemove.has(e.target)
//   );
//
//   return { nodes: updatedNodes, edges: updatedEdges };
// }

function convertToElkGraph(
  nodes: Node[],
  edges: Edge[],
  options?: ElkOptions
): { children: ElkNode[]; edges: ElkExtendedEdge[]; layoutOptions: LayoutOptions } {
  // Build parent-child relationships
  const childrenByParent = new Map<string | undefined, Node[]>();

  for (const node of nodes) {
    const parentId = node.parentId;
    if (!childrenByParent.has(parentId)) {
      childrenByParent.set(parentId, []);
    }
    childrenByParent.get(parentId)!.push(node);
  }

  // Recursively convert nodes to ELK format
  function convertNode(node: Node): ElkNode {
    const children = childrenByParent.get(node.id) ?? [];
    const isGroup = children.length > 0;

    const elkNode: ElkNode = {
      id: node.id,
    };

    if (isGroup) {
      // For groups, let ELK calculate size based on children
      // We only set padding and algorithm options
      elkNode.children = children.map(convertNode);
      elkNode.layoutOptions = {
        'elk.padding': `[top=${DEFAULT_GROUP_PADDING + 20},left=${DEFAULT_GROUP_PADDING},bottom=${DEFAULT_GROUP_PADDING},right=${DEFAULT_GROUP_PADDING}]`,
        'elk.algorithm': 'layered',
        'elk.direction': 'DOWN',
      };
    } else {
      // For leaf nodes, provide explicit dimensions
      const { width, height } = getNodeDimensions(node);
      elkNode.width = width;
      elkNode.height = height;
    }

    return elkNode;
  }

  // Get root-level nodes (no parent)
  const rootNodes = childrenByParent.get(undefined) ?? [];
  const elkChildren = rootNodes.map(convertNode);

  // Convert edges
  const elkEdges: ElkExtendedEdge[] = edges.map((edge) => ({
    id: edge.id,
    sources: [edge.source],
    targets: [edge.target],
  }));

  // Build layout options
  const layoutOptions: LayoutOptions = {
    'elk.algorithm': options?.algorithm ?? 'layered',
    'elk.direction': options?.direction ?? 'DOWN',
    'elk.hierarchyHandling': options?.['elk.hierarchyHandling'] ?? 'INCLUDE_CHILDREN',
    'elk.layered.spacing.nodeNodeBetweenLayers':
      options?.['elk.layered.spacing.nodeNodeBetweenLayers'] ?? '100',
    'elk.spacing.nodeNode': options?.['elk.spacing.nodeNode'] ?? '60',
    'elk.spacing.componentComponent': '80',
    'elk.layered.considerModelOrder.strategy': 'NODES_AND_EDGES',
    'elk.edgeRouting': 'ORTHOGONAL',
  };

  return { children: elkChildren, edges: elkEdges, layoutOptions };
}

interface ElkLayoutInfo {
  x: number;
  y: number;
  width?: number;
  height?: number;
}

function applyElkLayout(
  nodes: Node[],
  elkGraph: ElkNode
): Node[] {
  const layoutMap = new Map<string, ElkLayoutInfo>();

  // Recursively extract positions and dimensions from ELK result
  function extractLayout(elkNode: ElkNode, parentX = 0, parentY = 0): void {
    const x = (elkNode.x ?? 0) + parentX;
    const y = (elkNode.y ?? 0) + parentY;
    layoutMap.set(elkNode.id, {
      x,
      y,
      width: elkNode.width,
      height: elkNode.height,
    });

    if (elkNode.children) {
      for (const child of elkNode.children) {
        extractLayout(child, x, y);
      }
    }
  }

  // Process root children
  if (elkGraph.children) {
    for (const child of elkGraph.children) {
      extractLayout(child);
    }
  }

  // Apply positions and dimensions to nodes
  return nodes.map((node) => {
    const layout = layoutMap.get(node.id);
    if (layout) {
      // For nodes with a parent, position is relative to parent
      if (node.parentId) {
        const parentLayout = layoutMap.get(node.parentId);
        if (parentLayout) {
          return {
            ...node,
            position: {
              x: layout.x - parentLayout.x,
              y: layout.y - parentLayout.y,
            },
            // Apply ELK-calculated dimensions for groups
            ...(layout.width && layout.height ? {
              width: layout.width,
              height: layout.height,
              style: { ...node.style, width: layout.width, height: layout.height },
            } : {}),
          };
        }
      }
      return {
        ...node,
        position: { x: layout.x, y: layout.y },
        // Apply ELK-calculated dimensions for groups
        ...(layout.width && layout.height ? {
          width: layout.width,
          height: layout.height,
          style: { ...node.style, width: layout.width, height: layout.height },
        } : {}),
      };
    }
    return node;
  });
}

export function useElkLayout(): UseElkLayoutResult {
  const [isLayouting, setIsLayouting] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const layoutNodes = useCallback(
    async (
      nodes: Node[],
      edges: Edge[],
      options?: ElkOptions
    ): Promise<ElkLayoutResult> => {
      if (nodes.length === 0) {
        return { nodes: [], edges };
      }

      setIsLayouting(true);
      setError(null);

      try {
        // Step 1: Detect and merge single-child nodes (disabled for now - causes layout issues)
        // const singleChildMap = detectSingleChildNodes(nodes, edges);
        // const { nodes: mergedNodes, edges: mergedEdges } = mergeSingleChildNodes(
        //   nodes,
        //   edges,
        //   singleChildMap
        // );
        const mergedNodes = nodes;
        const mergedEdges = edges;

        // Step 2: Apply edge consolidation via junction points (disabled for now - causes layout issues)
        // const consolidationCandidates = detectConsolidationCandidates(mergedNodes, mergedEdges);
        // const { nodes: consolidatedNodes, edges: consolidatedEdges } = applyEdgeConsolidation(
        //   mergedNodes,
        //   mergedEdges,
        //   consolidationCandidates
        // );
        const consolidatedNodes = mergedNodes;
        const consolidatedEdges = mergedEdges;

        const { children, edges: elkEdges, layoutOptions } = convertToElkGraph(
          consolidatedNodes,
          consolidatedEdges,
          options
        );

        const elkGraph = await elk.layout({
          id: 'root',
          children,
          edges: elkEdges,
          layoutOptions,
        });

        const layoutedNodes = applyElkLayout(consolidatedNodes, elkGraph);

        return { nodes: layoutedNodes, edges: consolidatedEdges };
      } catch (err) {
        const layoutError =
          err instanceof Error ? err : new Error('Layout failed');
        setError(layoutError);
        throw layoutError;
      } finally {
        setIsLayouting(false);
      }
    },
    []
  );

  return { layoutNodes, isLayouting, error };
}
