import { MetricCard } from '../components/MetricCard.jsx'
import { Icon } from '../../../shared/components/Icon.jsx'

const metrics = [
  {
    eyebrow: 'Auth',
    title: 'JWT + JWKS ready',
    description: 'Frontend login surface is ready to hand over to AuthService.',
    icon: 'shield',
  },
  {
    eyebrow: 'Imports',
    title: 'CSV workflow prepared',
    description: 'Upload surfaces are ready for FileService and async transaction parsing.',
    icon: 'files',
  },
  {
    eyebrow: 'Users',
    title: 'Admin views prepared',
    description: 'User management panels are separated and ready for role-aware APIs.',
    icon: 'users',
  },
]

const services = [
  { label: 'AuthService', state: 'ready' },
  { label: 'APIGateway', state: 'ready' },
  { label: 'FileService', state: 'pending' },
  { label: 'TransactionService', state: 'pending' },
]

const nextSteps = [
  {
    title: 'Connect login',
    description: 'Replace the local auth flow with AuthService.',
  },
  {
    title: 'Connect upload',
    description: 'Send CSV files through the gateway to FileService.',
  },
  {
    title: 'Load data',
    description: 'Populate files, transactions and users with real endpoints.',
  },
]

export function OverviewPage() {
  return (
    <div className="view-stack">
      <div className="metric-grid">
        {metrics.map((metric) => (
          <MetricCard key={metric.title} {...metric} />
        ))}
      </div>

      <div className="overview-grid">
        <article className="surface-card panel-card">
          <div className="panel-title">
            <div>
              <span className="eyebrow">Services</span>
              <h3>Current status</h3>
            </div>
            <span className="icon-badge light">
              <Icon name="server" />
            </span>
          </div>

          <div className="service-row">
            {services.map((service) => (
              <span key={service.label} className={`service-badge ${service.state}`}>
                <span>{service.label}</span>
                <span className="service-dot" />
              </span>
            ))}
          </div>
        </article>

        <article className="surface-card panel-card">
          <div className="panel-title">
            <div>
              <span className="eyebrow">Next steps</span>
              <h3>Connect in this order</h3>
            </div>
            <span className="icon-badge light">
              <Icon name="clock" />
            </span>
          </div>

          <div className="timeline-list">
            {nextSteps.map((step, index) => (
              <div key={step.title} className="timeline-item">
                <span className="timeline-marker">{index + 1}</span>
                <div>
                  <strong>{step.title}</strong>
                  <p>{step.description}</p>
                </div>
              </div>
            ))}
          </div>
        </article>
      </div>
    </div>
  )
}
