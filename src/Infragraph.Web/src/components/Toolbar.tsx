import { memo } from 'react';
import { useReactFlow } from '@xyflow/react';

interface ToolbarProps {
  onFitView: () => void;
  onExport?: () => void;
  isLayouting?: boolean;
}

function ToolbarComponent({ onFitView, onExport, isLayouting }: ToolbarProps) {
  const { zoomIn, zoomOut, getZoom } = useReactFlow();

  return (
    <div className="diagram-toolbar" role="toolbar" aria-label="Diagram controls">
      <div className="toolbar-group" role="group" aria-label="Zoom controls">
        <button
          type="button"
          className="toolbar-button"
          onClick={() => zoomIn()}
          aria-label="Zoom in"
          title="Zoom in"
        >
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
            <line x1="11" y1="8" x2="11" y2="14" />
            <line x1="8" y1="11" x2="14" y2="11" />
          </svg>
        </button>

        <button
          type="button"
          className="toolbar-button"
          onClick={() => zoomOut()}
          aria-label="Zoom out"
          title="Zoom out"
        >
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
            <line x1="8" y1="11" x2="14" y2="11" />
          </svg>
        </button>

        <span className="toolbar-zoom-level" aria-live="polite" aria-label="Current zoom level">
          {Math.round(getZoom() * 100)}%
        </span>
      </div>

      <div className="toolbar-divider" role="separator" />

      <div className="toolbar-group" role="group" aria-label="View controls">
        <button
          type="button"
          className="toolbar-button"
          onClick={onFitView}
          disabled={isLayouting}
          aria-label="Fit diagram to view"
          title="Fit to view"
        >
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M8 3H5a2 2 0 0 0-2 2v3" />
            <path d="M21 8V5a2 2 0 0 0-2-2h-3" />
            <path d="M3 16v3a2 2 0 0 0 2 2h3" />
            <path d="M16 21h3a2 2 0 0 0 2-2v-3" />
          </svg>
        </button>
      </div>

      {onExport && (
        <>
          <div className="toolbar-divider" role="separator" />
          <div className="toolbar-group" role="group" aria-label="Export controls">
            <button
              type="button"
              className="toolbar-button"
              onClick={onExport}
              disabled={isLayouting}
              aria-label="Export diagram"
              title="Export"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                <polyline points="7 10 12 15 17 10" />
                <line x1="12" y1="15" x2="12" y2="3" />
              </svg>
            </button>
          </div>
        </>
      )}

      {isLayouting && (
        <div className="toolbar-status" role="status" aria-live="polite">
          <span className="toolbar-spinner" aria-hidden="true" />
          <span>Layouting...</span>
        </div>
      )}
    </div>
  );
}

export const Toolbar = memo(ToolbarComponent);
