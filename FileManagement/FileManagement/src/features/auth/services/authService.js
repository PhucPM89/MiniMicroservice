import { API_BASE_URL } from '../../../shared/config/apiConfig.js'

function deriveDisplayName(email) {
  const localPart = email.split('@')[0] ?? email

  return localPart
    .split(/[._-]+/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ')
}

export const LOGIN_RULES = {
  emailMaxLength: 255,
  passwordMinLength: 8,
  passwordMaxLength: 100,
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function validateLoginPayload({ email, password }) {
  const normalizedEmail = email.trim()
  const normalizedPassword = password.trim()

  const errors = {
    email: '',
    password: '',
  }

  if (!normalizedEmail) {
    errors.email = 'Email is required.'
  } else if (normalizedEmail.length > LOGIN_RULES.emailMaxLength) {
    errors.email = `Email must not exceed ${LOGIN_RULES.emailMaxLength} characters.`
  } else if (!EMAIL_PATTERN.test(normalizedEmail)) {
    errors.email = 'Email format is invalid.'
  }

  if (!normalizedPassword) {
    errors.password = 'Password is required.'
  } else if (normalizedPassword.length < LOGIN_RULES.passwordMinLength) {
    errors.password = `Password must be at least ${LOGIN_RULES.passwordMinLength} characters.`
  } else if (normalizedPassword.length > LOGIN_RULES.passwordMaxLength) {
    errors.password = `Password must not exceed ${LOGIN_RULES.passwordMaxLength} characters.`
  }

  return errors
}


function deriveRoleLabel(roles){
  if(!Array.isArray(roles) || roles.length === 0){
    return 'User'
  }

  if(roles.includes('Admin')){
    return 'Administrator'
  }

  return roles[0]
}

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
  return `Login failed with status ${response.status}.`
}

export async function loginWithPassword({ email, password }) {
  const normalizedEmail = email.trim().toLowerCase()
  const normalizedPassword = password.trim()

  if (!normalizedEmail || !normalizedPassword) {
    throw new Error('Email and password are required.')
  }

  const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: normalizedEmail, password: normalizedPassword }),
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const data = await response.json()

  return {
    id: data.user.id,
    email: data.user.email,
    isActive: data.user.isActive,
    roles: data.user.roles ?? [],
    permissions: data.user.effectivePermissions ?? [],
    directGrantedPermissions: data.user.directGrantedPermissions ?? [],
    directDeniedPermissions: data.user.directDeniedPermissions ?? [],
    createdAtUtc: data.user.createdAtUtc,
    updatedAtUtc: data.user.updatedAtUtc,
    accessToken: data.accessToken,
    expiresAtUtc: data.expiresAtUtc,
    displayName: deriveDisplayName(data.user.email) || 'Workspace User',
    roleLabel: deriveRoleLabel(data.user.roles),
  }
}
