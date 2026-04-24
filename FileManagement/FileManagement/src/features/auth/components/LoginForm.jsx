import { Icon } from '../../../shared/components/Icon.jsx'
import { ThemedIcon } from '../../../shared/components/ThemedIcon.jsx'

export function LoginForm({
  email,
  password,
  fieldErrors,
  statusMessage,
  statusTone,
  isSubmitting,
  onEmailChange,
  onPasswordChange,
  onEmailBlur,
  onPasswordBlur,
  onSubmit,
}) {

  const isError = statusTone === 'error'

  return (
    <section className="auth-panel">
      <div className="surface-card auth-card">
        <div className="auth-card-header">
          <h2>Login</h2>
        </div>

        <div className="auth-form">
          <label className="field">
            <span className="field-label">Email</span>
            <div className={`input-shell ${fieldErrors.email ? 'invalid' : ''}`}>
              <ThemedIcon name="mail" tone="blue" size="sm" />
              <input
                type="email"
                placeholder="name@example.com"
                value={email}
                onChange={(event) => onEmailChange(event.target.value)}
                onBlur={onEmailBlur}
                required
                maxLength={255}
                autoComplete="email"
                aria-invalid={Boolean(fieldErrors.email)}
              />
            </div>
            {fieldErrors.email ? <span className="field-error">{fieldErrors.email}</span> : null}
          </label>


          <label className="field">
            <span className="field-label">Password</span>
            <div className={`input-shell ${fieldErrors.password ? 'invalid' : ''}`}>
              <ThemedIcon name="lock" tone="slate" size="sm" />
              <input
                type="password"
                placeholder="Enter password"
                value={password}
                onChange={(event) => onPasswordChange(event.target.value)}
                onBlur={onPasswordBlur}
                required
                minLength={8}
                maxLength={100}
                autoComplete="current-password"
                aria-invalid={Boolean(fieldErrors.password)}
              />
            </div>
            {fieldErrors.password ? <span className="field-error">{fieldErrors.password}</span> : null}
          </label>


          <button type="button" className="primary-button" onClick={onSubmit} disabled={isSubmitting}>
            <Icon name="login" />
            <span>{isSubmitting ? 'Opening workspace...' : 'Login'}</span>
          </button>

          <div className={`status-note ${isError ? 'error' : ''}`}>
            <Icon name={isError ? 'warning' : 'info'} />
            <span>{statusMessage}</span>
          </div>
        </div>
      </div>
    </section>
  )
}
