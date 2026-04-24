import { useEffect, useRef, useState } from 'react'
import { Icon } from '../../../shared/components/Icon.jsx'
import { EmptyStateTable } from '../../../shared/components/EmptyStateTable.jsx'
import { ThemedIcon } from '../../../shared/components/ThemedIcon.jsx'
import { getFiles, uploadFile } from '../services/fileService.js'

const statusOptions = ['All', 'Queued', 'Completed', 'Failed']

function formatFileSize(sizeInBytes) {
  if (sizeInBytes < 1024) {
    return `${sizeInBytes} B`
  }

  if (sizeInBytes < 1024 * 1024) {
    return `${(sizeInBytes / 1024).toFixed(1)} KB`
  }

  return `${(sizeInBytes / (1024 * 1024)).toFixed(1)} MB`
}

function formatUploader(uploadedByUserId, currentUserId) {
  if (uploadedByUserId === currentUserId) {
    return 'You'
  }

  return uploadedByUserId.slice(0, 8)
}

function getStatusClass(status) {
  if (status === 'Completed') {
    return 'success'
  }

  if (status === 'Failed') {
    return 'danger'
  }

  return 'pending'
}

export function FilesPage({ accessToken, currentUserId }) {
  const fileInputRef = useRef(null)
  const [selectedFile, setSelectedFile] = useState(null)
  const [statusFilter, setStatusFilter] = useState('All')
  const [files, setFiles] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [isUploading, setIsUploading] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')
  const [uploadMessage, setUploadMessage] = useState('')
  const [uploadTone, setUploadTone] = useState('neutral')
  const [currentCursor, setCurrentCursor] = useState(null)
  const [cursorHistory, setCursorHistory] = useState([])
  const [nextCursor, setNextCursor] = useState(null)
  const [hasMore, setHasMore] = useState(false)
  const [refreshKey, setRefreshKey] = useState(0)

  useEffect(() => {
    setFiles([])
    setErrorMessage('')
    setCurrentCursor(null)
    setCursorHistory([])
    setNextCursor(null)
    setHasMore(false)
  }, [accessToken, statusFilter])

  useEffect(() => {
    let isMounted = true

    const loadFiles = async () => {
      try {
        setIsLoading(true)
        setErrorMessage('')

        const page = await getFiles(accessToken, {
          status: statusFilter,
          cursor: currentCursor,
        })

        if (isMounted) {
          setFiles(page.items)
          setNextCursor(page.nextCursor)
          setHasMore(page.hasMore)
        }
      } catch (error) {
        if (isMounted) {
          setErrorMessage(error.message ?? 'Failed to load files.')
        }
      } finally {
        if (isMounted) {
          setIsLoading(false)
        }
      }
    }

    loadFiles()

    return () => {
      isMounted = false
    }
  }, [accessToken, statusFilter, currentCursor, refreshKey])

  const currentPage = cursorHistory.length + 1
  const canGoPrevious = cursorHistory.length > 0
  const canGoNext = hasMore && Boolean(nextCursor)

  const handleFileSelection = (event) => {
    const file = event.target.files?.[0] ?? null
    setSelectedFile(file)
    setUploadMessage('')
  }

  const handleUpload = async () => {
    if (!selectedFile) {
      setUploadTone('error')
      setUploadMessage('Choose a CSV file before uploading.')
      return
    }

    try {
      setIsUploading(true)
      setUploadTone('neutral')
      setUploadMessage('')

      await uploadFile(accessToken, selectedFile)

      setSelectedFile(null)
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }

      setUploadTone('success')
      setUploadMessage('File uploaded and queued successfully.')
      setCurrentCursor(null)
      setCursorHistory([])
      setNextCursor(null)
      setHasMore(false)
      setRefreshKey((current) => current + 1)
    } catch (error) {
      setUploadTone('error')
      setUploadMessage(error.message ?? 'Failed to upload file.')
    } finally {
      setIsUploading(false)
    }
  }

  const handleNextPage = () => {
    if (!nextCursor || isLoading) {
      return
    }

    setCursorHistory((current) => [...current, currentCursor])
    setCurrentCursor(nextCursor)
  }

  const handlePreviousPage = () => {
    if (cursorHistory.length === 0 || isLoading) {
      return
    }

    const previousCursor = cursorHistory[cursorHistory.length - 1]
    setCursorHistory((current) => current.slice(0, -1))
    setCurrentCursor(previousCursor)
  }

  return (
    <div className="view-stack">
      <article className="surface-card page-section-card">
        <div className="section-card-header">
          <div className="section-card-title">
            <ThemedIcon name="files" tone="blue" size="md" />
            <div>
              <h2>Upload CSV</h2>
              <p>Send a file directly to FileService for async processing.</p>
            </div>
          </div>
          <span className="status-badge success">
            <span className="status-dot" />
            <span>Live API</span>
          </span>
        </div>

        <div className="section-card-body action-row">
          <label className="field">
            <span className="field-label">CSV file</span>
            <label className="file-picker-shell">
              <input
                ref={fileInputRef}
                type="file"
                accept=".csv,text/csv"
                className="file-picker-input"
                onChange={handleFileSelection}
              />
              <span className="file-picker-button">Choose file</span>
              <span className="file-picker-name">
                {selectedFile ? selectedFile.name : 'No file selected'}
              </span>
            </label>
          </label>

          <button type="button" className="primary-button" onClick={handleUpload} disabled={isUploading}>
            <Icon name="upload" />
            <span>{isUploading ? 'Uploading...' : 'Upload file'}</span>
          </button>
        </div>

        {uploadMessage ? (
          <div className={`status-note ${uploadTone === 'error' ? 'error' : ''}`}>
            <Icon name={uploadTone === 'error' ? 'warning' : 'info'} />
            <span>{uploadMessage}</span>
          </div>
        ) : null}
      </article>

      <article className="surface-card page-section-card">
        <div className="section-card-header">
          <div className="section-card-title">
            <ThemedIcon name="files" tone="slate" size="md" />
            <div>
              <h2>Upload history</h2>
              <p>Recent file imports fetched from FileService.</p>
            </div>
          </div>
        </div>

        <div className="section-card-body filter-row single">
          <label className="field">
            <span className="field-label">Status</span>
            <div className="input-shell">
              <Icon name="filter" />
              <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
                {statusOptions.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            </div>
          </label>
        </div>

        {isLoading ? (
          <div className="table-placeholder">Loading files...</div>
        ) : errorMessage ? (
          <div className="table-placeholder error">{errorMessage}</div>
        ) : files.length === 0 ? (
          <EmptyStateTable
            columns={['File name', 'Uploaded by', 'Status', 'Size', 'Uploaded at']}
            title="No file rows yet"
            body="No file matched the selected filter."
            icon="files"
          />
        ) : (
          <div className="files-table-wrapper">
            <div className="table-head files-table">
              <span>File name</span>
              <span>Uploaded by</span>
              <span>Status</span>
              <span>Size</span>
              <span>Uploaded at</span>
            </div>

            {files.map((file) => (
              <div key={file.id} className="table-row files-table">
                <span className="table-primary-text">{file.originalFileName}</span>
                <span>{formatUploader(file.uploadedByUserId, currentUserId)}</span>
                <div className="file-status-cell">
                  <span className={`inline-status-badge ${getStatusClass(file.status)}`}>
                    {file.status}
                  </span>
                  {file.errorMessage ? (
                    <span className="table-secondary-text">{file.errorMessage}</span>
                  ) : null}
                </div>
                <span>{formatFileSize(file.sizeInBytes)}</span>
                <span>{new Date(file.uploadedAtUtc).toLocaleString()}</span>
              </div>
            ))}
          </div>
        )}

        {files.length > 0 || canGoPrevious || canGoNext ? (
          <div className="table-footer-actions">
            <div className="table-pagination-status">
              <span>Page {currentPage}</span>
              <span>{hasMore ? 'More file records available' : 'End of results'}</span>
            </div>
            <div className="table-pagination-actions">
              <button
                type="button"
                className="secondary-button compact-action"
                onClick={handlePreviousPage}
                disabled={!canGoPrevious || isLoading}
              >
                Previous
              </button>
              <button
                type="button"
                className="secondary-button compact-action"
                onClick={handleNextPage}
                disabled={!canGoNext || isLoading}
              >
                Next
              </button>
            </div>
          </div>
        ) : null}
      </article>
    </div>
  )
}
