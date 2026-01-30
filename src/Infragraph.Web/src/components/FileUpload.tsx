import { memo, useRef, useState, useCallback, type ChangeEvent, type DragEvent } from 'react';

interface FileUploadProps {
  onFileLoad: (content: string, filename: string) => void;
  isLoading?: boolean;
  accept?: string;
}

function FileUploadComponent({
  onFileLoad,
  isLoading = false,
  accept = '.json,application/json',
}: FileUploadProps) {
  const [isDragging, setIsDragging] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleFile = useCallback(
    async (file: File) => {
      setError(null);

      if (!file.name.endsWith('.json') && file.type !== 'application/json') {
        setError('Please upload a JSON file');
        return;
      }

      try {
        const content = await file.text();
        // Validate JSON
        JSON.parse(content);
        onFileLoad(content, file.name);
      } catch {
        setError('Invalid JSON file');
      }
    },
    [onFileLoad]
  );

  const handleChange = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => {
      const file = event.target.files?.[0];
      if (file) {
        handleFile(file);
      }
    },
    [handleFile]
  );

  const handleDragOver = useCallback((event: DragEvent) => {
    event.preventDefault();
    setIsDragging(true);
  }, []);

  const handleDragLeave = useCallback((event: DragEvent) => {
    event.preventDefault();
    setIsDragging(false);
  }, []);

  const handleDrop = useCallback(
    (event: DragEvent) => {
      event.preventDefault();
      setIsDragging(false);

      const file = event.dataTransfer.files?.[0];
      if (file) {
        handleFile(file);
      }
    },
    [handleFile]
  );

  const handleClick = useCallback(() => {
    inputRef.current?.click();
  }, []);

  const handleKeyDown = useCallback(
    (event: React.KeyboardEvent) => {
      if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        handleClick();
      }
    },
    [handleClick]
  );

  return (
    <div className="file-upload-container">
      <div
        className={`file-upload-dropzone ${isDragging ? 'dragging' : ''} ${isLoading ? 'loading' : ''}`}
        role="button"
        tabIndex={0}
        onClick={handleClick}
        onKeyDown={handleKeyDown}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        aria-label="Upload Former2 JSON file"
        aria-disabled={isLoading}
      >
        <input
          ref={inputRef}
          type="file"
          accept={accept}
          onChange={handleChange}
          disabled={isLoading}
          className="file-upload-input"
          aria-hidden="true"
          tabIndex={-1}
        />

        <div className="file-upload-content">
          {isLoading ? (
            <>
              <div className="file-upload-spinner" aria-hidden="true" />
              <span>Processing...</span>
            </>
          ) : (
            <>
              <svg
                className="file-upload-icon"
                width="48"
                height="48"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.5"
                aria-hidden="true"
              >
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                <polyline points="17 8 12 3 7 8" />
                <line x1="12" y1="3" x2="12" y2="15" />
              </svg>
              <div className="file-upload-text">
                <span className="file-upload-primary">
                  Drop your Former2 JSON file here
                </span>
                <span className="file-upload-secondary">
                  or click to browse
                </span>
              </div>
            </>
          )}
        </div>
      </div>

      {error && (
        <div className="file-upload-error" role="alert">
          <svg
            width="16"
            height="16"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            aria-hidden="true"
          >
            <circle cx="12" cy="12" r="10" />
            <line x1="12" y1="8" x2="12" y2="12" />
            <line x1="12" y1="16" x2="12.01" y2="16" />
          </svg>
          <span>{error}</span>
        </div>
      )}

      <p className="file-upload-hint">
        Export your AWS infrastructure from{' '}
        <a
          href="https://former2.com"
          target="_blank"
          rel="noopener noreferrer"
        >
          Former2
        </a>{' '}
        and upload the JSON file here.
      </p>
    </div>
  );
}

export const FileUpload = memo(FileUploadComponent);
