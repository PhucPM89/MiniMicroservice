import { useState } from 'react'
import { loginWithPassword, validateLoginPayload } from '../services/authService.js'

const STORAGE_KEY = 'file-management.auth'

function readStoredSession() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) {
      return null
    }

    const parsed = JSON.parse(raw)

    if (!parsed?.accessToken || !parsed?.expiresAtUtc) {
      localStorage.removeItem(STORAGE_KEY)
      return null
    }

    const expiresAt = new Date(parsed.expiresAtUtc).getTime()
    if (Number.isNaN(expiresAt) || expiresAt <= Date.now()) {
      localStorage.removeItem(STORAGE_KEY)
      return null
    }

    return parsed
  } catch {
    localStorage.removeItem(STORAGE_KEY)
    return null
  }
}

function persistSession(session) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
}

function clearStoredSession() {
  localStorage.removeItem(STORAGE_KEY)
}

export function useAuthViewModel() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [session, setSession] = useState(() => readStoredSession())
  const [statusMessage, setStatusMessage] = useState('Use your email and password to open the workspace.')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [fieldErrors, setFieldErrors] = useState({ email: '', password: '' })
  const [touched, setTouched] = useState({ email: false, password: false })
  const [statusTone, setStatusTone] = useState('neutral')

  const runValidation = (nextEmail = email, nextPassword = password) => {
    return validateLoginPayload({
      email: nextEmail,
      password: nextPassword,
    })
  }

  const handleEmailChange = (value) => {
    setEmail(value)

    if (touched.email) {
      setFieldErrors((current) => ({
        ...current,
        email: runValidation(value, password).email,
      }))
    }
  }

  const handlePasswordChange = (value) => {
    setPassword(value)

    if (touched.password) {
      setFieldErrors((current) => ({
        ...current,
        password: runValidation(email, value).password,
      }))
    }
  }

  const handleEmailBlur = () => {
    const errors = runValidation(email, password)
    setTouched((current) => ({ ...current, email: true }))
    setFieldErrors((current) => ({ ...current, email: errors.email }))
  }

  const handlePasswordBlur = () => {
    const errors = runValidation(email, password)
    setTouched((current) => ({ ...current, password: true }))
    setFieldErrors((current) => ({ ...current, password: errors.password }))
  }


  const login = async () => {
    const errors = runValidation(email, password)

    setTouched({ email: true, password: true })
    setFieldErrors(errors)

    if (errors.email || errors.password) {
      setStatusTone('error')
      setStatusMessage('Please correct the highlighted fields.')
      return
    }

    try {
      setIsSubmitting(true)
      setStatusTone('neutral')
      setStatusMessage('Signing in...')

      const result = await loginWithPassword({ email, password })

      setSession(result)
      persistSession(result)
      setStatusTone('success')
      setStatusMessage('Signed in successfully.')
    } catch (error) {
      clearStoredSession()
      setSession(null)
      setStatusTone('error')
      setStatusMessage(error.message ?? 'Login failed.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const logout = () => {
    clearStoredSession()
    setSession(null)
    setPassword('')
    setFieldErrors({ email: '', password: '' })
    setTouched({ email: false, password: false })
    setStatusTone('neutral')
    setStatusMessage('Session cleared locally.')
  }


  return {
    email,
    password,
    session,
    statusMessage,
    statusTone,
    isSubmitting,
    fieldErrors,
    onEmailChange: handleEmailChange,
    onPasswordChange: handlePasswordChange,
    onEmailBlur: handleEmailBlur,
    onPasswordBlur: handlePasswordBlur,
    login,
    logout,
  }

}
