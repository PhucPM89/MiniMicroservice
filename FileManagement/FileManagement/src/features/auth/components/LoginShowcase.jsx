import { Icon } from '../../../shared/components/Icon.jsx'
import { ThemedIcon } from '../../../shared/components/ThemedIcon.jsx'

const serviceTags = [
  { icon: 'shield', label: 'JWT + JWKS', tone: 'blue' },
  { icon: 'files', label: 'CSV imports', tone: 'cyan' },
  { icon: 'users', label: 'Admin area', tone: 'violet' },
]

const previewItems = [
  { icon: 'shield', title: 'Auth', body: 'Login flow ready', tone: 'blue' },
  { icon: 'transactions', title: 'Queue', body: 'Async pipeline', tone: 'cyan' },
  { icon: 'users', title: 'Users', body: 'Role screens ready', tone: 'violet' },
]

export function LoginShowcase() {
  return (
    <section className="hero-card auth-showcase">
      <div className="hero-content">
        <div className="auth-brand">
          <span className="brand-mark">FM</span>
          <div>
            <span className="eyebrow">File Management</span>
          </div>
        </div>

        <h1 className="hero-title">Welcome back.</h1>
        <p className="hero-copy">Login to manage file imports, transactions and user access from one workspace.</p>
      </div>

      <article className="hero-preview-card">
        <div className="hero-preview-header">
          <span className="hero-preview-pill">
            <Icon name="spark" />
            <span>Workspace ready</span>
          </span>
        </div>

        <div className="hero-preview-grid">
          {previewItems.map((item) => (
            <article key={item.title} className="hero-preview-item">
              <ThemedIcon name={item.icon} tone={item.tone} size="md" />
              <div>
                <strong>{item.title}</strong>
                <span>{item.body}</span>
              </div>
            </article>
          ))}
        </div>
      </article>

      <div className="service-tag-row">
        {serviceTags.map((tag) => (
          <article key={tag.label} className="service-tag">
            <ThemedIcon name={tag.icon} tone={tag.tone} size="sm" />
            <strong>{tag.label}</strong>
          </article>
        ))}
      </div>
    </section>
  )
}
