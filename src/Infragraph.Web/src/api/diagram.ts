import type { DiagramResponse, AnalysisResponse, ResourceTypeInfo, DiagramOptions } from '../types/diagram';

const API_BASE = '/api';

function buildQueryString(options?: DiagramOptions): string {
  if (!options) return '';

  const params = new URLSearchParams();

  if (options.includeTypes?.length) {
    params.set('includeTypes', options.includeTypes.join(','));
  }
  if (options.excludeTypes?.length) {
    params.set('excludeTypes', options.excludeTypes.join(','));
  }
  if (options.regions?.length) {
    params.set('regions', options.regions.join(','));
  }
  if (options.showIsolated !== undefined) {
    params.set('showIsolated', String(options.showIsolated));
  }
  if (options.grouping?.length) {
    params.set('grouping', options.grouping.join(','));
  }

  const queryString = params.toString();
  return queryString ? `?${queryString}` : '';
}

export async function generateDiagram(
  former2Json: string | object,
  options?: DiagramOptions
): Promise<DiagramResponse> {
  const body = typeof former2Json === 'string' ? former2Json : JSON.stringify(former2Json);
  const queryString = buildQueryString(options);

  const response = await fetch(`${API_BASE}/diagram${queryString}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body,
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(`Failed to generate diagram: ${response.status} ${errorText}`);
  }

  return response.json();
}

export async function analyzeDiagram(former2Json: string | object): Promise<AnalysisResponse> {
  const body = typeof former2Json === 'string' ? former2Json : JSON.stringify(former2Json);

  const response = await fetch(`${API_BASE}/diagram/analyze`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body,
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(`Failed to analyze diagram: ${response.status} ${errorText}`);
  }

  return response.json();
}

export async function getResourceTypes(): Promise<ResourceTypeInfo[]> {
  const response = await fetch(`${API_BASE}/resources/types`);

  if (!response.ok) {
    throw new Error(`Failed to fetch resource types: ${response.status}`);
  }

  return response.json();
}

export async function getResourceTypeInfo(type: string): Promise<ResourceTypeInfo> {
  const response = await fetch(`${API_BASE}/resources/types/${encodeURIComponent(type)}`);

  if (!response.ok) {
    throw new Error(`Failed to fetch resource type info: ${response.status}`);
  }

  return response.json();
}

export async function getResourceCategories(): Promise<string[]> {
  const response = await fetch(`${API_BASE}/resources/categories`);

  if (!response.ok) {
    throw new Error(`Failed to fetch resource categories: ${response.status}`);
  }

  return response.json();
}
