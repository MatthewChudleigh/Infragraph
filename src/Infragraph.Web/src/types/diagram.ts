import type { Node, Edge } from '@xyflow/react';

export interface InlineChild {
  id: string;
  label: string;
  type: string;
}

export interface AwsResourceData extends Record<string, unknown> {
  label: string;
  resourceType: string;
  service: string;
  arn?: string;
  region?: string;
  tags?: Record<string, string>;
  properties?: Record<string, unknown>;
  inlineChildren?: InlineChild[];
}

export interface AwsGroupData extends Record<string, unknown> {
  label: string;
  groupType?: string;
  resourceType?: string;
  service?: string;
  childCount?: number;
  expanded?: boolean;
  isGroup?: boolean;
}

export interface JunctionPointData extends Record<string, unknown> {
  groupId: string;
  externalTargetId: string;
  direction: 'outgoing' | 'incoming';
  edgeCount: number;
}

export type AwsResourceNode = Node<AwsResourceData, 'awsResource'>;
export type AwsGroupNode = Node<AwsGroupData, 'awsGroup'>;
export type JunctionPointNode = Node<JunctionPointData, 'junctionPoint'>;
export type DiagramNode = AwsResourceNode | AwsGroupNode | JunctionPointNode;

export interface DiagramEdge extends Omit<Edge, 'label'> {
  label?: string;
  relationshipType?: string;
}

export interface ElkOptions {
  algorithm: string;
  direction: string;
  'elk.hierarchyHandling'?: string;
  'elk.layered.spacing.nodeNodeBetweenLayers'?: string;
  'elk.spacing.nodeNode'?: string;
}

export interface DiagramMetadata {
  totalResources: number;
  includedResources: number;
  totalRelationships: number;
  resourceTypes: string[];
  elkOptions: ElkOptions;
}

export interface DiagramResponse {
  nodes: DiagramNode[];
  edges: DiagramEdge[];
  metadata: DiagramMetadata;
}

export interface AnalysisResponse {
  totalResources: number;
  resourcesByType: Record<string, number>;
  relationships: Array<{
    source: string;
    target: string;
    type: string;
  }>;
  groups: Array<{
    id: string;
    name: string;
    type: string;
    childCount: number;
  }>;
}

export interface ResourceTypeInfo {
  type: string;
  service: string;
  category: string;
  displayName: string;
}

export interface DiagramOptions {
  includeTypes?: string[];
  excludeTypes?: string[];
  regions?: string[];
  showIsolated?: boolean;
  grouping?: string[];
}

// AWS service colors for styling
export const AWS_SERVICE_COLORS: Record<string, { bg: string; border: string; text: string }> = {
  ec2: { bg: '#FF9900', border: '#FF9900', text: '#232F3E' },
  vpc: { bg: '#FF9900', border: '#FF9900', text: '#232F3E' },
  ecs: { bg: '#FF9900', border: '#FF9900', text: '#232F3E' },
  lambda: { bg: '#FF9900', border: '#FF9900', text: '#232F3E' },
  elbv2: { bg: '#8C4FFF', border: '#8C4FFF', text: '#FFFFFF' },
  rds: { bg: '#3B48CC', border: '#3B48CC', text: '#FFFFFF' },
  s3: { bg: '#569A31', border: '#569A31', text: '#FFFFFF' },
  dynamodb: { bg: '#3B48CC', border: '#3B48CC', text: '#FFFFFF' },
  iam: { bg: '#DD344C', border: '#DD344C', text: '#FFFFFF' },
  sqs: { bg: '#FF4F8B', border: '#FF4F8B', text: '#FFFFFF' },
  sns: { bg: '#FF4F8B', border: '#FF4F8B', text: '#FFFFFF' },
  default: { bg: '#232F3E', border: '#232F3E', text: '#FFFFFF' },
};

// AWS service icons (simplified text-based for now)
export const AWS_SERVICE_ICONS: Record<string, string> = {
  'ec2.instance': 'EC2',
  'ec2.vpc': 'VPC',
  'ec2.subnet': 'SN',
  'ec2.securitygroup': 'SG',
  'ec2.routetable': 'RT',
  'ec2.internetgateway': 'IGW',
  'ec2.natgateway': 'NAT',
  'ec2.transitgateway': 'TGW',
  'ecs.cluster': 'ECS',
  'ecs.service': 'SVC',
  'ecs.taskdefinition': 'TD',
  'lambda.function': 'λ',
  'elbv2.loadbalancer': 'ALB',
  'elbv2.targetgroup': 'TG',
  'elbv2.listener': 'LST',
  'rds.dbinstance': 'RDS',
  's3.bucket': 'S3',
  'dynamodb.table': 'DDB',
  'iam.role': 'IAM',
  'iam.user': 'USR',
  'iam.policy': 'POL',
  'sqs.queue': 'SQS',
  'sns.topic': 'SNS',
  default: 'AWS',
};
