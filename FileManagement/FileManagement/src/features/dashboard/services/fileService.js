import { API_BASE_URL } from '../../../shared/config/apiConfig.js'
const DEFAULT_PAGE_SIZE = 10

async function readErrorMessage(response) {
  try {
    const payload = await response.json()

    if (Array.isArray(payload.errors) && payload.errors.length > 0) {
      return payload.errors[0]
    }

    if (payload?.message) {
      return payload.message
    }
  } catch {
    // ignore parse error
  }

  return `Request failed with status ${response.status}.`
}

function mapFile(file) {
  return {
    id: file.id,
    originalFileName: file.originalFileName,
    storedFileName: file.storedFileName,
    storagePath: file.storagePath,
    contentType: file.contentType,
    fileExtension: file.fileExtension,
    sizeInBytes: file.sizeInBytes,
    uploadedByUserId: file.uploadedByUserId,
    correlationId: file.correlationId,
    status: file.status,
    errorMessage: file.errorMessage,
    uploadedAtUtc: file.uploadedAtUtc,
  }
}

export async function getFiles(accessToken, { status, cursor, pageSize = DEFAULT_PAGE_SIZE } = {}) {
  if (!accessToken) {
    throw new Error('Access token is missing.')
  }

  const searchParams = new URLSearchParams()

  if (status && status !== 'All') {
    searchParams.set('status', status)
  }

  if (cursor) {
    searchParams.set('cursor', cursor)
  }

  if (pageSize) {
    searchParams.set('pageSize', String(pageSize))
  }

  const requestUrl = `${API_BASE_URL}/api/files${searchParams.size > 0 ? `?${searchParams.toString()}` : ''}`

  const response = await fetch(requestUrl, {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${accessToken}`,
      'Content-Type': 'application/json',
    },
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const data = await response.json()

  return {
    items: (data.items ?? []).map(mapFile),
    pageSize: data.pageSize ?? pageSize,
    hasMore: Boolean(data.hasMore),
    nextCursor: data.nextCursor ?? null,
  }
}

export async function uploadFile(accessToken, file) {
  if (!accessToken) {
    throw new Error('Access token is missing.')
  }

  if (!file) {
    throw new Error('A CSV file is required.')
  }

  const formData = new FormData()
  formData.append('file', file)

  const response = await fetch(`${API_BASE_URL}/api/files/upload`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
    body: formData,
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const data = await response.json()
  return mapFile(data)
}
