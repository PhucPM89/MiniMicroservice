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

export async function getTransactions(
  accessToken,
  {
    transactionId,
    type,
    cursor,
    pageSize = DEFAULT_PAGE_SIZE,
  } = {},
) {
  if (!accessToken) {
    throw new Error('Access token is missing.')
  }

  const searchParams = new URLSearchParams()

  if (transactionId?.trim()) {
    searchParams.set('transactionId', transactionId.trim())
  }

  if (type && type !== 'All') {
    searchParams.set('type', type)
  }

  if (cursor) {
    searchParams.set('cursor', cursor)
  }

  if (pageSize) {
    searchParams.set('pageSize', String(pageSize))
  }

  const requestUrl = `${API_BASE_URL}/api/transactions${searchParams.size > 0 ? `?${searchParams.toString()}` : ''}`

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
    items: (data.items ?? []).map((transaction) => ({
      id: transaction.id,
      importBatchId: transaction.importBatchId,
      fileId: transaction.fileId,
      fileName: transaction.fileName,
      transactionId: transaction.transactionId,
      amount: transaction.amount,
      type: transaction.type,
      description: transaction.description,
      rawLineNumber: transaction.rawLineNumber,
      createdAtUtc: transaction.createdAtUtc,
    })),
    pageSize: data.pageSize ?? pageSize,
    hasMore: Boolean(data.hasMore),
    nextCursor: data.nextCursor ?? null,
  }
}
