import { useState, useCallback } from 'react';
import { ReactFlowProvider } from '@xyflow/react';
import { FileUpload } from './components/FileUpload';
import { DiagramCanvas } from './components/DiagramCanvas';
import { generateDiagram } from './api/diagram';
import type { DiagramResponse, DiagramOptions } from './types/diagram';
import './App.css';

type AppState = 'upload' | 'loading' | 'diagram' | 'error';

function App() {
  const [state, setState] = useState<AppState>('upload');
  const [diagram, setDiagram] = useState<DiagramResponse | null>(null);
  const [filename, setFilename] = useState<string>('');
  const [error, setError] = useState<string | null>(null);
  const [options, setOptions] = useState<DiagramOptions>({
    grouping: ['vpc', 'service'],
    showIsolated: false,
  });

  const handleFileLoad = useCallback(
    async (content: string, name: string) => {
      setState('loading');
      setError(null);
      setFilename(name);

      try {
        const result = await generateDiagram(content, options);
        setDiagram(result);
        setState('diagram');
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to generate diagram');
        setState('error');
      }
    },
    [options]
  );

  const handleReset = useCallback(() => {
    setState('upload');
    setDiagram(null);
    setFilename('');
    setError(null);
  }, []);

  const handleExport = useCallback(() => {
    // Export functionality will be implemented in Phase 6
    console.log('Export not yet implemented');
  }, []);
  
  const GroupingItem = ({ groupName, groupKey } : { groupName : string, groupKey : string }) => {
    return (
        <div className="option-group">
          <label className="option-label">
            <input
                type="checkbox"
                checked={options.grouping?.includes(groupKey)}
                onChange={(e) => {
                  setOptions((prev) => ({
                    ...prev,
                    grouping: e.target.checked
                        ? [...(prev.grouping ?? []), groupKey]
                        : prev.grouping?.filter((g) => g !== groupKey),
                  }));
                }}
            />
            <span>Group by {groupName}</span>
          </label>
        </div>
    )
  };

  return (
    <div className="app-container">
      <header className="app-header">
        <div className="header-content">
          <div className="logo-section">
            <svg
              className="logo-icon"
              width="40"
              height="40"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.5"
            >
              <polygon points="12 2 2 7 12 12 22 7 12 2" />
              <polyline points="2 17 12 22 22 17" />
              <polyline points="2 12 12 17 22 12" />
            </svg>
            <div className="logo-text">
              <h1 className="app-title">Infragraph</h1>
              <p className="app-subtitle">AWS Infrastructure Visualizer</p>
            </div>
          </div>

          {state === 'diagram' && (
            <div className="header-actions">
              <span className="filename">{filename}</span>
              <button
                type="button"
                className="reset-button"
                onClick={handleReset}
                aria-label="Load new file"
              >
                <svg
                  width="18"
                  height="18"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                >
                  <path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8" />
                  <path d="M3 3v5h5" />
                </svg>
                New File
              </button>
            </div>
          )}
        </div>
      </header>

      <main className="main-content">
        {state === 'upload' && (
          <section className="upload-section">
            <FileUpload onFileLoad={handleFileLoad} isLoading={false} />

            <div className="options-panel">
              <h3 className="options-title">Options</h3>

              <GroupingItem groupName={'VPC'} groupKey={'vpc'} />
              <GroupingItem groupName={'Service'} groupKey={'service'} />
              <GroupingItem groupName={'IAM'} groupKey={'iam'} />

              <div className="option-group">
                <label className="option-label">
                  <input
                    type="checkbox"
                    checked={options.showIsolated ?? false}
                    onChange={(e) => {
                      setOptions((prev) => ({
                        ...prev,
                        showIsolated: e.target.checked,
                      }));
                    }}
                  />
                  <span>Show isolated resources</span>
                </label>
              </div>
            </div>
          </section>
        )}

        {state === 'loading' && (
          <section className="loading-section" role="status" aria-live="polite">
            <div className="loading-spinner" />
            <p>Analyzing infrastructure...</p>
          </section>
        )}

        {state === 'error' && (
          <section className="error-section">
            <div className="error-content" role="alert">
              <svg
                width="48"
                height="48"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.5"
              >
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              <h2>Error Processing File</h2>
              <p>{error}</p>
              <button
                type="button"
                className="retry-button"
                onClick={handleReset}
              >
                Try Again
              </button>
            </div>
          </section>
        )}

        {state === 'diagram' && diagram && (
          <section className="diagram-section">
            <ReactFlowProvider>
              <DiagramCanvas
                initialNodes={diagram.nodes}
                initialEdges={diagram.edges}
                metadata={diagram.metadata}
                onExport={handleExport}
              />
            </ReactFlowProvider>
          </section>
        )}
      </main>

      <footer className="app-footer">
        <nav aria-label="Footer navigation">
          <a
            href="https://github.com"
            target="_blank"
            rel="noopener noreferrer"
          >
            GitHub
          </a>
          <span className="footer-divider">|</span>
          <a
            href="https://former2.com"
            target="_blank"
            rel="noopener noreferrer"
          >
            Former2
          </a>
        </nav>
      </footer>
    </div>
  );
}

export default App;
