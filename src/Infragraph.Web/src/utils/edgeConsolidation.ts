import type { Node, Edge } from '@xyflow/react';
import type { JunctionPointData } from '../types/diagram';

export interface ConsolidationCandidate {
  groupId: string;
  externalTargetId: string;
  internalSourceIds: string[];
  originalEdgeIds: string[];
  direction: 'outgoing' | 'incoming';
}

/**
 * Detect edges that can be consolidated through junction points.
 * Looks for multiple edges from nodes within a group to a common external node.
 */
export function detectConsolidationCandidates(
  nodes: Node[],
  edges: Edge[],
  minEdges: number = 2
): ConsolidationCandidate[] {
  // Build parent map for all nodes
  const nodeParentMap = new Map<string, string | undefined>();
  nodes.forEach(n => nodeParentMap.set(n.id, n.parentId));

  // Group edges by (parentId, externalTarget, direction)
  const groups = new Map<string, { edges: Edge[]; sources: Set<string> }>();

  for (const edge of edges) {
    const sourceParent = nodeParentMap.get(edge.source);
    const targetParent = nodeParentMap.get(edge.target);

    // Outgoing: source is in a group, target is outside that group
    if (sourceParent && sourceParent !== targetParent) {
      const key = `out:${sourceParent}:${edge.target}`;
      if (!groups.has(key)) {
        groups.set(key, { edges: [], sources: new Set() });
      }
      const group = groups.get(key)!;
      group.edges.push(edge);
      group.sources.add(edge.source);
    }

    // Incoming: target is in a group, source is outside that group
    if (targetParent && targetParent !== sourceParent) {
      const key = `in:${targetParent}:${edge.source}`;
      if (!groups.has(key)) {
        groups.set(key, { edges: [], sources: new Set() });
      }
      const group = groups.get(key)!;
      group.edges.push(edge);
      group.sources.add(edge.target);
    }
  }

  // Return candidates with at least minEdges
  const candidates: ConsolidationCandidate[] = [];

  for (const [key, value] of groups.entries()) {
    if (value.edges.length >= minEdges) {
      const [dir, groupId, externalId] = key.split(':');
      candidates.push({
        groupId,
        externalTargetId: externalId,
        internalSourceIds: [...value.sources],
        originalEdgeIds: value.edges.map(e => e.id),
        direction: dir as 'outgoing' | 'incoming',
      });
    }
  }

  return candidates;
}

/**
 * Apply edge consolidation by creating junction point nodes and replacing
 * multiple edges with consolidated edges through junction points.
 */
export function applyEdgeConsolidation(
  nodes: Node[],
  edges: Edge[],
  candidates: ConsolidationCandidate[]
): { nodes: Node[]; edges: Edge[] } {
  if (candidates.length === 0) {
    return { nodes, edges };
  }

  const newNodes: Node[] = [...nodes];
  const newEdges: Edge[] = [];
  const processedEdgeIds = new Set<string>();

  for (const candidate of candidates) {
    const junctionId = `junction_${candidate.groupId}_${candidate.externalTargetId}_${candidate.direction}`;

    // Create junction point node
    const junctionNode: Node<JunctionPointData> = {
      id: junctionId,
      type: 'junctionPoint',
      parentId: candidate.groupId,
      position: { x: 0, y: 0 }, // Will be set by ELK layout
      data: {
        groupId: candidate.groupId,
        externalTargetId: candidate.externalTargetId,
        direction: candidate.direction,
        edgeCount: candidate.internalSourceIds.length,
      },
    };
    newNodes.push(junctionNode);

    // Create internal edges (from group members to junction or vice versa)
    for (const sourceId of candidate.internalSourceIds) {
      const internalEdge: Edge = {
        id: `e_internal_${sourceId}_${junctionId}`,
        source: candidate.direction === 'outgoing' ? sourceId : junctionId,
        target: candidate.direction === 'outgoing' ? junctionId : sourceId,
        type: 'smoothstep',
        style: {
          stroke: '#6b7280',
          strokeWidth: 1.5,
        },
      };
      newEdges.push(internalEdge);
    }

    // Create external edge (from junction to external node or vice versa)
    const externalEdge: Edge = {
      id: `e_external_${junctionId}_${candidate.externalTargetId}`,
      source: candidate.direction === 'outgoing' ? junctionId : candidate.externalTargetId,
      target: candidate.direction === 'outgoing' ? candidate.externalTargetId : junctionId,
      type: 'smoothstep',
      data: {
        consolidated: true,
        count: candidate.internalSourceIds.length,
      },
      style: {
        stroke: '#6b7280',
        strokeWidth: 2.5,
      },
    };
    newEdges.push(externalEdge);

    // Mark original edges as processed
    candidate.originalEdgeIds.forEach(id => processedEdgeIds.add(id));
  }

  // Add non-consolidated edges
  for (const edge of edges) {
    if (!processedEdgeIds.has(edge.id)) {
      newEdges.push(edge);
    }
  }

  return { nodes: newNodes, edges: newEdges };
}
