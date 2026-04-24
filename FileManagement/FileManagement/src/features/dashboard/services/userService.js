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

export async function getUsers(accessToken, { role, cursor, pageSize = DEFAULT_PAGE_SIZE } = {}) {
  if (!accessToken) {
    throw new Error('Access token is missing.')
  }

  const searchParams = new URLSearchParams()

  if (role && role !== 'All') {
    searchParams.set('role', role)
  }

  if (cursor) {
    searchParams.set('cursor', cursor)
  }

  if (pageSize) {
    searchParams.set('pageSize', String(pageSize))
  }

  const requestUrl = `${API_BASE_URL}/api/users${searchParams.size > 0 ? `?${searchParams.toString()}` : ''}`

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
    items: (data.items ?? []).map((user) => ({
      id: user.id,
      email: user.email,
      isActive: user.isActive,
      roles: user.roles ?? [],
      permissions: user.effectivePermissions ?? [],
      createdAtUtc: user.createdAtUtc,
    })),
    pageSize: data.pageSize ?? pageSize,
    hasMore: Boolean(data.hasMore),
    nextCursor: data.nextCursor ?? null,
  }
}

export async function createUser(accessToken, payload) {
  if (!accessToken) {
    throw new Error('Access token is missing.')
  }

  const response = await fetch(`${API_BASE_URL}/api/users`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${accessToken}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(payload),
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return response.json()
}

export async function updateUser(accessToken, userId, payload) {
  if (!accessToken) {
    throw new Error('Access token is missing.')
  }

  const response = await fetch(`${API_BASE_URL}/api/users/${userId}`, {
    method: 'PUT',
    headers: {
      Authorization: `Bearer ${accessToken}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(payload),
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return response.json()
}

export async function deleteUser(accessToken, userId) {
  if (!accessToken) {
    throw new Error('Access token is missing.')
  }

  const response = await fetch(`${API_BASE_URL}/api/users/${userId}`, {
    method: 'DELETE',
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }
}
