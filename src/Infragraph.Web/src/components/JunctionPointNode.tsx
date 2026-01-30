import { memo } from 'react';
import { Handle, Position, type NodeProps } from '@xyflow/react';
import type { JunctionPointNode as JunctionPointNodeType } from '../types/diagram';

function JunctionPointNodeComponent({ data }: NodeProps<JunctionPointNodeType>) {
  const isOutgoing = data.direction === 'outgoing';

  return (
    <div className="junction-point-node" title={`${data.edgeCount} connections`}>
      <Handle
        type="target"
        position={isOutgoing ? Position.Top : Position.Bottom}
        className="junction-handle"
      />
      <div className="junction-dot">
        {data.edgeCount > 2 && (
          <span className="junction-count">{data.edgeCount}</span>
        )}
      </div>
      <Handle
        type="source"
        position={isOutgoing ? Position.Bottom : Position.Top}
        className="junction-handle"
      />
    </div>
  );
}

export const JunctionPointNode = memo(JunctionPointNodeComponent);
