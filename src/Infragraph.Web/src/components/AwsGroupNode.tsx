import { memo } from 'react';
import { type NodeProps, NodeResizer } from '@xyflow/react';
import type { AwsGroupNode as AwsGroupNodeType } from '../types/diagram';

interface GroupStyle {
  borderColor: string;
  borderStyle: string;
  backgroundColor: string;
  headerColor: string;
}

const GROUP_STYLES: Record<string, GroupStyle> = {
  vpc: {
    borderColor: '#FF9900',
    borderStyle: 'dashed',
    backgroundColor: 'rgba(255, 153, 0, 0.05)',
    headerColor: '#FF9900',
  },
  subnet: {
    borderColor: '#FFB84D',
    borderStyle: 'dotted',
    backgroundColor: 'rgba(255, 184, 77, 0.05)',
    headerColor: '#FFB84D',
  },
  securitygroup: {
    borderColor: '#DD344C',
    borderStyle: 'solid',
    backgroundColor: 'rgba(221, 52, 76, 0.03)',
    headerColor: '#DD344C',
  },
  cluster: {
    borderColor: '#FF9900',
    borderStyle: 'solid',
    backgroundColor: 'rgba(255, 153, 0, 0.03)',
    headerColor: '#FF9900',
  },
  service: {
    borderColor: '#8C4FFF',
    borderStyle: 'solid',
    backgroundColor: 'rgba(140, 79, 255, 0.03)',
    headerColor: '#8C4FFF',
  },
  default: {
    borderColor: '#232F3E',
    borderStyle: 'solid',
    backgroundColor: 'rgba(35, 47, 62, 0.03)',
    headerColor: '#232F3E',
  },
};

function getGroupStyle(groupType: string | undefined): GroupStyle {
  if (!groupType) return GROUP_STYLES.default;
  const typeKey = groupType.split('.').pop()?.toLowerCase() ?? '';
  return GROUP_STYLES[typeKey] ?? GROUP_STYLES.default;
}

function AwsGroupNodeComponent({ data, selected }: NodeProps<AwsGroupNodeType>) {
  const style = getGroupStyle(data.groupType ?? data.resourceType);

  return (
    <div
      className={`aws-group-node ${selected ? 'selected' : ''}`}
      style={{
        borderColor: style.borderColor,
        borderStyle: style.borderStyle,
        backgroundColor: style.backgroundColor,
      }}
    >
      <NodeResizer
        minWidth={280}
        minHeight={200}
        isVisible={selected}
        lineClassName="aws-group-resize-line"
        handleClassName="aws-group-resize-handle"
      />
      <div
        className="aws-group-header"
        style={{ backgroundColor: style.headerColor }}
      >
        <span className="aws-group-label">{data.label}</span>
        {data.childCount !== undefined && (
          <span className="aws-group-count">{data.childCount}</span>
        )}
      </div>
      <div className="aws-group-content">
        {/* Child nodes will be rendered here by React Flow */}
      </div>
    </div>
  );
}

export const AwsGroupNode = memo(AwsGroupNodeComponent);
