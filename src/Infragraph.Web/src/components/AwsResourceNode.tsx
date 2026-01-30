import { memo, useState } from 'react';
import { Handle, Position, type NodeProps } from '@xyflow/react';
import type { AwsResourceNode as AwsResourceNodeType } from '../types/diagram';
import { AWS_SERVICE_COLORS, AWS_SERVICE_ICONS } from '../types/diagram';

function AwsResourceNodeComponent({ data, selected }: NodeProps<AwsResourceNodeType>) {
  const [showTooltip, setShowTooltip] = useState(false);

  const serviceKey = data.service?.toLowerCase() ?? 'default';
  const colors = AWS_SERVICE_COLORS[serviceKey] ?? AWS_SERVICE_COLORS.default;
  const icon = AWS_SERVICE_ICONS[data.resourceType] ?? AWS_SERVICE_ICONS.default;
  const inlineChildren = data.inlineChildren ?? [];

  return (
    <div
      className={`aws-resource-node ${selected ? 'selected' : ''} ${inlineChildren.length > 0 ? 'has-inline-children' : ''}`}
      style={{
        borderColor: colors.border,
        backgroundColor: colors.bg,
        color: colors.text,
      }}
      onMouseEnter={() => setShowTooltip(true)}
      onMouseLeave={() => setShowTooltip(false)}
    >
      <Handle
        type="target"
        position={Position.Top}
        className="aws-node-handle"
        style={{ backgroundColor: colors.border }}
      />

      <div className="aws-node-content">
        <div className="aws-node-icon" title={data.resourceType}>
          {icon}
        </div>
        <div className="aws-node-info">
          <div className="aws-node-label" title={data.label}>
            {data.label}
          </div>
          <div className="aws-node-type">{data.resourceType}</div>
        </div>
      </div>

      {inlineChildren.length > 0 && (
        <div className="aws-node-inline-children">
          {inlineChildren.map(child => (
            <div key={child.id} className="aws-inline-child">
              <span className="inline-child-label" title={child.label}>{child.label}</span>
              <span className="inline-child-type">{child.type}</span>
            </div>
          ))}
        </div>
      )}

      <Handle
        type="source"
        position={Position.Bottom}
        className="aws-node-handle"
        style={{ backgroundColor: colors.border }}
      />

      {showTooltip && (data.arn || data.region || data.tags) && (
        <div className="aws-node-tooltip">
          {data.arn && (
            <div className="tooltip-row">
              <span className="tooltip-label">ARN:</span>
              <span className="tooltip-value">{data.arn}</span>
            </div>
          )}
          {data.region && (
            <div className="tooltip-row">
              <span className="tooltip-label">Region:</span>
              <span className="tooltip-value">{data.region}</span>
            </div>
          )}
          {data.tags && Object.keys(data.tags).length > 0 && (
            <div className="tooltip-row">
              <span className="tooltip-label">Tags:</span>
              <div className="tooltip-tags">
                {Object.entries(data.tags)
                  .slice(0, 3)
                  .map(([key, value]) => (
                    <span key={key} className="tooltip-tag">
                      {key}: {value}
                    </span>
                  ))}
                {Object.keys(data.tags).length > 3 && (
                  <span className="tooltip-more">
                    +{Object.keys(data.tags).length - 3} more
                  </span>
                )}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

export const AwsResourceNode = memo(AwsResourceNodeComponent);
